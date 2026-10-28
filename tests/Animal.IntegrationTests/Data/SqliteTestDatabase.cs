using Animal.Data.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Animal.IntegrationTests.Data
{
    // A real relational database that lives in memory for one test. The connection must stay open or SQLite drops the data.
    public sealed class SqliteTestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SqliteTestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // EnsureCreated builds the schema from the same configurations and seeds the HasData rows (2 shelters, 5 pets).
            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        // A new context each time, like a new HTTP request, so tests don't pass just because EF cached the entity.
        public AnimalDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AnimalDbContext>()
                .UseSqlite(_connection)
                .Options;

            return new AnimalDbContext(options);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
