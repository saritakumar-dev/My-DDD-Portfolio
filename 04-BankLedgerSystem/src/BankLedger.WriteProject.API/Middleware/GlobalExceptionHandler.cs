using BankLedger.Domain.Common;
using BankLedger.WriteProject.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace BankLedger.WriteProject.API.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var response = exception switch
            {
                ApplicationDomainException appEx => new ApiErrorResponse(
                    Title: appEx.Title,
                    StatusCode: appEx.StatusCode,
                    Detail: appEx.Message
                ),
                _ => new ApiErrorResponse(
                    Title: "An unexpected error occurred",
                    StatusCode: StatusCodes.Status500InternalServerError,
                    Detail: "A critical backend system fault occurred."
                )
            };

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = response.StatusCode;

            await httpContext.Response.WriteAsJsonAsync(
                  response,
                  cancellationToken: cancellationToken
            );

            return true;
        }
    }
}
