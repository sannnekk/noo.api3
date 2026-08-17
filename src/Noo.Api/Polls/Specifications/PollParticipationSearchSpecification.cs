using Ardalis.Specification;
using Noo.Api.Polls.Models;

namespace Noo.Api.Polls.Specifications;

/// <summary>
/// Pulls in the participant the results are read by and matches the search term
/// against them. Most of the searchable names sit on the related user, and CompareTo
/// silently drops property paths it cannot resolve on the filtered entity, so the
/// search cannot be expressed on
/// <see cref="Noo.Api.Polls.Filters.PollParticipationFilter"/>.
/// </summary>
public class PollParticipationSearchSpecification : Specification<PollParticipationModel>
{
    public PollParticipationSearchSpecification(string? search = null)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            Query.Where(participation =>
                (
                    participation.User != null
                    && (
                        participation.User.Name.ToLower().Contains(term)
                        || participation.User.Username.ToLower().Contains(term)
                        || (
                            participation.User.Email != null
                            && participation.User.Email.ToLower().Contains(term)
                        )
                        || (
                            participation.User.TelegramUsername != null
                            && participation.User.TelegramUsername.ToLower().Contains(term)
                        )
                    )
                )
                // Anonymous participants have no user, only the identifier they left.
                || (
                    participation.UserExternalIdentifier != null
                    && participation.UserExternalIdentifier.ToLower().Contains(term)
                )
            );
        }

        Query.Include(participation => participation.User);
    }
}
