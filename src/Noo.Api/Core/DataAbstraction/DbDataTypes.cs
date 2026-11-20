namespace Noo.Api.Core.DataAbstraction;

public static class DbDataTypes
{
    // VARCHAR
    public const string Varchar15 = "VARCHAR(15)";
    public const string Varchar31 = "VARCHAR(31)";
    public const string Varchar63 = "VARCHAR(63)";
    public const string Varchar127 = "VARCHAR(127)";
    public const string Varchar255 = "VARCHAR(255)";
    public const string Varchar512 = "VARCHAR(512)";
    public const string Varchar1024 = "VARCHAR(1024)";

    public const string StringArray = "VARCHAR(512)";

    public const string BigStringArray = "VARCHAR(2048)";

    /// <summary>
    /// The TEXT type is a string with a maximum length of 65,535 characters.
    /// </summary>
    public const string Text = "TEXT";

    /// <summary>
    /// The MEDIUMTEXT type is a string with a maximum length of 16,777,215 characters.
    /// </summary>
    public const string MediumText = "MEDIUMTEXT";

    public const string Json = "JSON";

    public const string Ulid = "BINARY(16)";

    public const string Boolean = "TINYINT(1)";

    public const string DateTimeWithoutTZ = "DATETIME(0)";

    /// <summary>
    /// THe INT(11) type is a 4-byte integer with a maximum value of 2,147,483,647.
    /// </summary>
    public const string Int = "INT(11)";

    /// <summary>
    /// 1-byte unsigned integer, range 0–255
    /// </summary>
    public const string TinyIntUnsigned = "TINYINT UNSIGNED";

    /// <summary>
    /// 2-byte unsigned integer, range 0–65 535
    /// </summary>
    public const string SmallIntUnsigned = "SMALLINT UNSIGNED";

    /// <summary>
    /// 3-byte unsigned integer, range 0–16 777 215
    /// </summary>
    public const string MediumIntUnsigned = "MEDIUMINT UNSIGNED";

    /// <summary>
    /// 4-byte unsigned integer, range 0–4 294 967 295
    /// </summary>
    public const string BigIntUnsigned = "BIGINT UNSIGNED";

    public const string UserRolesEnum = "ENUM('student', 'mentor', 'assistant', 'teacher', 'admin')";
}
