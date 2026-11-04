namespace Animal.Data.Repositories
{
    public interface IShelterRepository
    {
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    }
}
