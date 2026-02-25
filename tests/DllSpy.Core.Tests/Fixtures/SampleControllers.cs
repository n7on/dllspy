using System.Collections.Generic;
using System.Threading.Tasks;

namespace DllSpy.Core.Tests.Fixtures
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        [HttpGet]
        public Task<List<UserDto>> GetAll() => Task.FromResult(new List<UserDto>());

        [HttpGet("{id}")]
        public Task<UserDto> GetById([FromRoute] int id) => Task.FromResult(new UserDto());

        [HttpPost]
        public Task<UserDto> Create([FromBody] UserDto user) => Task.FromResult(user);

        [Authorize]
        [HttpPut("{id}")]
        public Task Update([FromRoute] int id, [FromBody] UserDto user) => Task.CompletedTask;

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public Task Delete([FromRoute] int id) => Task.CompletedTask;
    }

    [Authorize(Policy = "AdminPolicy")]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        [HttpGet]
        public Task<string> GetDashboard() => Task.FromResult("dashboard");

        [HttpPost]
        public Task CreateSetting([FromBody] object setting) => Task.CompletedTask;

        [AllowAnonymous]
        [HttpGet("health")]
        public string HealthCheck() => "ok";
    }

    [Route("api/[controller]")]
    public class PublicController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet]
        public string GetInfo() => "info";

        [HttpPost]
        public Task Submit([FromBody] object data) => Task.CompletedTask;
    }

    public class PlainController
    {
        public string Index() => "Hello";
        public string Details(int id) => $"Details {id}";
    }
}
