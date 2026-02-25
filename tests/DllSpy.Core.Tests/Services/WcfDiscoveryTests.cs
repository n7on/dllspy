using System.Collections.Generic;
using System.Linq;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;
using DllSpy.Core.Tests.Fixtures;
using Xunit;

namespace DllSpy.Core.Tests.Services
{
    public class WcfDiscoveryTests
    {
        private readonly List<WcfOperation> _operations;

        public WcfDiscoveryTests()
        {
            var analyzer = new AttributeAnalyzer();
            var discovery = new WcfDiscovery(analyzer);
            var assembly = typeof(IOrderService).Assembly;
            var surfaces = discovery.Discover(assembly);
            _operations = surfaces.Cast<WcfOperation>().ToList();
        }

        [Fact]
        public void Discovers_AllOperations()
        {
            // IOrderService:3 + ISecureService:2 + IAuditService:1 = 6
            Assert.Equal(6, _operations.Count);
        }

        [Fact]
        public void OrderService_HasThreeOperations()
        {
            var ops = _operations.Where(o => o.ContractName == "IOrderService").ToList();
            Assert.Equal(3, ops.Count);
            Assert.Contains(ops, o => o.MethodName == "GetOrder");
            Assert.Contains(ops, o => o.MethodName == "PlaceOrder");
            Assert.Contains(ops, o => o.MethodName == "NotifyShipped");
        }

        [Fact]
        public void ContractName_IsInterfaceName()
        {
            var op = _operations.First(o => o.MethodName == "GetOrder");
            Assert.Equal("IOrderService", op.ContractName);
        }

        [Fact]
        public void ServiceNamespace_ExtractedFromAttribute()
        {
            var op = _operations.First(o => o.ContractName == "IOrderService");
            Assert.Equal("http://example.com/orders", op.ServiceNamespace);
        }

        [Fact]
        public void ServiceNamespace_NullWhenNotSpecified()
        {
            var op = _operations.First(o => o.ContractName == "ISecureService");
            Assert.Null(op.ServiceNamespace);
        }

        [Fact]
        public void IsOneWay_DetectedFromAttribute()
        {
            var notify = _operations.First(o => o.MethodName == "NotifyShipped");
            Assert.True(notify.IsOneWay);

            var get = _operations.First(o => o.MethodName == "GetOrder");
            Assert.False(get.IsOneWay);
        }

        [Fact]
        public void ClassLevelAuth_InheritedByOperations()
        {
            var getStatus = _operations.First(o => o.ContractName == "ISecureService" && o.MethodName == "GetStatus");
            Assert.True(getStatus.RequiresAuthorization);
        }

        [Fact]
        public void MethodLevelAuth_HasRole()
        {
            var updateConfig = _operations.First(o => o.ContractName == "ISecureService" && o.MethodName == "UpdateConfig");
            Assert.True(updateConfig.RequiresAuthorization);
            Assert.Contains("Admin", updateConfig.Roles);
        }

        [Fact]
        public void ContractOnly_FallsBackToInterfaceName()
        {
            var audit = _operations.First(o => o.ContractName == "IAuditService");
            Assert.Equal("IAuditService", audit.ClassName);
        }

        [Fact]
        public void ContractOnly_NoAuth()
        {
            var audit = _operations.First(o => o.ContractName == "IAuditService");
            Assert.False(audit.RequiresAuthorization);
            Assert.False(audit.AllowAnonymous);
        }

        [Fact]
        public void DisplayRoute_Format()
        {
            var op = _operations.First(o => o.MethodName == "GetOrder");
            Assert.Equal("WCF IOrderService/GetOrder", op.DisplayRoute);
        }

        [Fact]
        public void Parameters_Extracted()
        {
            var placeOrder = _operations.First(o => o.MethodName == "PlaceOrder");
            Assert.Equal(2, placeOrder.Parameters.Count);
            Assert.Contains(placeOrder.Parameters, p => p.Name == "product");
            Assert.Contains(placeOrder.Parameters, p => p.Name == "quantity");
        }

        [Fact]
        public void ImplementedService_UsesClassName()
        {
            var op = _operations.First(o => o.ContractName == "IOrderService");
            Assert.Equal("OrderService", op.ClassName);
        }
    }
}
