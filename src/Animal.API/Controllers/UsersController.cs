using Animal.API.Authorization;
using Animal.API.Contracts;
using Animal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Animal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICurrentUser _currentUser;

        public UsersController(IUserService userService, ICurrentUser currentUser)
        {
            _userService = userService;
            _currentUser = currentUser;
        }

        [HttpGet("me")]
        [Authorize(Policy = AuthorizationPolicies.UsersRead)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponse>> GetMyProfile(CancellationToken cancellationToken)
        {
            return Ok(await _userService.GetMyProfileAsync(_currentUser.Subject, cancellationToken));
        }

        [HttpPut("me")]
        [Authorize(Policy = AuthorizationPolicies.UsersWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateMyProfile(
            UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            await _userService.CreateOrUpdateMyProfileAsync(
                _currentUser.Subject,
                request,
                cancellationToken);

            return NoContent();
        }
    }
}
