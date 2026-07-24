using Grpc.Core;

namespace ElectrostoreIA.Tests.Utils
{
    // Minimal hand-rolled substitute for Grpc.Core.Testing.TestServerCallContext (not referenced by this project).
    public class TestServerCallContext : ServerCallContext
    {
        private readonly CancellationToken _cancellationToken;

        private TestServerCallContext(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public static TestServerCallContext Create(CancellationToken cancellationToken = default)
        {
            return new TestServerCallContext(cancellationToken);
        }

        protected override string MethodCore => "TestMethod";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "test-peer";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => new Metadata();
        protected override CancellationToken CancellationTokenCore => _cancellationToken;
        protected override Metadata ResponseTrailersCore => new Metadata();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions WriteOptionsCore { get; set; } = new WriteOptions();
        protected override AuthContext AuthContextCore => new AuthContext(string.Empty, new Dictionary<string, List<AuthProperty>>());

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
        {
            return null!;
        }

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
        {
            return Task.CompletedTask;
        }
    }
}
