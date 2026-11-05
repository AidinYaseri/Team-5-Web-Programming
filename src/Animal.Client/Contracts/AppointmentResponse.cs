namespace Animal.Client.Contracts
{
    public sealed record AppointmentResponse(int Id, int PetId, int ShelterId, DateTime AppointmentDate, string Status);
}
