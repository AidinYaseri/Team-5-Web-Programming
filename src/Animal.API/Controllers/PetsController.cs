using Animal.API.Contracts;
using Animal.API.Services;
using Animal.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Animal.API.Controllers
{
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
            var criteria = new PetSearchCriteria
            {
                Species = query.Species,
                MaxAge = query.MaxAge,
                AvailableOnly = query.AvailableOnly
            };

            return Ok(await _petService.SearchPetsAsync(criteria, cancellationToken));
        }
    }
}
