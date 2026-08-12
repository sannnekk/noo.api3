using System.Net;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Polls.Exceptions;

/// <summary>
/// Error Code: INVALID_POLL_ANSWER
/// Name: Некорректный ответ
/// Description: Ответ не соответствует настройкам вопроса
/// </summary>
public class InvalidPollAnswerException : NooException
{
    public InvalidPollAnswerException(string message = "The answer does not match the question it belongs to.")
        : base(message)
    {
        Id = "INVALID_POLL_ANSWER";
        StatusCode = HttpStatusCode.BadRequest;
    }
}
