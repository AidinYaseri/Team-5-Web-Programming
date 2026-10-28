using Animal.Data.Repositories;

namespace Animal.IntegrationTests.Data
{
    public class ShelterRepositoryTests : IDisposable
    {
        private readonly SqliteTestDatabase _database = new();

        public void Dispose() => _database.Dispose();

        [Theory]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(99, false)]
        public async Task ExistsAsync_ChecksSeededShelters(int id, bool expected)
        {
            using var context = _database.CreateContext();

            Assert.Equal(expected, await new ShelterRepository(context).ExistsAsync(id));
        }
    }
}
