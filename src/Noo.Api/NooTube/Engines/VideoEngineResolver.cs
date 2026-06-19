using Noo.Api.Core.Utils.DI;
using Noo.Api.NooTube.Exceptions;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.Engines;

[RegisterScoped(typeof(IVideoEngineResolver))]
public class VideoEngineResolver : IVideoEngineResolver
{
    private readonly IReadOnlyDictionary<NooTubeServiceType, IVideoEngine> _engines;

    public VideoEngineResolver(IEnumerable<IVideoEngine> engines)
    {
        _engines = engines.ToDictionary(engine => engine.ServiceType);
    }

    public IVideoEngine Resolve(NooTubeServiceType serviceType)
    {
        if (_engines.TryGetValue(serviceType, out var engine))
        {
            return engine;
        }

        throw new UnsupportedVideoEngineException(
            $"No video engine is registered for service type '{serviceType}'."
        );
    }
}
