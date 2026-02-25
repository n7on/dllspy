namespace DllSpy.Core.Contracts
{
    /// <summary>
    /// Represents an operation on a gRPC service.
    /// </summary>
    public class GrpcOperation : InputSurface
    {
        /// <inheritdoc />
        public override SurfaceType SurfaceType => SurfaceType.GrpcOperation;

        /// <summary>Gets or sets the gRPC service name (e.g. "GreeterService").</summary>
        public string ServiceName { get; set; }

        /// <summary>Gets or sets the gRPC method type (Unary, ServerStreaming, ClientStreaming, BidiStreaming).</summary>
        public GrpcMethodType MethodType { get; set; }

        /// <inheritdoc />
        public override string DisplayRoute => $"gRPC {ServiceName}/{MethodName}";
    }

    /// <summary>
    /// Identifies the streaming mode of a gRPC method.
    /// </summary>
    public enum GrpcMethodType
    {
        /// <summary>Simple request-response.</summary>
        Unary,
        /// <summary>Server streams multiple responses.</summary>
        ServerStreaming,
        /// <summary>Client streams multiple requests.</summary>
        ClientStreaming,
        /// <summary>Both sides stream.</summary>
        BidiStreaming
    }
}
