using Animal.API.Contracts;
using Animal.Domain.Models;
using Animal.Domain.Services;
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
        public ActionResult<IEnumerable<PetResponse>> GetPets([FromQuery] PetSearchQuery query)
        {
            var criteria = new PetSearchCriteria
            {
                Species = query.Species,
                MaxAge = query.MaxAge,
                AvailableOnly = query.AvailableOnly
            };

            var pets = _petService.SearchPets(criteria);
            return Ok(pets.Select(PetResponse.FromPet));
        }
    }
}
