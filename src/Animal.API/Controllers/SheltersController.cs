using Animal.API.Contracts;
using Animal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Animal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SheltersController : ControllerBase
    {
        private readonly IShelterService _shelterService;

        public SheltersController(IShelterService shelterService)
        {
            _shelterService = shelterService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ShelterResponse>>> GetShelters(CancellationToken cancellationToken)
        {
            return Ok(await _shelterService.GetSheltersAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ShelterResponse>> GetShelter(int id, CancellationToken cancellationToken)
        {
            return Ok(await _shelterService.GetShelterAsync(id, cancellationToken));
        }

        [HttpGet("{id:int}/pets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PetResponse>>> GetShelterPets(int id, CancellationToken cancellationToken)
        {
            return Ok(await _shelterService.GetShelterPetsAsync(id, cancellationToken));
        }

        [HttpPost]
        [Authorize(Policy = "shelters.write")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ShelterResponse>> CreateShelter(CreateShelterRequest request, CancellationToken cancellationToken)
        {
            var shelter = await _shelterService.CreateShelterAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetShelter), new { id = shelter.Id }, shelter);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "shelters.write")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateShelter(int id, UpdateShelterRequest request, CancellationToken cancellationToken)
        {
            await _shelterService.UpdateShelterAsync(id, request, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "shelters.write")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteShelter(int id, CancellationToken cancellationToken)
        {
            await _shelterService.DeleteShelterAsync(id, cancellationToken);
            return NoContent();
        }
    }
}