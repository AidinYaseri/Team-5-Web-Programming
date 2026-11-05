namespace Animal.Client.Contracts
{
    public sealed record NotificationResponse(int Id, string Message, DateTime Date, bool IsRead);
}
