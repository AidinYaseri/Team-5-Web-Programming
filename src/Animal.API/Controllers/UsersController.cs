using Animal.API.Authorization;
using Animal.API.Contracts;
using Animal.API.Services;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Animal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private const int ProfilePictureRequestSizeLimitBytes = 3 * 1024 * 1024;

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

        [HttpPut("me/picture")]
        [Authorize(Policy = AuthorizationPolicies.UsersWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveMyPicture([FromForm] IFormFile? file, CancellationToken cancellationToken)
        {
            if (Request.ContentLength is > ProfilePictureRequestSizeLimitBytes)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            if (file is null || file.Length == 0)
            {
                throw new DomainValidationException("A profile picture file is required.");
            }

            if (file.Length > ProfilePictureRequestSizeLimitBytes)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            if (file.Length > ProfilePictureRules.MaxBytes)
            {
                throw new DomainValidationException($"Profile picture can't be larger than {ProfilePictureRules.MaxBytes} bytes.");
            }

            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);

            await _userService.SaveMyProfilePictureAsync(
                _currentUser.Subject,
                file.ContentType,
                buffer.ToArray(),
                cancellationToken);

            return NoContent();
        }

        [HttpGet("me/picture")]
        [Authorize(Policy = AuthorizationPolicies.UsersRead)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyPicture(CancellationToken cancellationToken)
        {
            var picture = await _userService.GetMyProfilePictureAsync(_currentUser.Subject, cancellationToken);
            Response.Headers.CacheControl = "private, no-store";
            return File(picture.Bytes, picture.ContentType);
        }

        [HttpDelete("me/picture")]
        [Authorize(Policy = AuthorizationPolicies.UsersWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMyPicture(CancellationToken cancellationToken)
        {
            await _userService.DeleteMyProfilePictureAsync(_currentUser.Subject, cancellationToken);
            return NoContent();
        }
    }
}
