namespace Animal.Data.Repositories
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        void Add(TEntity entity);
        void Remove(TEntity entity);
    }
}