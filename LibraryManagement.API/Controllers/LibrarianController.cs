using Azure.Core;
using LibraryManagement.API.Data;
using LibraryManagement.API.DTOs;
using LibraryManagement.API.Models;
using LibraryManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        public LibrarianController(LibraryService libraryService, AuthService authService, LibraryContext context)
        {
            _libraryService = libraryService;
            _authService = authService;
            _context = context;
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

        [HttpPatch("students/{id}/deactivate")]
        public async Task<IActionResult> DeactivateStudent(int id)
        {
            try
            {
                await _libraryService.DeactivateStudent(id);
                return Ok(new
                {
                    Success = true,
                    Message = "Student deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Message = $"Failed to deactivate student: {ex.Message}"
                });
            }

        }

        [HttpPatch("students/{id}/activate")]
        public async Task<IActionResult> ActivateStudent(int id)
        {
            try
            {
                await _libraryService.ActivateStudent(id);
                return Ok(new
                {
                    Success = true,
                    Message = "Student activated successfully"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Message = $"Failed to activate student: {ex.Message}"
                });
            }
        }

        [HttpPost("books")]
        public async Task<IActionResult> AddBook([FromForm] AddBookFormDto form)
        {
            var bookDto = new BookDto
            {
                Title = form.Title,
                Author = form.Author,
                Publication = form.Publication,
                Stock = form.Stock
            };
            await _libraryService.AddBook(bookDto, form.PdfFile);
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
            try
            {
                await _libraryService.IssueBook(issueDto.StudentId, issueDto.BookId);
                return Ok(new
                {
                    Success = true,
                    Message = "Book issued successfully"
                });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An unexpected error occurred"
                });
            }
        }

        [HttpPost("return/{issueId}")]
        public async Task<IActionResult> ReturnBook(int issueId)
        {
            try
            {
                await _libraryService.ReturnBook(issueId);
                return Ok(new
                {
                    Success = true,
                    Message = "Book returned successfully"
                });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An unexpected error occurred"
                });
            }

        }

        [HttpGet("books")]
        public async Task<IActionResult> GetAllBooks()
        {
            var books = await _libraryService.SearchBooks("");
            return Ok(books);
        }

        [HttpGet("books/search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string query = "")
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
            return Ok(new
            {
                Success = true,
                Message = "Settings updated successfully"
            });
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
                // Data includes PdfFile property, which is the path to the PDF file for each book
                Data = books
            });
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications([FromBody] string message)
        {
            var notifications = await _libraryService.Notifications(message);
            return Ok(new
            {
                Success = true,
                Data = notifications
            });
        }

        [HttpPost("sendNotifications")]
        public async Task<IActionResult> SendNotifications([FromBody] SendNotificationsDto request)
        {
            if (string.IsNullOrEmpty(request?.Message))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "The message field is required."
                });
            }

            var notifications = await _libraryService.SendNotifications(request.Message);
            return Ok(new
            {
                Success = true,
                Message = "Notifications sent successfully",
                Data = notifications
            });
        }




        [HttpDelete("students/{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                await _libraryService.DeleteStudent(id);
                return Ok(new
                {
                    Success = true,
                    Message = "Student deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Message = $"Failed to delete student: {ex.Message}"
                });
            }
        }

        [HttpGet("students/{id}")]
        public async Task<IActionResult> GetStudentById(int id)
        {
            var student = await _libraryService.StudentById(id);
            if (student == null)
            {
                return NotFound(new { Success = false, Message = "Student not found" });
            }
            return Ok(new { Success = true, Data = student });
        }

        [HttpDelete("books/{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                var success = await _libraryService.DeleteBook(id);
                if (!success)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Book not found"
                    });
                }
                return Ok(new
                {
                    Success = true,
                    Message = "Book deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Failed to delete book: {ex.Message}"
                });
            }
        }

        [HttpPatch("books/{id}/deactivate")]
        public async Task<IActionResult> DeactivateBook(int id)
        {
            try
            {
                await _libraryService.DeactivateBook(id);
                return Ok(new
                {
                    Success = true,
                    Message = "Book deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Message = $"Failed to deactivate book: {ex.Message}"
                });
            }

        }

        [HttpPatch("books/{id}/activate")]
        public async Task<IActionResult> ActivateBook(int id)
        {
            try
            {
                await _libraryService.ActivateBook(id);
                return Ok(new
                {
                    Success = true,
                    Message = "Book activated successfully"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Message = $"Failed to activate book: {ex.Message}"
                });
            }
        }

        [HttpGet("books/{id}")]
        public async Task<IActionResult> GetBook(int id)
        {
            try
            {
                var book = await _libraryService.GetBookById(id);
                return Ok(new
                {
                    Success = true,
                    Data = book
                });
            }
            catch (Exception ex)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }


        [HttpPut("books/{id}")]
        public async Task<IActionResult> UpdateBook(int id, [FromForm] AddBookFormDto form)
        {
            try
            {
                var bookDto = new BookDto
                {
                    Id = id,
                    Title = form.Title,
                    Author = form.Author,
                    Publication = form.Publication,
                    Stock = form.Stock,
                    IsActive = true
                };
                await _libraryService.UpdateBook(id, bookDto, form.PdfFile);
                return Ok(new
                {
                    Success = true,
                    Message = "Book updated successfully",
                    Data = bookDto
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPatch("students/{id}/verify")]
        public async Task<IActionResult> VerifyStudent(int id)
        {
            try
            {
                await _libraryService.VerifyStudent(id);
                return Ok(new
                {
                    Success = true,
                    Message = "Student verified successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Failed to verify student: {ex.Message}"
                });
            }
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var settings = await _libraryService.GetSettings();
                return Ok(settings);
            }
            catch (Exception ex)
            {         
                return StatusCode(500, new { Error = "An error occurred while retrieving settings" });
            }
        }
    }
}
