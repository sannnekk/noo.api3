using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Core.Utils.Richtext.Delta;

[JsonDerivedType(
    derivedType: typeof(DeltaRichText),
    typeDiscriminator: TypeDiscriminator
)]
public class DeltaRichText : IRichTextType
{
    public const string TypeDiscriminator = "delta";

    [Required]
    [MinLength(1)]
    [JsonPropertyName("ops")]
    public List<DeltaOp> Ops { get; set; } = [];

    /// <summary>
    /// Default constructor for DeltaRichText.
    /// Initializes the Ops property with an empty DeltaOp.
    /// </summary>
    public DeltaRichText()
    {
        Ops = [DeltaOp.Empty()];
    }

    /// <summary>
    /// Creates a DeltaRichText object from a string.
    /// </summary>
    public static DeltaRichText FromString(string? delta)
    {
        return new DeltaRichText()
        {
            Ops = [new DeltaOp() {
                Insert = delta ?? string.Empty + "\n"
            }]
        };
    }

    public bool IsEmpty()
    {
        return Length() == 0;
    }

    public int Length()
    {
        return Ops.Sum(op => op.HasInsert ? op.Insert?.ToString()?.Length ?? 0 : 0);
    }

    public override string ToString()
    {
        return string.Concat(Ops
            .Where(op => op.HasInsert)
            .Select(op => op.Insert?.ToString() ?? string.Empty));
    }
}
