namespace Noo.Api.Core.Utils.AutoMapper;

/// <summary>
/// Marks a DTO date property as a day-granular value: the user only chooses a
/// date, and it is taken as the end of that day (23:59:59) in Moscow time.
///
/// When such a DTO is mapped onto a model, the value is normalized via
/// <see cref="Clock.EndOfDay(DateTime)"/>. The mechanism is generic and lives in
/// the mapping layer (see AutoMapper configuration) — it keys off this attribute,
/// not off any specific model.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MoscowEndOfDayAttribute : Attribute
{
}
