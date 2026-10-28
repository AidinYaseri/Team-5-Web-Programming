namespace Animal.Client.Contracts
{
    // The JSON the API returns for a pet. This is the client's own copy: the client never references
    // Animal.API, Animal.Data or Animal.Domain, it only knows the public HTTP contract.
    public record PetResponse(int Id, string Name, string Species, string Breed, int Age, bool IsAvailable, int ShelterId);
}
