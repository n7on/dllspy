using System.Collections.Generic;
using System.Linq;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;
using DllSpy.Core.Tests.Fixtures;
using Xunit;

namespace DllSpy.Core.Tests.Services
{
    public class GrpcDiscoveryTests
    {
        private readonly List<GrpcOperation> _operations;

        public GrpcDiscoveryTests()
        {
            var analyzer = new AttributeAnalyzer();
            var discovery = new GrpcDiscovery(analyzer);
            var assembly = typeof(GreeterService).Assembly;
            var surfaces = discovery.Discover(assembly);
            _operations = surfaces.Cast<GrpcOperation>().ToList();
        }

        [Fact]
        public void Discovers_AllGrpcOperations()
        {
            // GreeterService:2 + OrderGrpcService:4 = 6
            Assert.Equal(6, _operations.Count);
        }

        [Fact]
        public void GreeterService_HasTwoMethods()
        {
            var ops = _operations.Where(o => o.ServiceName == "Greeter").ToList();
            Assert.Equal(2, ops.Count);
            Assert.Contains(ops, o => o.MethodName == "SayHello");
            Assert.Contains(ops, o => o.MethodName == "SayHellos");
        }

        [Fact]
        public void ServiceName_DerivedFromDeclaringType()
        {
            var op = _operations.First(o => o.MethodName == "SayHello");
            Assert.Equal("Greeter", op.ServiceName);
        }

        [Fact]
        public void ServiceName_DerivedFromNestedDeclaringType()
        {
            var op = _operations.First(o => o.MethodName == "GetOrder");
            Assert.Equal("OrderProto", op.ServiceName);
        }

        [Fact]
        public void UnaryMethod_Detected()
        {
            var op = _operations.First(o => o.MethodName == "SayHello");
            Assert.Equal(GrpcMethodType.Unary, op.MethodType);
        }

        [Fact]
        public void ServerStreamingMethod_Detected()
        {
            var op = _operations.First(o => o.MethodName == "SayHellos");
            Assert.Equal(GrpcMethodType.ServerStreaming, op.MethodType);
        }

        [Fact]
        public void ClientStreamingMethod_Detected()
        {
            var op = _operations.First(o => o.MethodName == "StreamOrders");
            Assert.Equal(GrpcMethodType.ClientStreaming, op.MethodType);
        }

        [Fact]
        public void BidiStreamingMethod_Detected()
        {
            var op = _operations.First(o => o.MethodName == "Chat");
            Assert.Equal(GrpcMethodType.BidiStreaming, op.MethodType);
        }

        [Fact]
        public void UnauthenticatedService_NoAuth()
        {
            var op = _operations.First(o => o.MethodName == "SayHello");
            Assert.False(op.RequiresAuthorization);
            Assert.False(op.AllowAnonymous);
        }

        [Fact]
        public void ClassLevelAuth_InheritedByMethods()
        {
            var op = _operations.First(o => o.MethodName == "GetOrder");
            Assert.True(op.RequiresAuthorization);
        }

        [Fact]
        public void MethodLevelAuth_HasRole()
        {
            var op = _operations.First(o => o.MethodName == "PlaceOrder");
            Assert.True(op.RequiresAuthorization);
            Assert.Contains("Admin", op.Roles);
        }

        [Fact]
        public void DisplayRoute_Format()
        {
            var op = _operations.First(o => o.MethodName == "SayHello");
            Assert.Equal("gRPC Greeter/SayHello", op.DisplayRoute);
        }

        [Fact]
        public void Parameters_ExcludeServerCallContext()
        {
            var op = _operations.First(o => o.MethodName == "SayHello");
            Assert.Single(op.Parameters);
            Assert.Equal("request", op.Parameters[0].Name);
        }

        [Fact]
        public void Parameters_ExcludeStreamWriterAndReader()
        {
            var op = _operations.First(o => o.MethodName == "Chat");
            Assert.Empty(op.Parameters);
        }
    }
}
