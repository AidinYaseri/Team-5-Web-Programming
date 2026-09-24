using System;
using System.Collections.Generic;
using System.Text;

namespace Animal.Domain.Models
{
    public class AdoptionApplication
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PetId { get; set; }
        public string Status { get; set; }
        public DateTime ApplicationDate { get; set; }
    }
}
