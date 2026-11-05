using Animal.Domain.Enums;

namespace Animal.API.Contracts
{
    // Query string for GET /api/adoptions (reviewers). Status is optional, e.g. ?status=Pending.
    public class AdoptionQuery
    {
        public AdoptionStatus? Status { get; set; }
    }
}
