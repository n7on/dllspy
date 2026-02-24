using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spy.Core.Contracts;

namespace Spy.Core.Services
{
    /// <summary>
    /// Resolves merged security attributes from a class and its method.
    /// </summary>
    internal class SecurityResolver
    {
        private readonly AttributeAnalyzer _analyzer;

        public SecurityResolver(AttributeAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        /// <summary>
        /// Reads security attributes from a class (controller/hub) level.
        /// </summary>
        public ClassSecurity ReadClass(MemberInfo classType)
        {
            return new ClassSecurity
            {
                HasAuth = _analyzer.HasAuthorizeAttribute(classType),
                AllowAnon = _analyzer.HasAllowAnonymousAttribute(classType),
                Roles = _analyzer.GetRoles(classType),
                Policies = _analyzer.GetPolicies(classType),
                SecurityAttributes = _analyzer.GetSecurityAttributes(classType)
            };
        }

        /// <summary>
        /// Merges class-level and method-level security into a resolved result.
        /// </summary>
        public MergedSecurity Merge(ClassSecurity classSec, MethodInfo method)
        {
            var methodHasAuth = _analyzer.HasAuthorizeAttribute(method);
            var methodAllowAnon = _analyzer.HasAllowAnonymousAttribute(method);
            var methodRoles = _analyzer.GetRoles(method);
            var methodPolicies = _analyzer.GetPolicies(method);
            var methodSecurityAttrs = _analyzer.GetSecurityAttributes(method);

            var allRoles = new List<string>(classSec.Roles);
            allRoles.AddRange(methodRoles);

            var allPolicies = new List<string>(classSec.Policies);
            allPolicies.AddRange(methodPolicies);

            var allSecurityAttrs = new List<SecurityAttribute>(classSec.SecurityAttributes);
            allSecurityAttrs.AddRange(methodSecurityAttrs);

            return new MergedSecurity
            {
                RequiresAuthorization = methodHasAuth || (classSec.HasAuth && !methodAllowAnon),
                AllowAnonymous = methodAllowAnon || (classSec.AllowAnon && !methodHasAuth),
                Roles = allRoles.Distinct().ToList(),
                Policies = allPolicies.Distinct().ToList(),
                SecurityAttributes = allSecurityAttrs
            };
        }

        internal class ClassSecurity
        {
            public bool HasAuth { get; set; }
            public bool AllowAnon { get; set; }
            public List<string> Roles { get; set; }
            public List<string> Policies { get; set; }
            public List<SecurityAttribute> SecurityAttributes { get; set; }
        }

        internal class MergedSecurity
        {
            public bool RequiresAuthorization { get; set; }
            public bool AllowAnonymous { get; set; }
            public List<string> Roles { get; set; }
            public List<string> Policies { get; set; }
            public List<SecurityAttribute> SecurityAttributes { get; set; }
        }
    }
}
