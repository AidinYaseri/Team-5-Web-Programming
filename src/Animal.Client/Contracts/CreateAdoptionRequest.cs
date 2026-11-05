namespace Animal.Client.Contracts
{
    // Client copy of the API's CreateAdoptionRequest. The applicant comes from the access token.
    public sealed record CreateAdoptionRequest(int PetId);
}
