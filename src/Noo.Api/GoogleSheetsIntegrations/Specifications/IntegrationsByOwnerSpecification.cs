using Ardalis.Specification;
using Noo.Api.GoogleSheetsIntegrations.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Specifications;

/// <summary>
/// Narrows a listing to the integrations a single user created — which is all anyone ever
/// sees of them, whatever their role.
/// </summary>
public class IntegrationsByOwnerSpecification : Specification<GoogleSheetsIntegrationModel>
{
    public IntegrationsByOwnerSpecification(Ulid ownerId)
    {
        Query.Where(integration => integration.OwnerId == ownerId);
    }
}
