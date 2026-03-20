using Microsoft.AspNetCore.Components;

namespace Noo.Api.Core.System.Email;

public struct Email<TData, TTemplateComponent> where TTemplateComponent : IComponent where TData : class
{
    public required string ToEmail { get; set; }

    public required string ToName { get; set; }

    public required string FromEmail { get; set; }

    public required string FromName { get; set; }

    public required string Subject { get; set; }

    public TData Body { get; set; }
}
