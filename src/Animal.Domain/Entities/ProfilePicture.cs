namespace Animal.Domain.Entities
{
    public class ProfilePicture
    {
        public int UserId { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public byte[] Bytes { get; set; } = [];
        public DateTime UpdatedAt { get; set; }
    }
}
