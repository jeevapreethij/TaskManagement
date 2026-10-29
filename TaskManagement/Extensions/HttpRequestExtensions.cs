using Microsoft.AspNetCore.Http;

namespace TaskManagement.Extensions
{
    public static class HttpRequestExtensions
    {
        /// <summary>True for fetch/XHR calls that send X-Requested-With: XMLHttpRequest.</summary>
        public static bool IsAjaxRequest(this HttpRequest request) =>
            string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);
    }
}
