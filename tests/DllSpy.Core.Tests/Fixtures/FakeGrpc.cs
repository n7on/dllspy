using System;
using System.Threading.Tasks;

namespace DllSpy.Core.Tests.Fixtures
{
    // Mimics Grpc.Core.ServerCallContext
    public class ServerCallContext { }

    // Mimics Grpc.Core.IServerStreamWriter<T>
    public interface IServerStreamWriter<T> { }

    // Mimics Grpc.Core.IAsyncStreamReader<T>
    public interface IAsyncStreamReader<T> { }

    // --- Generated gRPC base class pattern (nested type with BindService) ---

    public static class Greeter
    {
        public static object BindService(GreeterBase service) => null;

        public abstract class GreeterBase
        {
            public virtual Task<string> SayHello(string request, ServerCallContext context)
                => Task.FromResult("");

            public virtual Task SayHellos(string request, IServerStreamWriter<string> responseStream, ServerCallContext context)
                => Task.CompletedTask;
        }
    }

    public static class OrderProto
    {
        public static object BindService(OrderServiceBase service) => null;

        public abstract class OrderServiceBase
        {
            public virtual Task<string> GetOrder(string request, ServerCallContext context)
                => Task.FromResult("");

            public virtual Task<string> PlaceOrder(string request, ServerCallContext context)
                => Task.FromResult("");

            public virtual Task<string> StreamOrders(IAsyncStreamReader<string> requestStream, ServerCallContext context)
                => Task.FromResult("");

            public virtual Task Chat(IAsyncStreamReader<string> requestStream, IServerStreamWriter<string> responseStream, ServerCallContext context)
                => Task.CompletedTask;
        }
    }
}
