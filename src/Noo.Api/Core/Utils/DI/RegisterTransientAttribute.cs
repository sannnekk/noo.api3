namespace Noo.Api.Core.Utils.DI;

[AttributeUsage(AttributeTargets.Class)]
public class RegisterTransientAttribute : RegisterClassAttribute
{
    public RegisterTransientAttribute(Type? type = null) : base(ClassRegistrationScope.Transient, type) { }
}
