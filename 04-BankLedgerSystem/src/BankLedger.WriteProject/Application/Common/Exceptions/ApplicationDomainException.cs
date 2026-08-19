namespace BankLedger.WriteProject.Application.Common.Exceptions
{
    public class ApplicationDomainException : Exception
    {
        public string Title { get; }
        public int StatusCode { get; }

        public ApplicationDomainException(string title, string detail, int statusCode)
            : base(detail)
        {
            Title = title;
            StatusCode = statusCode;
        }
    }
}
