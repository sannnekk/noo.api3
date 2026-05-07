using Noo.Api.Core.Security.Authorization;
using Noo.Api.Media.Models;

namespace Noo.Api.Media.Access;

public sealed record MediaAccessContext(MediaModel Media, ICurrentUser User);
