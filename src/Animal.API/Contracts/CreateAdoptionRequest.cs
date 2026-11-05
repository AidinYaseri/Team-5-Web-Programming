using System.ComponentModel.DataAnnotations;

namespace Animal.API.Contracts
{
    // Body for POST /api/adoptions. The applicant comes from the token, never from the body.
    public class CreateAdoptionRequest
    {
        [Range(1, int.MaxValue)]
        public int PetId { get; set; }
    }
}
