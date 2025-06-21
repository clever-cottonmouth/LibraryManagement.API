using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.API.DTOs
{
    public class BookIssueDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int BookId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Penalty { get; set; }
    }
}
