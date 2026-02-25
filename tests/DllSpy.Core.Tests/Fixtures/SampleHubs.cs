using System;
using System.Threading.Tasks;

namespace DllSpy.Core.Tests.Fixtures
{
    public class ChatHub : Hub
    {
        public Task SendMessage(string user, string message) => Task.CompletedTask;
        public Task JoinRoom(string roomName) => Task.CompletedTask;
    }

    public interface INotificationClient { }

    [Authorize]
    public class NotificationHub : Hub<INotificationClient>
    {
        public Task Subscribe(string topic) => Task.CompletedTask;

        [Authorize(Roles = "Admin")]
        public Task Broadcast(string message) => Task.CompletedTask;
    }

    public class LifecycleHub : Hub
    {
        public Task OnConnectedAsync() => Task.CompletedTask;
        public Task OnDisconnectedAsync(Exception exception) => Task.CompletedTask;
        public void Dispose() { }
        public Task SendPing() => Task.CompletedTask;
    }
}
