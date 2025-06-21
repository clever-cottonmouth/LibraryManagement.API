using LibraryManagement.API.Data;
using LibraryManagement.API.DTOs;
using LibraryManagement.API.Models;
using LibraryManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Librarian")]
    public class LibrarianController : Controller
    {
        private readonly LibraryService _libraryService;
        private readonly AuthService _authService;
        private readonly LibraryContext _context;

        public LibrarianController(LibraryService libraryService, AuthService authService)
        {
            _libraryService = libraryService;
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var token = await _authService.LibrarianLogin(loginDto);
            return Ok(new { Token = token });
        }

        [HttpPost("students")]
        public async Task<IActionResult> AddStudent([FromBody] StudentDto studentDto)
        {
            await _libraryService.AddStudent(studentDto);
            return Ok("Student added");
        }

        [HttpPut("students/{id}/deactivate")]
        public async Task<IActionResult> DeactivateStudent(int id)
        {
            await _libraryService.DeactivateStudent(id);
            return Ok("Student deactivated");
        }

        [HttpPost("books")]
        public async Task<IActionResult> AddBook([FromBody] BookDto bookDto)
        {
            await _libraryService.AddBook(bookDto);
            return Ok("Book added");
        }

        [HttpPost("issue")]
        public async Task<IActionResult> IssueBook([FromBody] BookIssueDto issueDto)
        {
            await _libraryService.IssueBook(issueDto.StudentId, issueDto.BookId);
            return Ok("Book issued");
        }

        [HttpPost("return/{issueId}")]
        public async Task<IActionResult> ReturnBook(int issueId)
        {
            await _libraryService.ReturnBook(issueId);
            return Ok("Book returned");
        }

        [HttpGet("books")]
        public async Task<IActionResult> GetAllBooks()
        {
            var books = await _libraryService.SearchBooks("");
            return Ok(books);
        }

        [HttpGet("books/search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string query)
        {
            var books = await _libraryService.SearchBooks(query);
            return Ok(books);
        }

        [HttpGet("students/search")]
        public async Task<IActionResult> SearchStudents([FromQuery] string query)
        {
            var students = await _libraryService.SearchStudents(query);
            return Ok(students);
        }

        [HttpGet("issued-books")]
        public async Task<IActionResult> GetIssuedBooks()
        {
            var issues = await _context.BookIssues
                .Include(i => i.Student)
                .Include(i => i.Book)
                .Where(i => i.ReturnDate == null)
                .ToListAsync();
            return Ok(issues);
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] LibrarySettings settings)
        {
            await _libraryService.UpdateSettings(settings.MaxBookLimit, settings.PenaltyPerDay);
            return Ok("Settings updated");
        }
    }
}
