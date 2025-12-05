using Microsoft.AspNetCore.Http;
using Moq;
using System.Net;
using System.Security.Claims;

namespace electrostore.Tests.Utils
{
    public static class HttpContextAccessorMockFactory
    {
        public static Mock<IHttpContextAccessor> Create(
            Dictionary<string, string>? claims = null,
            Dictionary<string, string>? headers = null,
            string? remoteIp = null)
        {
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();

            // Setup claims
            if (claims != null && claims.Any())
            {
                var claimsList = claims.Select(c => new Claim(c.Key, c.Value)).ToList();
                var identity = new ClaimsIdentity(claimsList, "TestAuthType");
                var claimsPrincipal = new ClaimsPrincipal(identity);
                context.User = claimsPrincipal;
            }

            // Setup headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    context.Request.Headers[header.Key] = header.Value;
                }
            }

            // Setup remote IP address
            if (!string.IsNullOrEmpty(remoteIp))
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
            }

            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);

            return mockHttpContextAccessor;
        }
    }
}
