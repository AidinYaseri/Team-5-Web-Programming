using System;
using System.Collections.Generic;
using System.Text;
using Animal.Domain.Models;

namespace Animal.Domain.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public bool IsRead { get; set; }
    }
}
