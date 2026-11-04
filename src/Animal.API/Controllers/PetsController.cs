using Animal.API.Contracts;
using Animal.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Animal.API.Controllers
{
    // Thin controller: routing, model validation and status codes only. The work happens in IPetService.
    // Domain errors (404, 400, 409) are turned into responses by DomainExceptionHandler.
    [ApiController]
    [Route("api/[controller]")]
    public class PetsController : ControllerBase
    {
        private readonly IPetService _petService;

        public PetsController(IPetService petService)
        {
            _petService = petService;
        }

        // GET api/pets?species=dog&maxAge=5&availableOnly=true
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<PetResponse>>> GetPets([FromQuery] PetSearchQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _petService.SearchPetsAsync(query, cancellationToken));
        }

        // GET api/pets/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PetResponse>> GetPet(int id, CancellationToken cancellationToken)
        {
            return Ok(await _petService.GetPetAsync(id, cancellationToken));
        }

        // POST api/pets
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PetResponse>> CreatePet(CreatePetRequest request, CancellationToken cancellationToken)
        {
            var pet = await _petService.CreatePetAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetPet), new { id = pet.Id }, pet);
        }

        // PUT api/pets/5
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePet(int id, UpdatePetRequest request, CancellationToken cancellationToken)
        {
            await _petService.UpdatePetAsync(id, request, cancellationToken);
            return NoContent();
        }

        // DELETE api/pets/5
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeletePet(int id, CancellationToken cancellationToken)
        {
            await _petService.DeletePetAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
