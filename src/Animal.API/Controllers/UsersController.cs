using Animal.API.Authorization;
using Animal.API.Contracts;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;
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
        private readonly IProfilePictureService _profilePictureService;
        private readonly ICurrentUser _currentUser;

        public UsersController(
            IUserService userService,
            IProfilePictureService profilePictureService,
            ICurrentUser currentUser)
        {
            _userService = userService;
            _profilePictureService = profilePictureService;
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

        [HttpGet("me/picture")]
        [Authorize(Policy = AuthorizationPolicies.UsersRead)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfilePicture(CancellationToken cancellationToken)
        {
            Response.Headers.CacheControl = "private, no-store";
            var picture = await _profilePictureService.GetAsync(_currentUser.Subject, cancellationToken);
            if (picture is null)
            {
                return NoContent();
            }

            return File(picture.Bytes, picture.ContentType);
        }

        [HttpPut("me/picture")]
        [Authorize(Policy = AuthorizationPolicies.UsersWrite)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(ProfilePictureRules.MaxBytes + 65_536)]
        [RequestFormLimits(MultipartBodyLengthLimit = ProfilePictureRules.MaxBytes + 65_536)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadMyProfilePicture(
            [FromForm] IFormFile? file,
            CancellationToken cancellationToken)
        {
            if (file is null)
            {
                throw new DomainValidationException("Profile picture is required.");
            }

            if (file.Length > ProfilePictureRules.MaxBytes)
            {
                throw new DomainValidationException(
                    $"Profile picture can't be larger than {ProfilePictureRules.MaxBytes} bytes.");
            }

            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            await _profilePictureService.SaveAsync(
                _currentUser.Subject,
                file.ContentType,
                buffer.ToArray(),
                cancellationToken);

            return NoContent();
        }

        [HttpDelete("me/picture")]
        [Authorize(Policy = AuthorizationPolicies.UsersWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMyProfilePicture(CancellationToken cancellationToken)
        {
            await _profilePictureService.DeleteAsync(_currentUser.Subject, cancellationToken);
            return NoContent();
        }
    }
}
