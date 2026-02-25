using System.Collections.Generic;
using System.Linq;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;
using DllSpy.Core.Tests.Fixtures;
using Xunit;

namespace DllSpy.Core.Tests.Services
{
    public class HttpEndpointDiscoveryTests
    {
        private readonly List<HttpEndpoint> _endpoints;

        public HttpEndpointDiscoveryTests()
        {
            var analyzer = new AttributeAnalyzer();
            var discovery = new HttpEndpointDiscovery(analyzer);
            var assembly = typeof(UsersController).Assembly;
            var surfaces = discovery.Discover(assembly);
            _endpoints = surfaces.Cast<HttpEndpoint>().ToList();
        }

        [Fact]
        public void Discovers_AllControllerEndpoints()
        {
            // UsersController:5 + AdminController:3 + PublicController:2 + PlainController:2 = 12
            Assert.Equal(12, _endpoints.Count);
        }

        [Fact]
        public void UsersController_HasFiveEndpoints()
        {
            var users = _endpoints.Where(e => e.ClassName == "Users").ToList();
            Assert.Equal(5, users.Count);
        }

        [Fact]
        public void UsersController_RoutesResolvedCorrectly()
        {
            Assert.Contains(_endpoints, e => e.Route == "api/Users" && e.HttpMethod == "GET" && e.MethodName == "GetAll");
            Assert.Contains(_endpoints, e => e.Route == "api/Users/{id}" && e.HttpMethod == "GET" && e.MethodName == "GetById");
            Assert.Contains(_endpoints, e => e.Route == "api/Users" && e.HttpMethod == "POST" && e.MethodName == "Create");
            Assert.Contains(_endpoints, e => e.Route == "api/Users/{id}" && e.HttpMethod == "PUT" && e.MethodName == "Update");
            Assert.Contains(_endpoints, e => e.Route == "api/Users/{id}" && e.HttpMethod == "DELETE" && e.MethodName == "Delete");
        }

        [Fact]
        public void AdminController_RoutesResolvedCorrectly()
        {
            Assert.Contains(_endpoints, e => e.Route == "api/Admin" && e.HttpMethod == "GET" && e.MethodName == "GetDashboard");
            Assert.Contains(_endpoints, e => e.Route == "api/Admin" && e.HttpMethod == "POST" && e.MethodName == "CreateSetting");
            Assert.Contains(_endpoints, e => e.Route == "api/Admin/health" && e.HttpMethod == "GET" && e.MethodName == "HealthCheck");
        }

        [Fact]
        public void PlainController_UsesConventionalRouting()
        {
            Assert.Contains(_endpoints, e => e.Route == "api/Plain/Index" && e.HttpMethod == "GET");
            Assert.Contains(_endpoints, e => e.Route == "api/Plain/Details" && e.HttpMethod == "GET");
        }

        [Fact]
        public void HttpMethods_InferredFromAttributes()
        {
            var getAll = _endpoints.First(e => e.MethodName == "GetAll" && e.ClassName == "Users");
            Assert.Equal("GET", getAll.HttpMethod);

            var create = _endpoints.First(e => e.MethodName == "Create" && e.ClassName == "Users");
            Assert.Equal("POST", create.HttpMethod);

            var update = _endpoints.First(e => e.MethodName == "Update" && e.ClassName == "Users");
            Assert.Equal("PUT", update.HttpMethod);

            var delete = _endpoints.First(e => e.MethodName == "Delete" && e.ClassName == "Users");
            Assert.Equal("DELETE", delete.HttpMethod);
        }

        [Fact]
        public void PlainController_DefaultsToGetMethod()
        {
            var plain = _endpoints.Where(e => e.ClassName == "Plain").ToList();
            Assert.All(plain, e => Assert.Equal("GET", e.HttpMethod));
        }

        [Fact]
        public void Parameters_ExtractedWithCorrectTypes()
        {
            var create = _endpoints.First(e => e.MethodName == "Create" && e.ClassName == "Users");
            Assert.Single(create.Parameters);
            Assert.Equal("user", create.Parameters[0].Name);
            Assert.Equal("UserDto", create.Parameters[0].Type);
            Assert.Equal(ParameterSource.Body, create.Parameters[0].Source);
        }

        [Fact]
        public void RouteParameters_ExtractedWithSource()
        {
            var getById = _endpoints.First(e => e.MethodName == "GetById" && e.ClassName == "Users");
            var idParam = getById.Parameters.First(p => p.Name == "id");
            Assert.Equal("int", idParam.Type);
            Assert.Equal(ParameterSource.Route, idParam.Source);
            Assert.True(idParam.IsRequired);
        }

        [Fact]
        public void AsyncMethods_DetectedCorrectly()
        {
            var getAll = _endpoints.First(e => e.MethodName == "GetAll" && e.ClassName == "Users");
            Assert.True(getAll.IsAsync);

            var healthCheck = _endpoints.First(e => e.MethodName == "HealthCheck" && e.ClassName == "Admin");
            Assert.False(healthCheck.IsAsync);
        }

        [Fact]
        public void ReturnTypes_ResolvedCorrectly()
        {
            var getAll = _endpoints.First(e => e.MethodName == "GetAll" && e.ClassName == "Users");
            Assert.Equal("Task<List<UserDto>>", getAll.ReturnType);

            var healthCheck = _endpoints.First(e => e.MethodName == "HealthCheck" && e.ClassName == "Admin");
            Assert.Equal("string", healthCheck.ReturnType);
        }
    }
}
