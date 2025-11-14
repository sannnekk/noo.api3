namespace Noo.Api.Core.Utils.DI;

[AttributeUsage(AttributeTargets.Class)]
public class RegisterScopedAttribute : RegisterClassAttribute
{
    public RegisterScopedAttribute(Type? type = null) : base(ClassRegistrationScope.Scoped, type) { }
}
