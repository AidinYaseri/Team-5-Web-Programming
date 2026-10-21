using System;
using System.Collections.Generic;
using System.Text;

namespace Animal.Domain.Models
{
    public class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Species { get; set; }
        public string Breed { get; set; }
        public int Age { get; set; }
        public bool IsAvailable { get; set; }
        public int ShelterId { get; set; }
    }
}
