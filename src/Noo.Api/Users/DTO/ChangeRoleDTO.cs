using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Users.DTO;

public record ChangeRoleDTO
{
    /// <summary>
    /// The new role to assign to the user.
    /// </summary>
    [Required]
    [JsonPropertyName("newRole")]
    public UserRoles NewRole { get; init; }
}
