using Animal.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Animal.Data.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly AnimalDbContext Context;
        protected readonly DbSet<TEntity> Set;

        public GenericRepository(AnimalDbContext context)
        {
            Context = context;
            Set = context.Set<TEntity>();
        }

        public Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Set.AsNoTracking().ToListAsync(cancellationToken);
        }

        public void Add(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            Set.Add(entity);
        }

        public void Remove(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            Set.Remove(entity);
        }
    }
}