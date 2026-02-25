using DllSpy.Core.Contracts;
using DllSpy.Core.Services;
using DllSpy.Core.Tests.Fixtures;
using Xunit;

namespace DllSpy.Core.Tests.Services
{
    public class AttributeAnalyzerTests
    {
        private readonly AttributeAnalyzer _analyzer = new AttributeAnalyzer();

        [Fact]
        public void HasAuthorizeAttribute_DetectsOnClass()
        {
            Assert.True(_analyzer.HasAuthorizeAttribute(typeof(AdminController)));
        }

        [Fact]
        public void HasAuthorizeAttribute_ReturnsFalseWhenAbsent()
        {
            Assert.False(_analyzer.HasAuthorizeAttribute(typeof(PublicController)));
        }

        [Fact]
        public void HasAuthorizeAttribute_DetectsOnMethod()
        {
            var method = typeof(UsersController).GetMethod("Update");
            Assert.True(_analyzer.HasAuthorizeAttribute(method));
        }

        [Fact]
        public void HasAllowAnonymousAttribute_DetectsOnMethod()
        {
            var method = typeof(AdminController).GetMethod("HealthCheck");
            Assert.True(_analyzer.HasAllowAnonymousAttribute(method));
        }

        [Fact]
        public void HasAllowAnonymousAttribute_ReturnsFalseWhenAbsent()
        {
            var method = typeof(UsersController).GetMethod("GetAll");
            Assert.False(_analyzer.HasAllowAnonymousAttribute(method));
        }

        [Fact]
        public void GetRoles_ExtractsRolesFromAttribute()
        {
            var method = typeof(UsersController).GetMethod("Delete");
            var roles = _analyzer.GetRoles(method);
            Assert.Contains("Admin", roles);
        }

        [Fact]
        public void GetPolicies_ExtractsPoliciesFromAttribute()
        {
            var policies = _analyzer.GetPolicies(typeof(AdminController));
            Assert.Contains("AdminPolicy", policies);
        }

        [Fact]
        public void GetRoles_ReturnsEmptyWhenNoRoles()
        {
            var method = typeof(UsersController).GetMethod("Update");
            var roles = _analyzer.GetRoles(method);
            Assert.Empty(roles);
        }

        [Fact]
        public void GetHttpMethod_ReturnsGet()
        {
            var method = typeof(UsersController).GetMethod("GetAll");
            Assert.Equal("GET", _analyzer.GetHttpMethod(method));
        }

        [Fact]
        public void GetHttpMethod_ReturnsPost()
        {
            var method = typeof(UsersController).GetMethod("Create");
            Assert.Equal("POST", _analyzer.GetHttpMethod(method));
        }

        [Fact]
        public void GetHttpMethod_ReturnsPut()
        {
            var method = typeof(UsersController).GetMethod("Update");
            Assert.Equal("PUT", _analyzer.GetHttpMethod(method));
        }

        [Fact]
        public void GetHttpMethod_ReturnsDelete()
        {
            var method = typeof(UsersController).GetMethod("Delete");
            Assert.Equal("DELETE", _analyzer.GetHttpMethod(method));
        }

        [Fact]
        public void GetHttpMethod_ReturnsNullWhenNoAttribute()
        {
            var method = typeof(PlainController).GetMethod("Index");
            Assert.Null(_analyzer.GetHttpMethod(method));
        }

        [Fact]
        public void GetRouteTemplate_ReturnsControllerRoute()
        {
            var template = _analyzer.GetRouteTemplate(typeof(UsersController));
            Assert.Equal("api/[controller]", template);
        }

        [Fact]
        public void GetRouteTemplate_ReturnsActionTemplate()
        {
            var method = typeof(UsersController).GetMethod("GetById");
            var template = _analyzer.GetRouteTemplate(method);
            Assert.Equal("{id}", template);
        }

        [Fact]
        public void GetRouteTemplate_ReturnsNullWhenAbsent()
        {
            var method = typeof(PlainController).GetMethod("Index");
            Assert.Null(_analyzer.GetRouteTemplate(method));
        }

        [Fact]
        public void GetSecurityAttributes_ReturnsAuthorizeWithRoles()
        {
            var method = typeof(UsersController).GetMethod("Delete");
            var attrs = _analyzer.GetSecurityAttributes(method);
            Assert.Single(attrs);
            Assert.Equal("AuthorizeAttribute", attrs[0].AttributeName);
            Assert.Contains("Admin", attrs[0].Roles);
        }

        [Fact]
        public void GetParameterSource_ReturnsBody()
        {
            var param = typeof(UsersController).GetMethod("Create").GetParameters()[0];
            Assert.Equal(ParameterSource.Body, _analyzer.GetParameterSource(param));
        }

        [Fact]
        public void GetParameterSource_ReturnsRoute()
        {
            var param = typeof(UsersController).GetMethod("GetById").GetParameters()[0];
            Assert.Equal(ParameterSource.Route, _analyzer.GetParameterSource(param));
        }

        [Fact]
        public void GetParameterSource_ReturnsUnknownWhenNoAttribute()
        {
            var param = typeof(PlainController).GetMethod("Details").GetParameters()[0];
            Assert.Equal(ParameterSource.Unknown, _analyzer.GetParameterSource(param));
        }

        [Fact]
        public void HasAuthorizeAttribute_DetectsPrincipalPermission()
        {
            Assert.True(_analyzer.HasAuthorizeAttribute(typeof(SecureService)));
        }

        [Fact]
        public void GetRoles_ExtractsRoleFromPrincipalPermission()
        {
            var method = typeof(SecureService).GetMethod("UpdateConfig");
            var roles = _analyzer.GetRoles(method);
            Assert.Contains("Admin", roles);
        }

        [Fact]
        public void GetSecurityAttributes_IncludesPrincipalPermission()
        {
            var attrs = _analyzer.GetSecurityAttributes(typeof(SecureService));
            Assert.Contains(attrs, a => a.AttributeName == "PrincipalPermissionAttribute");
        }
    }
}
