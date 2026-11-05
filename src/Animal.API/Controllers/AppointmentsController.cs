using Animal.API.Contracts;
using Animal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Animal.API.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public sealed class AppointmentsController(IAppointmentService appointments, ICurrentUser currentUser) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = "appointments.read")]
        [ProducesResponseType<IReadOnlyList<AppointmentResponse>>(StatusCodes.Status200OK)]
        public Task<IReadOnlyList<AppointmentResponse>> GetMine(CancellationToken cancellationToken) =>
            appointments.GetForUserAsync(currentUser.UserId, cancellationToken);

        [HttpPost]
        [Authorize(Policy = "appointments.write")]
        [ProducesResponseType<AppointmentResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppointmentResponse>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken)
        {
            var created = await appointments.CreateAsync(currentUser.UserId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, created);
        }

        [HttpPost("{id:int}/cancel")]
        [Authorize(Policy = "appointments.write")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            var found = await appointments.CancelAsync(currentUser.UserId, id, cancellationToken);
            return found ? NoContent() : NotFound();
        }
    }
}
