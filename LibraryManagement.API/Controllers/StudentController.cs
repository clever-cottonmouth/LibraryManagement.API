using LibraryManagement.API.Data;
using LibraryManagement.API.DTOs;
using LibraryManagement.API.Services;
using LibraryManagement.API.utils;
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

        public StudentController(LibraryService libraryService, AuthService authService, LibraryContext context)
        {
            _libraryService = libraryService;
            _authService = authService;
            _context = context;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.StudentRegister(loginDto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the error (e.g., using ILogger)
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var token = await _authService.StudentLogin(loginDto);
                if (token == null)
                {
                    return Unauthorized(new { Message = "Invalid email or password" });
                }

                return Ok(new { Token = token, Email = loginDto.Email });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred during login" });
            }
        }

        [HttpGet("isVerified/{email}")]
        [AllowAnonymous]
        public async Task<IActionResult> IsVerified(string email)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student == null) return NotFound("Student not found");
            return Ok(new { IsVerified = student.IsVerified, IsActive = student.IsActive });
        }



        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            try
            {
                await _authService.SendPasswordResetEmail(email);
                return Ok(new ApiResponse
                {
                    Message = "Password reset email sent",
                    Status = "Success"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Message = "Failed to send password reset email",
                    Status = "Error",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("books/search")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SearchBooks([FromQuery] string query="")
        {
            var books = await _libraryService.SearchBooks(query);
            return Ok(books);
        }

        [HttpGet("books/list")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetBooks()
        {
            var books = await _libraryService.BooksList();
            return Ok(books);
        }


        [HttpGet("issued-books/{email}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetIssuedBooks(string email)
        {
            try
            {
                var data = await _libraryService.GetIssuedBooks(email);
                return Ok(data);
            }
            catch (Exception ex)
            {
                if (ex.Message == "Student not found")
                    return NotFound(ex.Message);
                if (ex.Message == "Library settings not configured")
                    return StatusCode(500, ex.Message);
                return StatusCode(500, "An unexpected error occurred");
            }
        }

        [HttpPut("notifications/reply/{Id}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ReplyNotification(int id, [FromBody] string reply)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) throw new Exception("Notification not found");
            notification.Reply = reply;
            await _context.SaveChangesAsync();
            return Ok(new
            {
                Success = true,
                Message = "Message send Sussessfully"
            });
        }

        [HttpGet("notifications/{email}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetNotifications(string email)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student == null) throw new Exception("Student not found");
            var notifications = await _context.Notifications
                .Where(n => n.StudentId == student.Id)
                .ToListAsync();
            return Ok(notifications);
        }


        [HttpPatch("profile-update/{email}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ProfileUpdate(string email, [FromBody] StudentDto student)
        {
            try
            {
                var existingStudent = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
                if (existingStudent == null) throw new Exception("Student not found");
                existingStudent.Name = student.Name;

                _context.Students.Update(existingStudent);
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    Email = email,
                    Name = existingStudent.Name,
                });
            }
            catch(Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }

            
        }

        [HttpPut("change-password/{email}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ChangePassword(string email, [FromBody] UpdatePasswordDto form)
        {
            try
            {
                if (form == null)
                {
                    throw new Exception("Form data is required.");
                }
                if (string.IsNullOrWhiteSpace(form.OldPassword) || string.IsNullOrWhiteSpace(form.NewPassword) || string.IsNullOrWhiteSpace(form.ConfirmPassword))
                {
                    throw new Exception("All password fields are required.");
                }
                if (form.NewPassword.Length < 6)
                {
                    throw new Exception("New password must be at least 6 characters long.");
                }
                var result = await _authService.ChangePassword(email, form);
                if (result == null)
                {
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
                    if (student == null)
                    {
                        throw new Exception("Student not found.");
                    }
                    else if (form.NewPassword != form.ConfirmPassword)
                    {
                        throw new Exception("New password and confirm password do not match.");
                    }
                    else if (!_authService.VerifyPassword(form.OldPassword, student.PasswordHash))
                    {
                        throw new Exception("Old password is incorrect.");
                    }
                    else
                    {
                        throw new Exception("An error occurred while changing the password.");
                    }
                }
                return Ok(new
                {
                    Message = "Password updated successfully",
                    Success = true,
                    Student = result
                });
            }
            catch (Exception ex)
            {
                return Unauthorized (new
                {
                    Message = ex.Message,
                    Success = false
                });
            }           
        }

        [HttpGet("getInfo/{email}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentInfo(string email)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student == null) return NotFound("Student not found");
            return Ok(new StudentDto
            {
                Id = student.Id,
                Email = student.Email,
                Name = student.Name,
                IsActive = student.IsActive,
                IsVerified = student.IsVerified
            });
        }
    }
}
