using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.Engines;

public interface IVideoEngineResolver
{
    public IVideoEngine Resolve(NooTubeServiceType serviceType);
}
