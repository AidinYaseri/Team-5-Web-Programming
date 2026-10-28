namespace Animal.Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool IsRead { get; set; }
    }
}
