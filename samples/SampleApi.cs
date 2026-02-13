// Sample ASP.NET Core API Controller for testing Spy module
// This file demonstrates various endpoint patterns that Spy can detect.
//
// Build with: dotnet new webapi && copy this file into Controllers/
// Or reference the compiled assembly directly with Spy.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
