namespace Animal.Client.Contracts
{
    // Client copy of the API's AdoptionResponse. Status is "Pending", "Approved" or "Rejected".
    public sealed record AdoptionResponse(int Id, int PetId, string PetName, int UserId, string Status, DateTime ApplicationDate);
}
