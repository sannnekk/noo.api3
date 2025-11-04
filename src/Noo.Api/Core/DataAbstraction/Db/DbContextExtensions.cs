using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Noo.Api.Core.DataAbstraction.Db.ValueComparers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.Core.Utils.Ulid;

namespace Noo.Api.Core.DataAbstraction.Db;

public static class DbContextExtensions
{
    /// <summary>
    /// Registers all models in the assembly marked with ModelAttribute with the DbContext.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static void RegisterModels(this ModelBuilder modelBuilder)
    {
        var modelTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(
                t => t.IsClass &&
                !t.IsAbstract &&
                t.GetCustomAttribute<ModelAttribute>() != null
            );

        foreach (var modelType in modelTypes)
        {
            if (!modelType.IsSubclassOf(typeof(BaseModel)))
            {
                throw new InvalidOperationException($"Model {modelType.Name} must inherit from BaseModel");
            }

            modelBuilder.Entity(modelType);

            modelBuilder.Entity(modelType)
                .HasKey("Id");

            modelBuilder.Entity(modelType)
                .Property("CreatedAt")
                .ValueGeneratedOnAdd();

            modelBuilder.Entity(modelType)
                .Property("UpdatedAt")
                .ValueGeneratedOnUpdate();
        }
    }

    /// <summary>
    /// Configures many-to-many tables in the database.
    /// Makes readable many-to-many table names and configures ON DELETE CASCADE.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static void ConfigureManyToManyTables(this ModelBuilder modelBuilder)
    {
        var skipNavs = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(et => et.GetSkipNavigations());

        foreach (var skipNav in skipNavs)
        {
            var joinEntityType = skipNav.JoinEntityType;

            if (joinEntityType == null)
            {
                continue;
            }

            var rightName = skipNav.TargetEntityType.ClrType.GetCustomAttribute<ModelAttribute>()?.Name;
            var leftName = skipNav.DeclaringEntityType.ClrType
                .GetCustomAttribute<ModelAttribute>()?.Name;

            var propName = skipNav.Name;

            if (rightName == null || leftName == null)
            {
                throw new InvalidOperationException(
                    $"Join entity type {joinEntityType.Name} does not have a valid name attribute."
                );
            }

            var tableName = string.CompareOrdinal(leftName, rightName) <= 0
                ? $"{leftName}_mm_{propName}_{rightName}"
                : $"{rightName}_mm_{propName}_{leftName}";

            joinEntityType.SetTableName(tableName);

            foreach (var fk in joinEntityType.GetForeignKeys())
            {
                var principal = fk.PrincipalEntityType.ClrType.Name;

                principal = principal.EndsWith("Model") ? principal[0..^5] : principal;

                fk.Properties[0].SetColumnName(StringHelpers.ToSnakeCase($"{principal}Id"));
                fk.DeleteBehavior = DeleteBehavior.Cascade;
            }
        }
    }

