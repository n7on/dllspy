// Sample ASP.NET Core API for testing Spy module
// This file demonstrates various endpoint patterns that Spy can detect,
// including HTTP controllers and SignalR hubs.
//
// Build with: dotnet new webapi && copy this file into the project
// Or reference the compiled assembly directly with Spy.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace SampleApi.Controllers
{
    // -------------------------------------------------------------------------
    // A typical CRUD controller with mixed authorization
    // -------------------------------------------------------------------------
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        /// <summary>
        /// Get all users - publicly accessible.
        /// Spy should flag this as Medium: missing explicit auth declaration.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            return Ok(Array.Empty<UserDto>());
        }

        /// <summary>
        /// Get user by ID - publicly accessible.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            return Ok(new UserDto());
        }

        /// <summary>
        /// Create a new user - requires authentication but no specific role.
        /// Spy should flag: POST without roles/policies (Low severity).
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
        {
            return CreatedAtAction(nameof(GetById), new { id = 1 }, new UserDto());
        }

        /// <summary>
        /// Update a user - requires Admin role.
        /// This is properly secured.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
        {
            return NoContent();
        }

        /// <summary>
        /// Delete a user - NO authorization!
        /// Spy should flag this as High severity: DELETE without [Authorize].
        /// </summary>
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            return NoContent();
        }
    }

    // -------------------------------------------------------------------------
    // Controller-level authorization with action-level overrides
    // -------------------------------------------------------------------------
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdminPolicy")]
    public class AdminController : ControllerBase
    {
        /// <summary>
        /// Admin dashboard - inherits controller-level [Authorize].
        /// </summary>
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            return Ok();
        }

        /// <summary>
        /// Health check - allows anonymous despite controller-level auth.
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok("healthy");
        }

        /// <summary>
        /// Reset system - requires both Admin policy and SuperAdmin role.
        /// </summary>
        [HttpPost("reset")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ResetSystem()
        {
            return Ok();
        }
    }

    // -------------------------------------------------------------------------
    // Controller with NO authorization at all
    // -------------------------------------------------------------------------
    [ApiController]
    [Route("api/[controller]")]
    public class PublicController : ControllerBase
    {
        [HttpGet("info")]
        [AllowAnonymous]
        public IActionResult GetInfo()
        {
            return Ok(new { version = "1.0" });
        }

        /// <summary>
        /// POST without auth - Spy should flag as High severity.
        /// </summary>
        [HttpPost("feedback")]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequest request)
        {
            return Ok();
        }
    }

    // -------------------------------------------------------------------------
    // DTOs used by the sample controllers
    // -------------------------------------------------------------------------
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class CreateUserRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class FeedbackRequest
    {
        public string Message { get; set; }
        public int Rating { get; set; }
    }
}

namespace SampleApi.Hubs
{
    // -------------------------------------------------------------------------
    // SignalR hub with NO authorization — Spy should flag all methods as High
    // -------------------------------------------------------------------------
    public class ChatHub : Hub
    {
        /// <summary>
        /// Send a message to all connected clients.
        /// Spy should flag: unauthenticated hub method (High severity).
        /// </summary>
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        /// <summary>
        /// Join a specific chat group.
        /// Spy should flag: unauthenticated hub method (High severity).
        /// </summary>
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
    }

    // -------------------------------------------------------------------------
    // SignalR hub with authorization
    // -------------------------------------------------------------------------
    [Authorize]
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Subscribe to notifications — inherits hub-level [Authorize].
        /// Spy should flag: authorize without roles/policies (Low severity).
        /// </summary>
        public async Task Subscribe(string channel)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, channel);
        }

        /// <summary>
        /// Unsubscribe from notifications.
        /// </summary>
        public async Task Unsubscribe(string channel)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channel);
        }

        /// <summary>
        /// Admin-only broadcast — requires Admin role.
        /// This is properly secured.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task Broadcast(string message)
        {
            await Clients.All.SendAsync("Notification", message);
        }
    }
}
