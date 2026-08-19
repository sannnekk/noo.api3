using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext.Tiptap;

namespace Noo.Api.Core.Utils.Richtext;

/// <summary>
/// One stored rich text value, in whichever format the editor that wrote it uses.
/// </summary>
/// <remarks>
/// Stored and read back polymorphically: every value carries a "$type" naming its
/// format, so more than one can exist at a time. Tiptap (ProseMirror) is the only
/// one today — Quill's Delta was the other until its content was rewritten as
/// tiptap and it was removed.
/// <para>
/// Adding another editor is this much: a class implementing this interface with a
/// <c>TypeDiscriminator</c> of its own, and a <see cref="JsonDerivedTypeAttribute"/>
/// line here naming it. Existing rows keep their own discriminator and go on being
/// read as they were.
/// </para>
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(
    derivedType: typeof(TiptapRichText),
    typeDiscriminator: TiptapRichText.TypeDiscriminator
)]
public interface IRichTextType
{
    public bool IsEmpty();

    public int Length();
}
