namespace Animal.Data.Repositories
{
    // Saves everything the repositories changed in one transaction. Application services call this once per operation.
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
