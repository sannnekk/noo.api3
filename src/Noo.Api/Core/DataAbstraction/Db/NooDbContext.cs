using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.DataAbstraction.Model;

namespace Noo.Api.Core.DataAbstraction.Db;

public class NooDbContext : DbContext
{
    private readonly DbConfig _dbConfig;

    public NooDbContext(IOptions<DbConfig> dbConfig, DbContextOptions<NooDbContext> options) : base(options)
    {
        _dbConfig = dbConfig.Value;
    }

    public DbSet<T> GetDbSet<T>() where T : BaseModel
    {
        return Set<T>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet(_dbConfig.DefaultCharset);
        modelBuilder.UseCollation(_dbConfig.DefaultCollation);

        modelBuilder.RegisterModels();
        modelBuilder.UseOnModelCreatingExtensions();
        modelBuilder.ConfigureManyToManyTables();
        modelBuilder.UseRichTextColumns();
        modelBuilder.UseJsonDictionaryColumns();
        modelBuilder.UseUlidArrayColumns();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<Ulid>()
            .HaveConversion<UlidToBytesConverter>()
            .HaveColumnType("BINARY(16)");

        configurationBuilder.Properties<Ulid?>()
            .HaveConversion<NullableUlidToBytesConverter>()
            .HaveColumnType("BINARY(16)");
    }
}
