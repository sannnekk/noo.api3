using System.ComponentModel.DataAnnotations;

namespace Noo.Api.Core.Config.Env;

public class DbConfig : IConfig
{
    public static string SectionName => "Db";

    [Required]
    public required string User { get; init; }

    [Required]
    public required string Password { get; init; }

    [Required]
    public required string Host { get; init; }

    [Required]
    public required string Port { get; init; }

    [Required]
    public required string Database { get; init; }

    [Required]
    public required int CommandTimeout { get; init; }

    [Required]
    public required string DefaultCharset { get; init; }

    [Required]
    public required string DefaultCollation { get; init; }

    public string ConnectionString => $"server={Host};port={Port};user={User};password={Password};database={Database};SslMode=Preferred;";
}
