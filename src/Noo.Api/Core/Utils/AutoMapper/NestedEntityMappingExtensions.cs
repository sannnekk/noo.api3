using AutoMapper;
using Noo.Api.Core.DataAbstraction.Model;

namespace Noo.Api.Core.Utils.AutoMapper;

/// <summary>
/// Extension methods for mapping nested entities to and from dictionary-based DTOs.
/// This is useful for JSON Patch operations where array indices are not ideal.
/// </summary>
public static class NestedEntityMappingExtensions
{
    /// <summary>
    /// Maps a collection of entities to a dictionary keyed by entity ID.
    /// Used for converting Model -> UpdateDTO (for patch operations).
    /// </summary>
    /// <typeparam name="TEntity">The entity type (must have an Id property)</typeparam>
    /// <typeparam name="TDto">The DTO type to map to</typeparam>
    /// <param name="entities">The collection of entities to map</param>
    /// <param name="context">The AutoMapper resolution context</param>
    /// <returns>A dictionary with entity IDs as keys and mapped DTOs as values</returns>
    public static IDictionary<string, TDto>? MapCollectionToDictionary<TEntity, TDto>(
        this IEnumerable<TEntity>? entities,
        ResolutionContext context
    ) where TEntity : BaseModel
    {
        if (entities == null)
            return null;

        return entities.ToDictionary(
            entity => entity.Id.ToString(),
            context.Mapper.Map<TDto>,
            StringComparer.OrdinalIgnoreCase
        );
    }

    /// <summary>
    /// Maps a dictionary of DTOs back to a collection of entities.
    /// Used for converting UpdateDTO -> Model (after patch operations).
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TEntity">The entity type to map to</typeparam>
    /// <param name="dtoDict">The dictionary of DTOs keyed by entity ID</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <returns>A collection of mapped entities, or null if input is null</returns>
    public static ICollection<TEntity>? MapDictionaryToCollection<TDto, TEntity>(
        this IDictionary<string, TDto>? dtoDict,
        IRuntimeMapper mapper
    )
    {
        if (dtoDict == null)
            return null;

        return dtoDict.Values
            .Select(dto => mapper.Map<TEntity>(dto))
            .ToList();
    }
}

