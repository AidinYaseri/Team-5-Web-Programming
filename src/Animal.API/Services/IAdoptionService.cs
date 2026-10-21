using Animal.Domain.Entities;

namespace Animal.API.Services
{
    // Not implemented yet. The slice owner switches these to the DTOs in docs/milestone-3/api-contracts.md.
    public interface IAdoptionService
    {
        Task<AdoptionApplication> GetApplicationAsync(int id, CancellationToken cancellationToken = default);
        Task<List<AdoptionApplication>> GetApplicationsAsync(CancellationToken cancellationToken = default);
        Task<AdoptionApplication> CreateApplicationAsync(AdoptionApplication application, CancellationToken cancellationToken = default);
        Task ApproveApplicationAsync(int id, CancellationToken cancellationToken = default);
        Task RejectApplicationAsync(int id, CancellationToken cancellationToken = default);
    }
}
