using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.Core.Utils.Richtext.Tiptap;

namespace Noo.Api.Core.Utils.Richtext;

/// <summary>
/// Represents a rich text type for use in rich text fields.
/// </summary>
/// <remarks>
/// This interface is used to define different rich text formats that can be
/// serialized and deserialized using polymorphic JSON serialization.
/// It supports the Delta (Quill) and Tiptap (ProseMirror) rich text formats.
/// The type discriminator property is "$type"
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(
    derivedType: typeof(DeltaRichText),
    typeDiscriminator: DeltaRichText.TypeDiscriminator
)]
[JsonDerivedType(
    derivedType: typeof(TiptapRichText),
    typeDiscriminator: TiptapRichText.TypeDiscriminator
)]
public interface IRichTextType
{
    public bool IsEmpty();

    public int Length();
}
