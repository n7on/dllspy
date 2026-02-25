namespace DllSpy.Core.Contracts
{
    /// <summary>
    /// Represents an operation on a WCF service contract.
    /// </summary>
    public class WcfOperation : InputSurface
    {
        /// <inheritdoc />
        public override SurfaceType SurfaceType => SurfaceType.WcfOperation;

        /// <summary>Gets or sets the service contract interface name (e.g. "IOrderService").</summary>
        public string ContractName { get; set; }

        /// <summary>Gets or sets the namespace from [ServiceContract(Namespace=...)].</summary>
        public string ServiceNamespace { get; set; }

        /// <summary>Gets or sets whether this is a one-way operation from [OperationContract(IsOneWay=true)].</summary>
        public bool IsOneWay { get; set; }

        /// <inheritdoc />
        public override string DisplayRoute => $"WCF {ContractName}/{MethodName}";
    }
}
