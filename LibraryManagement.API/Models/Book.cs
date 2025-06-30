using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.API.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Publication { get; set; }
        public string? PdfUrl { get; set; }
        public string? VideoUrl { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
    }
}
