using AutoMapper;
using Noo.Api.Core.DataAbstraction.Model;
using UlidT = System.Ulid;

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
    /// Merges a dictionary of DTOs against an existing collection of tracked entities, keyed by
    /// entity Id (the dictionary key), and returns the merged result as a new list.
    ///
    /// Existing entities that appear in the dict are updated in place via the AutoMapper instance
    /// (their original Id and the EF-tracked instance reference are preserved); new keys produce
    /// new entities with the key parsed as their Id; entities missing from the dict are simply
    /// absent from the returned list — EF will then treat them as orphaned children and cascade as
    /// configured.
    ///
    /// Used for converting UpdateDTO -> Model after JSON-patch operations on nested children.
    /// A new list is returned (rather than mutating <paramref name="destination"/> in place)
    /// because AutoMapper's default collection mapping clears the destination before assigning;
    /// if MapFrom returned the same reference, the clear step would wipe the merge result. The
    /// entity instances inside the new list are still the original tracked references, so EF
    /// observes only field-level edits on kept entries plus inserts/orphans on the diff.
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TEntity">The entity type to map to</typeparam>
    /// <param name="dtoDict">The dictionary of DTOs keyed by entity Id. If null, the destination is returned unchanged.</param>
    /// <param name="destination">The existing destination collection on the parent entity. May be null or empty.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    /// <returns>A new list containing the merged entities (existing references reused, new entries created).</returns>
    public static ICollection<TEntity> MapDictionaryToCollection<TDto, TEntity>(
        this IDictionary<string, TDto>? dtoDict,
        ICollection<TEntity>? destination,
        IRuntimeMapper mapper
    ) where TEntity : BaseModel
    {
        if (dtoDict == null)
        {
            return destination ?? new List<TEntity>();
        }

        var existingById = destination?.ToDictionary(e => e.Id) ?? new Dictionary<UlidT, TEntity>();
        var merged = new List<TEntity>(dtoDict.Count);

        foreach (var (key, dto) in dtoDict)
        {
            var id = UlidT.TryParse(key, out var parsed) ? parsed : UlidT.NewUlid();

            if (existingById.TryGetValue(id, out var entity))
            {
                mapper.Map(dto, entity);
                merged.Add(entity);
            }
            else
            {
                var newEntity = mapper.Map<TEntity>(dto);
                newEntity.Id = id;
                merged.Add(newEntity);
            }
        }

        return merged;
    }
}

