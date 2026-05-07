using System.Net;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Utils;

namespace Noo.Api.Core.System.Email;

/// <summary>
/// Error Code: CANT_SEND_EMAIL
/// Name: Ошибка отправки письма
/// Description: Не удалось отправить письмо. Попробуйте позже или свяжитесь с поддержкой
/// </summary>
public class CantSendEmailException : NooException
{
    public CantSendEmailException(Exception? inner = null) : base(HttpStatusCode.ServiceUnavailable, "Unable to send email.")
    {
        Id = "CANT_SEND_EMAIL";
        IsInternal = true;
        LogId = RandomGenerator.GenerateReadableCode();

        if (inner is not null)
        {
            Payload = new
            {
                innerMessage = inner.Message,
                innerType = inner.GetType().Name,
                innerStackTrace = inner.StackTrace,
            };
        }
    }
}
