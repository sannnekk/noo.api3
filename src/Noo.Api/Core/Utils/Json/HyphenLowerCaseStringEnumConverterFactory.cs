using System.Text.Json;
using System.Text.Json.Serialization;

namespace Noo.Api.Core.Utils.Json;

public class HyphenLowerCaseStringEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsEnum || Nullable.GetUnderlyingType(typeToConvert)?.IsEnum == true;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var underlyingType = Nullable.GetUnderlyingType(typeToConvert);

        if (underlyingType != null)
        {
            var nullableConverterType = typeof(NullableHyphenLowerCaseStringEnumConverter<>).MakeGenericType(underlyingType);
            return (JsonConverter)Activator.CreateInstance(nullableConverterType)!;
        }

        var converterType = typeof(HyphenLowerCaseStringEnumConverter<>).MakeGenericType(typeToConvert);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private class HyphenLowerCaseStringEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        private readonly JsonNamingPolicy _namingPolicy = new HyphenLowerCaseNamingPolicy();

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string enumString = reader.GetString()!;
            string pascalCase = enumString.Replace("-", "");

            return Enum.Parse<T>(pascalCase, ignoreCase: true);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            string enumName = value.ToString();
            string hyphenCase = _namingPolicy.ConvertName(enumName);

            writer.WriteStringValue(hyphenCase);
        }

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string enumString = reader.GetString() ?? throw new JsonException("Enum property name cannot be null");
            string pascalCase = enumString.Replace("-", "");

            return Enum.Parse<T>(pascalCase, ignoreCase: true);
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            string enumName = value.ToString();
            string hyphenCase = _namingPolicy.ConvertName(enumName);

            writer.WritePropertyName(hyphenCase);
        }
    }

    private class NullableHyphenLowerCaseStringEnumConverter<T> : JsonConverter<T?> where T : struct, Enum
    {
        private readonly HyphenLowerCaseStringEnumConverter<T> _innerConverter = new();

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return _innerConverter.Read(ref reader, typeof(T), options);
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            _innerConverter.Write(writer, value.Value, options);
        }

        public override T? ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return _innerConverter.ReadAsPropertyName(ref reader, typeof(T), options);
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                throw new JsonException("Nullable enum keys are not supported in JSON objects.");
            }

            _innerConverter.WriteAsPropertyName(writer, value.Value, options);
        }
    }
}
