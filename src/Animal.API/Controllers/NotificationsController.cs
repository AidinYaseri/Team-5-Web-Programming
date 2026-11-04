using Animal.API.Contracts;
using Animal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Animal.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public sealed class NotificationsController(INotificationService notifications, ICurrentUser currentUser) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = "notifications.read")]
        [ProducesResponseType<IReadOnlyList<NotificationResponse>>(StatusCodes.Status200OK)]
        public Task<IReadOnlyList<NotificationResponse>> Get([FromQuery] bool unreadOnly = false, CancellationToken cancellationToken = default) =>
            notifications.GetForUserAsync(currentUser.UserId, unreadOnly, cancellationToken);

        [HttpPost("{id:int}/read")]
        [Authorize(Policy = "notifications.write")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
        {
            var found = await notifications.MarkAsReadAsync(currentUser.UserId, id, cancellationToken);
            return found ? NoContent() : NotFound();
        }
    }
}
