using System.Security.Claims;
using Animal.Client.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace Animal.UnitTests.Client
{
    public class InMemoryTicketStoreTests
    {
        private readonly InMemoryTicketStore _store = new(new MemoryCache(new MemoryCacheOptions()));

        [Fact]
        public async Task Store_ThenRetrieve_ReturnsTheSameSession()
        {
            var key = await _store.StoreAsync(Ticket("user-1", DateTimeOffset.UtcNow.AddMinutes(30)));

            var ticket = await _store.RetrieveAsync(key);

            Assert.Equal("user-1", ticket?.Principal.FindFirst("sub")?.Value);
        }

        [Fact]
        public async Task Store_TwoSignIns_GetDifferentRandomKeys()
        {
            var first = await _store.StoreAsync(Ticket("user-1", DateTimeOffset.UtcNow.AddMinutes(30)));
            var second = await _store.StoreAsync(Ticket("user-1", DateTimeOffset.UtcNow.AddMinutes(30)));

            Assert.NotEqual(first, second);
            Assert.Equal(32, first.Length);
        }

        [Fact]
        public async Task Remove_EndsOnlyThatSession()
        {
            var deviceA = await _store.StoreAsync(Ticket("user-1", DateTimeOffset.UtcNow.AddMinutes(30)));
            var deviceB = await _store.StoreAsync(Ticket("user-1", DateTimeOffset.UtcNow.AddMinutes(30)));

            await _store.RemoveAsync(deviceA);

            Assert.Null(await _store.RetrieveAsync(deviceA));
            Assert.NotNull(await _store.RetrieveAsync(deviceB));
        }

        [Fact]
        public async Task Retrieve_UnknownOrExpiredKey_ReturnsNull()
        {
            var expired = await _store.StoreAsync(Ticket("user-1", DateTimeOffset.UtcNow.AddSeconds(-1)));

            Assert.Null(await _store.RetrieveAsync(expired));
            Assert.Null(await _store.RetrieveAsync("not-a-real-key"));
        }

        private static AuthenticationTicket Ticket(string subject, DateTimeOffset expiresUtc) =>
            new(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)], "Cookies")),
                new AuthenticationProperties { ExpiresUtc = expiresUtc },
                "Cookies");
    }
}
