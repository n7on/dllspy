namespace DllSpy.Core.Contracts
{
    /// <summary>
    /// Represents a callable method on a SignalR hub.
    /// </summary>
    public class SignalRMethod : InputSurface
    {
        /// <inheritdoc />
        public override SurfaceType SurfaceType => SurfaceType.SignalRMethod;

        /// <summary>Gets or sets the conventional hub route (e.g. "chat" for ChatHub).</summary>
        public string HubRoute { get; set; }

        /// <summary>Gets or sets the hub class name.</summary>
        public string HubName { get; set; }

        /// <summary>Gets or sets whether the method returns a streaming result (IAsyncEnumerable, ChannelReader).</summary>
        public bool IsStreamingResult { get; set; }

        /// <summary>Gets or sets whether the method accepts a streaming parameter.</summary>
        public bool AcceptsStreaming { get; set; }

        /// <inheritdoc />
        public override string DisplayRoute => $"WS {HubRoute}/{MethodName}";
    }
}
