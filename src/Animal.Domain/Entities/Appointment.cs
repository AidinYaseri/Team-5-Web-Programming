using System;
using System.Collections.Generic;
using System.Text;

namespace Animal.Domain.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PetId { get; set; }
        public int ShelterId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; }
    }
}