    /// <summary>
    /// Configures rich text columns in the database.
    /// </summary>
    public static void UseRichTextColumns(this ModelBuilder modelBuilder)
    {
        var richTextProperties = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(BaseModel)))
            .SelectMany(t => t.GetProperties())
            .Where(p => p?.GetCustomAttribute<RichTextColumnAttribute>() != null);

        foreach (var property in richTextProperties)
        {
            var richTextAttribute = property.GetCustomAttribute<RichTextColumnAttribute>()!;

            var converter = new ValueConverter<IRichTextType, string?>(
                v => v.ToString(),
                v => new DeltaRichText(v)
            );

            modelBuilder.Entity(property.DeclaringType!)
                .Property(property.Name)
                .HasConversion(converter)
                .HasColumnType(richTextAttribute.TypeName)
                .HasCharSet(richTextAttribute.Charset)
                .UseCollation(richTextAttribute.Collation);
        }
    }

    /// <summary>
    /// Configures JSON dictionary columns in the database.
    /// </summary>
    /// <param name="modelBuilder"></param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="InvalidCastException"></exception>
    public static void UseJsonDictionaryColumns(this ModelBuilder modelBuilder)
    {
        var jsonProperties = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(BaseModel)))
            .SelectMany(t => t.GetProperties())
            .Where(p => p?.GetCustomAttribute<JsonColumnAttribute>() != null);

        var serializationOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        foreach (var property in jsonProperties)
        {
            var jsonAttribute = property.GetCustomAttribute<JsonColumnAttribute>()!;

            if (jsonAttribute.Converter != null)
            {
                modelBuilder.Entity(property.DeclaringType!)
                    .Property(property.Name)
                    .HasColumnName(jsonAttribute.Name)
                    .HasConversion(jsonAttribute.Converter)
                    .HasColumnType("json");

                continue;
            }

            // assume it is a Dictionary with primitive types

            var propertyType = property.PropertyType;
            if (!propertyType.IsGenericType || propertyType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
                continue;

            var typeArgs = propertyType.GetGenericArguments();
            if (typeArgs.Length != 2)
            {
                throw new InvalidOperationException(
                    $"JsonDictionaryColumnAttribute on {property.DeclaringType!.Name}.{property.Name} must have exactly two type arguments (Dictionary)."
                );
            }

            var keyType = typeArgs[0];
            if (keyType != typeof(string))
            {
                throw new InvalidOperationException(
                    $"JsonDictionaryColumnAttribute on {property.DeclaringType!.Name}.{property.Name} must have a key type of string."
                );
            }

            var valueType = typeArgs[1];
            if (!_IsPrimitiveType(valueType) && !_IsNullablePrimitiveType(valueType))
            {
                throw new InvalidOperationException(
                    $"JsonDictionaryColumnAttribute on {property.DeclaringType!.Name}.{property.Name} must have a primitive value type."
                );
            }

            var converterType = typeof(JsonDictionaryConverter<>).MakeGenericType(valueType);
            var converter = (ValueConverter?)Activator.CreateInstance(converterType) ?? throw new InvalidCastException($"Failed creating ValueConverter for {property.DeclaringType!.Name}.{property.Name}");

            var propBuilder = modelBuilder.Entity(property.DeclaringType!)
                .Property(property.Name)
                .HasColumnName(jsonAttribute.Name)
                .HasConversion(converter)
                .HasColumnType("json");

            // Attach a ValueComparer so EF Core can perform structural equality on the dictionary
            var valueComparer = _CreateDictionaryValueComparer(valueType);
            if (valueComparer != null)
            {
                propBuilder.Metadata.SetValueComparer(valueComparer);
            }
        }
    }

    /// <summary>
    /// Configures ULID array columns in the database.
    /// </summary>
    /// <param name="modelBuilder"></param>
    public static void UseUlidArrayColumns(this ModelBuilder modelBuilder)
    {
        var ulidArrayProperties = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(BaseModel)))
            .SelectMany(t => t.GetProperties())
            .Where(p => p?.GetCustomAttribute<UlidArrayColumnAttribute>() != null);

        foreach (var property in ulidArrayProperties)
        {
            var ulidArrayAttribute = property.GetCustomAttribute<UlidArrayColumnAttribute>()!;

            var propBuilder = modelBuilder.Entity(property.DeclaringType!)
                .Property(property.Name)
                .HasConversion(new UlidArrayToJsonConverter())
                .HasColumnType("json");

            // Provide structural comparer for Ulid[] so EF change tracker works correctly
            propBuilder.Metadata.SetValueComparer(CollectionValueComparers.UlidArray);
        }
    }

    /// <summary>
    /// Applies all IOnModelCreationExtension implementations found in the assembly to the model builder.
    /// </summary>
    /// <param name="modelBuilder"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void UseOnModelCreatingExtensions(this ModelBuilder modelBuilder)
    {
        var extensions = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<OnModelCreationExtensionAttribute>() != null);

        foreach (var extension in extensions)
        {
            var instance = Activator.CreateInstance(extension);

            if (instance is IOnModelCreationExtension onModelCreationExtension)
            {
                onModelCreationExtension.OnModelCreating(modelBuilder);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Type {extension.Name} does not implement IOnModelCreationExtension."
                );
            }
        }
    }

    private static bool _IsPrimitiveType(Type type)
    {
        return type.IsPrimitive || type == typeof(decimal) || type == typeof(string) || type == typeof(DateTime);
    }

    private static bool _IsNullablePrimitiveType(Type type)
    {
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>))
            return false;

        var underlyingType = Nullable.GetUnderlyingType(type)!;
        return _IsPrimitiveType(underlyingType);
    }

    private static ValueComparer? _CreateDictionaryValueComparer(Type valueType)
    {
        var method = typeof(CollectionValueComparers).GetMethod(nameof(CollectionValueComparers.Dictionary))!;
        var generic = method.MakeGenericMethod(valueType);
        return (ValueComparer?)generic.Invoke(null, null);
    }
}
