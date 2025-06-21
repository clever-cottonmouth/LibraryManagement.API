using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.API.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public int BooksIssued { get; set; }
        public decimal Penalty { get; set; }
    }
}
