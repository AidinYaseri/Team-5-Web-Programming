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
        private const int MaxMultipartBodySizeBytes = 4 * 1024 * 1024;

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

        [HttpGet("me/picture")]
        [Authorize(Policy = AuthorizationPolicies.UsersRead)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfilePicture(CancellationToken cancellationToken)
        {
            var picture = await _userService.GetMyProfilePictureAsync(_currentUser.Subject, cancellationToken);
            // Private to this caller: never cached, and never sniffed into another content type.
            Response.Headers.CacheControl = "private, no-store";
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(picture.Bytes, picture.ContentType);
        }

        [HttpPut("me/picture")]
        [Authorize(Policy = AuthorizationPolicies.UsersWrite)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxMultipartBodySizeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxMultipartBodySizeBytes)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        public async Task<IActionResult> UploadMyProfilePicture(
            [FromForm] IFormFile? file,
            CancellationToken cancellationToken)
        {
            if (Request.ContentLength is > ProfilePictureRequestSizeLimitBytes)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            if (file is null || file.Length == 0)
            {
                throw new DomainValidationException("A profile picture file is required.");
            }

            if (file.Length > ProfilePictureRules.MaxBytes)
            {
                throw new DomainValidationException(
                    $"Profile picture can't be larger than {ProfilePictureRules.MaxBytes} bytes.");
            }

            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            await _userService.SaveMyProfilePictureAsync(
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
            await _userService.DeleteMyProfilePictureAsync(_currentUser.Subject, cancellationToken);
            return NoContent();
        }
    }
}
