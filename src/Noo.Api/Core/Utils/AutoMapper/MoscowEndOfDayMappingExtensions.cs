using System.Reflection;
using AutoMapper;
using AutoMapper.Internal;

namespace Noo.Api.Core.Utils.AutoMapper;

public static class MoscowEndOfDayMappingExtensions
{
    /// <summary>
    /// Generic, model-agnostic convention: any DTO date member tagged with
    /// <see cref="MoscowEndOfDayAttribute"/> is normalized to the end of its day
    /// (Moscow time) when mapped onto a model. Keyed off the attribute on the
    /// source member, so it only fires for DTO -> model directions.
    /// </summary>
    public static void AddMoscowEndOfDayNormalization(this IMapperConfigurationExpression cfg)
    {
        cfg.Internal().ForAllPropertyMaps(
            map => map.SourceMember is PropertyInfo source
                && source.IsDefined(typeof(MoscowEndOfDayAttribute), inherit: false),
            (map, opt) => opt.MapFrom(
                new MoscowEndOfDayValueResolver((PropertyInfo)map.SourceMember!)
            )
        );
    }
}
