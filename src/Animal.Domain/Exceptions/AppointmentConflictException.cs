namespace Animal.Domain.Exceptions
{
    public sealed class AppointmentConflictException(string message) : DomainException(message)
    {
    }
}
