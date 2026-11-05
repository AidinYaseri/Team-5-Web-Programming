using Animal.API.Authorization;
using Animal.API.Contracts;
using Animal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Animal.API.Controllers
{
    // Thin controller: routing, model validation and status codes only. The work happens in IAdoptionService.
    // The applicant always comes from the token (ICurrentUser), never from the body or route.
    [ApiController]
    [Route("api/[controller]")]
    public class AdoptionsController : ControllerBase
    {
        private readonly IAdoptionService _adoptionService;
        private readonly ICurrentUser _currentUser;

        public AdoptionsController(IAdoptionService adoptionService, ICurrentUser currentUser)
        {
            _adoptionService = adoptionService;
            _currentUser = currentUser;
        }

        // POST api/adoptions
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdoptionsWrite)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AdoptionResponse>> Apply(CreateAdoptionRequest request, CancellationToken cancellationToken)
        {
            var created = await _adoptionService.ApplyAsync(_currentUser.UserId, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // GET api/adoptions/mine
        [HttpGet("mine")]
        [Authorize(Policy = AuthorizationPolicies.AdoptionsRead)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AdoptionResponse>>> GetMine(CancellationToken cancellationToken)
        {
            return Ok(await _adoptionService.GetMineAsync(_currentUser.UserId, cancellationToken));
        }

        // GET api/adoptions/5 (own application, or any with adoptions.review)
        [HttpGet("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdoptionsReadOrReview)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdoptionResponse>> GetById(int id, CancellationToken cancellationToken)
        {
            var canReview = ScopeClaims.HasScope(User, AuthorizationPolicies.AdoptionsReview);
            int? requesterUserId = ScopeClaims.HasMappedUser(User) ? _currentUser.UserId : null;

            return Ok(await _adoptionService.GetAsync(id, requesterUserId, canReview, cancellationToken));
        }

        // GET api/adoptions?status=Pending (reviewer queue)
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdoptionsReview)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<AdoptionResponse>>> Search([FromQuery] AdoptionQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _adoptionService.SearchAsync(query, cancellationToken));
        }

        // POST api/adoptions/5/approve
        [HttpPost("{id:int}/approve")]
        [Authorize(Policy = AuthorizationPolicies.AdoptionsReview)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        {
            await _adoptionService.ApproveAsync(id, cancellationToken);
            return NoContent();
        }

        // POST api/adoptions/5/reject
        [HttpPost("{id:int}/reject")]
        [Authorize(Policy = AuthorizationPolicies.AdoptionsReview)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
        {
            await _adoptionService.RejectAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
