using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Animal.Client.Services
{
    // Keeps each signed-in session (the user and their tokens) on the server. The auth cookie only holds a random
    // session key, so signing out deletes the session itself: a cookie copied before sign-out stops working, and
    // tokens never go to the browser, even encrypted. Sessions end at the cookie's expiry.
    // In memory means a restart signs everyone out; a deployment with several servers would use a shared cache.
    public sealed class InMemoryTicketStore(IMemoryCache cache) : ITicketStore
    {
        private const string KeyPrefix = "auth-session:";

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var key = Guid.NewGuid().ToString("N");
            Save(key, ticket);
            return Task.FromResult(key);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            Save(key, ticket);
            return Task.CompletedTask;
        }

        public Task<AuthenticationTicket?> RetrieveAsync(string key) =>
            Task.FromResult(cache.TryGetValue(KeyPrefix + key, out AuthenticationTicket? ticket) ? ticket : null);

        public Task RemoveAsync(string key)
        {
            cache.Remove(KeyPrefix + key);
            return Task.CompletedTask;
        }

        private void Save(string key, AuthenticationTicket ticket)
        {
            var options = new MemoryCacheEntryOptions();
            if (ticket.Properties.ExpiresUtc is { } expires)
            {
                options.SetAbsoluteExpiration(expires);
            }

            cache.Set(KeyPrefix + key, ticket, options);
        }
    }
}
