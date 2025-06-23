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
            try
            {
                await _libraryService.AddStudent(studentDto);
                return Ok(new
                {
                    Success = true,
                    Message = "Student added successfully",
                    Data = studentDto
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Message = $"Failed to add student: {ex.Message}"
                });
            }
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
            return Ok(new
            {
                Success = true,
                Message = "Book added successfully",
                Data = bookDto
            });
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

        [HttpGet("students/list")]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _libraryService.StudentsList();
            return Ok(students);
        }

        [HttpGet("books/list")]
        public async Task<IActionResult> GetBooks()
        {
            var books = await _libraryService.BooksList();
            return Ok(new
            {
                Success = true,
                Data = books
            });
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var notifications = await _libraryService.Notifications();
            return Ok(new
            {
                Success = true,
                Data = notifications
            });
        }
    }
}
