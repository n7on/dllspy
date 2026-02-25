using System;

namespace DllSpy.Core.Tests.Fixtures
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ServiceContractAttribute : Attribute
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class OperationContractAttribute : Attribute
    {
        public bool IsOneWay { get; set; }
        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceBehaviorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class PrincipalPermissionAttribute : Attribute
    {
        public string Role { get; set; }
        public string Name { get; set; }
    }
}
