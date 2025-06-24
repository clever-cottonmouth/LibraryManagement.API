using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.API.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }
        public string Message { get; set; }
        public DateTime SentDate { get; set; }
        public string? Reply { get; set; }
    }
}
