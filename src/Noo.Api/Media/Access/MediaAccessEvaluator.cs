using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Media.Models;

namespace Noo.Api.Media.Access;

[RegisterScoped(typeof(IMediaAccessEvaluator))]
public class MediaAccessEvaluator : IMediaAccessEvaluator
{
    private readonly ICurrentUser _currentUser;
    private readonly IEnumerable<IMediaAccessRule> _rules;

    public MediaAccessEvaluator(ICurrentUser currentUser, IEnumerable<IMediaAccessRule> rules)
    {
        _currentUser = currentUser;
        _rules = rules;
    }

    public async Task EnsureCanAccessAsync(MediaModel media, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }

        var context = new MediaAccessContext(media, _currentUser);

        foreach (var rule in _rules.Where(r => r.Categories.Contains(media.Category)))
        {
            var decision = await rule.EvaluateAsync(context, cancellationToken);

            if (!decision.Allowed)
            {
                throw new ForbiddenException(decision.Reason ?? "Access denied");
            }
        }
    }
}
