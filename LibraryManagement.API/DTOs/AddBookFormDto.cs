using Microsoft.AspNetCore.Http;

namespace LibraryManagement.API.DTOs
{
    public class AddBookFormDto
    {
        public string Title { get; set; }
        public string? Author { get; set; }
        public string? Publication { get; set; }
        public int Stock { get; set; }
        public IFormFile? PdfFile { get; set; }
        
    }
} 