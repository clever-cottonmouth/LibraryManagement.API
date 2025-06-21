using LibraryManagement.API.Data;
using LibraryManagement.API.DTOs;
using LibraryManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly LibraryService _libraryService;
        private readonly AuthService _authService;
        private readonly LibraryContext _context;

        public StudentController(LibraryService libraryService, AuthService authService)
        {
            _libraryService = libraryService;
            _authService = authService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] LoginDto loginDto)
        {
            var result = await _authService.StudentRegister(loginDto);
            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var token = await _authService.StudentLogin(loginDto);
            return Ok(new { Token = token });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            await _authService.SendPasswordResetEmail(email);
            return Ok("Password reset email sent");
        }

        [HttpGet("books/search")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SearchBooks([FromQuery] string query)
        {
            var books = await _libraryService.SearchBooks(query);
            return Ok(books);
        }

        [HttpGet("issued-books")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetIssuedBooks()
        {
            var studentId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Jti).Value);
            var issues = await _context.BookIssues
                .Include(i => i.Book)
                .Where(i => i.StudentId == studentId && i.ReturnDate == null)
                .ToListAsync();
            return Ok(issues);
        }

        [HttpPost("notifications/reply/{id}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ReplyNotification(int id, [FromBody] string reply)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) throw new Exception("Notification not found");
            notification.Reply = reply;
            await _context.SaveChangesAsync();
            return Ok("Reply sent");
        }
    }
}
