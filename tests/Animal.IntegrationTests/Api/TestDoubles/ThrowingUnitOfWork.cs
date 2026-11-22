using Animal.Data.Repositories;

namespace Animal.IntegrationTests.Api.TestDoubles
{
    public sealed class ThrowingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new TimeoutException("SQL timeout while saving to Server=secret-db;Database=AnimalDb;User Id=sa;Password=P@ssw0rd;");
        }
    }
}
