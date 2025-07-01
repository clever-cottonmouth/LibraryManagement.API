using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.API.utils
{
    public class ApiResponse
    {
        public string Message { get; set; }
        public string Status { get; set; }
        public string? Error { get; set; }
    }
}
