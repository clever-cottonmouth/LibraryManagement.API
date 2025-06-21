using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.API.Models
{
    public class LibrarySettings
    {
        public int Id { get; set; }
        public int MaxBookLimit { get; set; }
        public decimal PenaltyPerDay { get; set; }
    }
}
