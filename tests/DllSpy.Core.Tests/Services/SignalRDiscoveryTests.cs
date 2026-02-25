using System.Collections.Generic;
using System.Linq;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;
using DllSpy.Core.Tests.Fixtures;
using Xunit;

namespace DllSpy.Core.Tests.Services
{
    public class SignalRDiscoveryTests
    {
        private readonly List<SignalRMethod> _methods;

        public SignalRDiscoveryTests()
        {
            var analyzer = new AttributeAnalyzer();
            var discovery = new SignalRDiscovery(analyzer);
            var assembly = typeof(ChatHub).Assembly;
            var surfaces = discovery.Discover(assembly);
            _methods = surfaces.Cast<SignalRMethod>().ToList();
        }

        [Fact]
        public void Discovers_AllHubMethods()
        {
            // ChatHub:2 + NotificationHub:2 + LifecycleHub:1 (lifecycle methods excluded)
            Assert.Equal(5, _methods.Count);
        }

        [Fact]
        public void ChatHub_HasTwoMethods()
        {
            var chat = _methods.Where(m => m.HubName == "ChatHub").ToList();
            Assert.Equal(2, chat.Count);
            Assert.Contains(chat, m => m.MethodName == "SendMessage");
            Assert.Contains(chat, m => m.MethodName == "JoinRoom");
        }

        [Fact]
        public void LifecycleMethods_AreExcluded()
        {
            Assert.DoesNotContain(_methods, m => m.MethodName == "OnConnectedAsync");
            Assert.DoesNotContain(_methods, m => m.MethodName == "OnDisconnectedAsync");
            Assert.DoesNotContain(_methods, m => m.MethodName == "Dispose");
        }

        [Fact]
        public void LifecycleHub_OnlyIncludesNonLifecycleMethods()
        {
            var lifecycle = _methods.Where(m => m.HubName == "LifecycleHub").ToList();
            Assert.Single(lifecycle);
            Assert.Equal("SendPing", lifecycle[0].MethodName);
        }

        [Fact]
        public void ConventionalRoute_StripsHubSuffixAndLowercases()
        {
            var chat = _methods.First(m => m.HubName == "ChatHub");
            Assert.Equal("chat", chat.HubRoute);

            var notification = _methods.First(m => m.HubName == "NotificationHub");
            Assert.Equal("notification", notification.HubRoute);
        }

        [Fact]
        public void HubLevelAuth_InheritedByMethods()
        {
            var subscribe = _methods.First(m => m.HubName == "NotificationHub" && m.MethodName == "Subscribe");
            Assert.True(subscribe.RequiresAuthorization);

            var broadcast = _methods.First(m => m.HubName == "NotificationHub" && m.MethodName == "Broadcast");
            Assert.True(broadcast.RequiresAuthorization);
            Assert.Contains("Admin", broadcast.Roles);
        }

        [Fact]
        public void UnauthenticatedHub_MethodsAreUnprotected()
        {
            var sendMessage = _methods.First(m => m.HubName == "ChatHub" && m.MethodName == "SendMessage");
            Assert.False(sendMessage.RequiresAuthorization);
            Assert.False(sendMessage.AllowAnonymous);
        }

        [Fact]
        public void GenericHub_IsDetected()
        {
            var notification = _methods.Where(m => m.HubName == "NotificationHub").ToList();
            Assert.Equal(2, notification.Count);
        }
    }
}
