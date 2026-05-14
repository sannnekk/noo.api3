using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.Utils.AutoMapper;

namespace Noo.UnitTests.Core.Utils.AutoMapper;

/// <summary>
/// Regression tests for <see cref="NestedEntityMappingExtensions.MapDictionaryToCollection"/>.
///
/// This helper is the foundation for every patch flow in the codebase that maps a
/// dictionary-keyed-by-Id of child DTOs onto a parent entity's tracked collection
/// (Work.Tasks, Course.Chapters, CourseChapter.SubChapters, CourseChapter.Materials,
/// CourseMaterialContent.WorkAssignments). A bug here corrupts all of them at once —
/// e.g. assigning fresh entity instances would orphan EF-tracked rows and cascade-null
/// linked tables (assigned_work_answer was bitten by exactly that).
/// </summary>
public class NestedEntityMappingExtensionsTests
{
    private sealed class FakeChildDto
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    private sealed class FakeChildEntity : BaseModel
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    private static IRuntimeMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FakeChildDto, FakeChildEntity>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.UpdatedAt, o => o.Ignore());
        }, NullLoggerFactory.Instance);
        // AutoMapper's Mapper implements both IMapper and IRuntimeMapper.
        return (IRuntimeMapper)config.CreateMapper();
    }

    [Fact(DisplayName = "Merge: null dict returns the destination unchanged")]
    public void NullDict_ReturnsDestinationUnchanged()
    {
        var mapper = CreateMapper();
        var existing = new FakeChildEntity { Name = "keep", Value = 1 };
        var dest = new List<FakeChildEntity> { existing };

        var result = ((IDictionary<string, FakeChildDto>?)null)
            .MapDictionaryToCollection(dest, mapper);

        // When the dict is null the helper signals "no change" by handing back
        // the destination as-is.
        Assert.Single(result);
        Assert.Same(existing, result.First());
    }

    [Fact(DisplayName = "Merge: null destination is replaced with a fresh list")]
    public void NullDestination_AllocatesList()
    {
        var mapper = CreateMapper();
        var id = Ulid.NewUlid();
        var dict = new Dictionary<string, FakeChildDto>
        {
            [id.ToString()] = new() { Name = "n", Value = 7 }
        };

        var result = dict.MapDictionaryToCollection<FakeChildDto, FakeChildEntity>(null, mapper);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(id, result.First().Id);
    }

    [Fact(DisplayName = "Merge: existing entity instance is reused (so EF tracking is preserved)")]
    public void ExistingEntity_InstanceIsReused()
    {
        var mapper = CreateMapper();
        var id = Ulid.NewUlid();
        var existing = new FakeChildEntity { Id = id, Name = "old", Value = 1 };
        var dest = new List<FakeChildEntity> { existing };

        var dict = new Dictionary<string, FakeChildDto>
        {
            [id.ToString()] = new() { Name = "new", Value = 42 }
        };

        var result = dict.MapDictionaryToCollection(dest, mapper);

        Assert.Single(result);
        // Same tracked instance — EF will see only field changes, not insert/delete.
        Assert.Same(existing, result.First());
        Assert.Equal(id, existing.Id);
        Assert.Equal("new", existing.Name);
        Assert.Equal(42, existing.Value);
    }

    [Fact(DisplayName = "Merge: new key produces new entity with Id parsed from the key")]
    public void NewKey_ProducesEntityWithIdFromKey()
    {
        var mapper = CreateMapper();
        var newId = Ulid.NewUlid();
        var dest = new List<FakeChildEntity>();

        var dict = new Dictionary<string, FakeChildDto>
        {
            [newId.ToString()] = new() { Name = "new", Value = 9 }
        };

        var result = dict.MapDictionaryToCollection(dest, mapper);

        Assert.Single(result);
        var added = result.First();
        Assert.Equal(newId, added.Id);
        Assert.Equal("new", added.Name);
    }

    [Fact(DisplayName = "Merge: non-ulid key still produces an entity (with a fresh Id)")]
    public void NonUlidKey_StillProducesEntity()
    {
        var mapper = CreateMapper();
        var dest = new List<FakeChildEntity>();

        var dict = new Dictionary<string, FakeChildDto>
        {
            ["not-a-ulid"] = new() { Name = "n", Value = 0 }
        };

        var result = dict.MapDictionaryToCollection(dest, mapper);

        Assert.Single(result);
        // Id is a freshly generated Ulid, not the zero/empty Ulid — guards against
        // multiple new entries colliding on the empty PK.
        Assert.NotEqual(default, result.First().Id);
    }

    [Fact(DisplayName = "Merge: entries missing from the dict are removed")]
    public void MissingEntries_AreRemoved()
    {
        var mapper = CreateMapper();
        var keepId = Ulid.NewUlid();
        var dropId = Ulid.NewUlid();
        var dest = new List<FakeChildEntity>
        {
            new() { Id = keepId, Name = "keep", Value = 1 },
            new() { Id = dropId, Name = "drop", Value = 2 },
        };

        var dict = new Dictionary<string, FakeChildDto>
        {
            [keepId.ToString()] = new() { Name = "keep", Value = 1 }
        };

        var result = dict.MapDictionaryToCollection(dest, mapper);

        Assert.Single(result);
        Assert.Equal(keepId, result.First().Id);
    }

    [Fact(DisplayName = "Merge: combined add + update + remove in one call")]
    public void Mixed_AddUpdateRemove()
    {
        var mapper = CreateMapper();
        var keepId = Ulid.NewUlid();
        var updateId = Ulid.NewUlid();
        var dropId = Ulid.NewUlid();
        var newId = Ulid.NewUlid();

        var keep = new FakeChildEntity { Id = keepId, Name = "keep", Value = 1 };
        var update = new FakeChildEntity { Id = updateId, Name = "old", Value = 2 };
        var drop = new FakeChildEntity { Id = dropId, Name = "drop", Value = 3 };

        var dest = new List<FakeChildEntity> { keep, update, drop };

        var dict = new Dictionary<string, FakeChildDto>
        {
            [keepId.ToString()] = new() { Name = "keep", Value = 1 },
            [updateId.ToString()] = new() { Name = "renamed", Value = 99 },
            [newId.ToString()] = new() { Name = "added", Value = 7 },
        };

        var result = dict.MapDictionaryToCollection(dest, mapper);

        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, e => e.Id == dropId);

        // kept entry is the same reference, unchanged
        Assert.Same(keep, result.Single(e => e.Id == keepId));
        Assert.Equal("keep", keep.Name);

        // updated entry is the SAME reference (EF tracking preserved), only fields changed
        Assert.Same(update, result.Single(e => e.Id == updateId));
        Assert.Equal("renamed", update.Name);
        Assert.Equal(99, update.Value);

        // new entry is a fresh entity with the Id from the dict key
        var added = result.Single(e => e.Id == newId);
        Assert.NotSame(keep, added);
        Assert.NotSame(update, added);
        Assert.Equal("added", added.Name);
        Assert.Equal(7, added.Value);
    }

    [Fact(DisplayName = "Merge: empty dict empties the destination")]
    public void EmptyDict_EmptiesDestination()
    {
        var mapper = CreateMapper();
        var dest = new List<FakeChildEntity>
        {
            new() { Name = "gone", Value = 1 },
            new() { Name = "also gone", Value = 2 },
        };

        var emptyDict = new Dictionary<string, FakeChildDto>();
        var merged = emptyDict.MapDictionaryToCollection(dest, mapper);

        Assert.Empty(merged);
    }
}
