namespace Animal.Domain.Entities
{
    public class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsAvailable { get; set; }
        public int ShelterId { get; set; }
    }
}
