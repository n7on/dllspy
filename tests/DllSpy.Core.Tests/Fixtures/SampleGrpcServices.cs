using System.Threading.Tasks;

namespace DllSpy.Core.Tests.Fixtures
{
    // --- Unauthenticated gRPC service ---

    public class GreeterService : Greeter.GreeterBase
    {
        public override Task<string> SayHello(string request, ServerCallContext context)
            => Task.FromResult("Hello");

        public override Task SayHellos(string request, IServerStreamWriter<string> responseStream, ServerCallContext context)
            => Task.CompletedTask;
    }

    // --- Authenticated gRPC service with roles ---

    [Authorize]
    public class OrderGrpcService : OrderProto.OrderServiceBase
    {
        public override Task<string> GetOrder(string request, ServerCallContext context)
            => Task.FromResult("order");

        [Authorize(Roles = "Admin")]
        public override Task<string> PlaceOrder(string request, ServerCallContext context)
            => Task.FromResult("placed");

        public override Task<string> StreamOrders(IAsyncStreamReader<string> requestStream, ServerCallContext context)
            => Task.FromResult("streamed");

        public override Task Chat(IAsyncStreamReader<string> requestStream, IServerStreamWriter<string> responseStream, ServerCallContext context)
            => Task.CompletedTask;
    }
}
