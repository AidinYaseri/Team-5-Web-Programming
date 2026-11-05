namespace Animal.Client.Services
{
    public sealed class ClientPermissions
    {
        private readonly HashSet<string> _permissions;

        private ClientPermissions(HashSet<string> permissions)
        {
            _permissions = permissions;
        }

        public bool CanWritePets => _permissions.Contains("pets.write");

        public static ClientPermissions FromString(string? permissions)
        {
            var set = string.IsNullOrWhiteSpace(permissions)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : permissions
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return new ClientPermissions(set);
        }
    }
}