using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.API.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Reply { get; set; }
        public DateTime SentDate { get; set; }
    }
}
