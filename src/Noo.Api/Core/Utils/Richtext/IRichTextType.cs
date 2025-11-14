using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext.Delta;

namespace Noo.Api.Core.Utils.Richtext;

/// <summary>
/// Represents a rich text type for use in rich text fields.
/// </summary>
/// <remarks>
/// This interface is used to define different rich text formats that can be
/// serialized and deserialized using polymorphic JSON serialization.
/// Currently, it includes only support for Delta rich text format.
/// The type discriminator property is "$type"
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(
    derivedType: typeof(DeltaRichText),
    typeDiscriminator: DeltaRichText.TypeDiscriminator
)]
public interface IRichTextType
{
    public bool IsEmpty();

    public int Length();
}
