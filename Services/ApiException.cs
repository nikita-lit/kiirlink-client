using System.Net;

namespace KiirLink.Services;

public class ApiException : Exception
{
    public ApiException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}

public sealed class NetworkUnavailableException : ApiException
{
    public NetworkUnavailableException(Exception? innerException = null)
        : base("No internet connection. Check your network and try again.", null, innerException)
    {
    }
}
