using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ElectrostoreAPI.Tests.Utils
{
    // Minimal local identity provider: AuthService talks to it through a plain HttpClient, so it cannot be mocked.
    public sealed class FakeOidcServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;

        public string Authority { get; }
        public int TokenStatusCode { get; set; } = 200;
        public string TokenBody { get; set; } = """{"access_token":"access","token_type":"Bearer","id_token":"id","scope":"openid"}""";
        public int UserInfoStatusCode { get; set; } = 200;
        public string UserInfoBody { get; set; } = """{"sub":"1","email":"sso@example.com","given_name":"Sso","family_name":"User","groups":[]}""";
        public string? LastTokenRequestBody { get; private set; }
        public string? LastUserInfoAuthorization { get; private set; }

        public FakeOidcServer()
        {
            var port = GetFreePort();
            Authority = $"http://127.0.0.1:{port}/application/o/authorize/";
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            _loop = Task.Run(Serve);
        }

        private static int GetFreePort()
        {
            var tcp = new TcpListener(IPAddress.Loopback, 0);
            tcp.Start();
            var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
            tcp.Stop();
            return port;
        }

        private async Task Serve()
        {
            while (!_cts.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch
                {
                    return;
                }
                var path = context.Request.Url!.AbsolutePath;
                int status;
                string body;
                if (path == "/application/o/token/")
                {
                    using var reader = new StreamReader(context.Request.InputStream);
                    LastTokenRequestBody = await reader.ReadToEndAsync();
                    (status, body) = (TokenStatusCode, TokenBody);
                }
                else if (path == "/application/o/userinfo/")
                {
                    LastUserInfoAuthorization = context.Request.Headers["Authorization"];
                    (status, body) = (UserInfoStatusCode, UserInfoBody);
                }
                else
                {
                    (status, body) = (404, "not found");
                }
                var bytes = Encoding.UTF8.GetBytes(body);
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Close();
            try { _loop.Wait(TimeSpan.FromSeconds(2)); } catch { /* listener closed while waiting */ }
            _cts.Dispose();
        }
    }
}
