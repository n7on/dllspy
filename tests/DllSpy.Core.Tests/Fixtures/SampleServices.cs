using System.Threading.Tasks;

namespace DllSpy.Core.Tests.Fixtures
{
    // --- Standard WCF service: no auth, 3 operations ---

    [ServiceContract(Namespace = "http://example.com/orders")]
    public interface IOrderService
    {
        [OperationContract]
        Task<string> GetOrder(int orderId);

        [OperationContract]
        Task PlaceOrder(string product, int quantity);

        [OperationContract(IsOneWay = true)]
        void NotifyShipped(int orderId);
    }

    public class OrderService : IOrderService
    {
        public Task<string> GetOrder(int orderId) => Task.FromResult("order");
        public Task PlaceOrder(string product, int quantity) => Task.CompletedTask;
        public void NotifyShipped(int orderId) { }
    }

    // --- Secured WCF service: class-level + method-level PrincipalPermission ---

    [ServiceContract]
    public interface ISecureService
    {
        [OperationContract]
        string GetStatus();

        [OperationContract]
        void UpdateConfig(string key, string value);
    }

    [PrincipalPermission]
    public class SecureService : ISecureService
    {
        public string GetStatus() => "ok";

        [PrincipalPermission(Role = "Admin")]
        public void UpdateConfig(string key, string value) { }
    }

    // --- Contract-only: no implementation class ---

    [ServiceContract(Namespace = "http://example.com/audit")]
    public interface IAuditService
    {
        [OperationContract]
        void LogEvent(string eventName);
    }
}
