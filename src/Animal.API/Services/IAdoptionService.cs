using Animal.API.Contracts;

namespace Animal.API.Services
{
    // Adopters apply and follow their own applications; reviewers (adoptions.review) see all and approve or reject.
    public interface IAdoptionService
    {
        Task<AdoptionResponse> ApplyAsync(int userId, CreateAdoptionRequest request, CancellationToken cancellationToken = default);
        Task<List<AdoptionResponse>> GetMineAsync(int userId, CancellationToken cancellationToken = default);

        // Reviewers can read any application. Everyone else only their own; someone else's reads as not found.
        Task<AdoptionResponse> GetAsync(int id, int? requesterUserId, bool requesterCanReview, CancellationToken cancellationToken = default);

        Task<List<AdoptionResponse>> SearchAsync(AdoptionQuery query, CancellationToken cancellationToken = default);
        Task ApproveAsync(int id, CancellationToken cancellationToken = default);
        Task RejectAsync(int id, CancellationToken cancellationToken = default);
    }
}
