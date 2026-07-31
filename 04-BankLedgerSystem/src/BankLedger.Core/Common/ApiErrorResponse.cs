
namespace BankLedger.Domain.Common
{
    public record ApiErrorResponse(
        String Title,
        Int32 StatusCode,
        String Detail
    );
}
