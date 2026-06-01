using System.Reflection;
using AutoMapper;

namespace Noo.Api.Core.Utils.AutoMapper;

/// <summary>
/// Resolves a date member marked with <see cref="MoscowEndOfDayAttribute"/> to the
/// end of its day (Moscow time), reading the value straight from the source member.
/// Wired up generically for every such member in the AutoMapper configuration.
/// </summary>
public sealed class MoscowEndOfDayValueResolver : IValueResolver<object, object, object?>
{
    private readonly PropertyInfo _sourceMember;

    public MoscowEndOfDayValueResolver(PropertyInfo sourceMember)
    {
        _sourceMember = sourceMember;
    }

    public object? Resolve(object source, object destination, object? destMember, ResolutionContext context)
    {
        return _sourceMember.GetValue(source) is DateTime value ? Clock.EndOfDay(value) : null;
    }
}
