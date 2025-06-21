using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.API.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
    }
}
