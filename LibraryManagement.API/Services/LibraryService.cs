﻿using Konscious.Security.Cryptography;
using LibraryManagement.API.Data;
using LibraryManagement.API.DTOs;
using LibraryManagement.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.API.Services
{
    public class LibraryService
    {
        private readonly LibraryContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LibraryService(LibraryContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task AddStudent(StudentDto studentDto)
        {
            try { 
                if (await _context.Students.AnyAsync(s => s.Email == studentDto.Email))
                {
                    throw new Exception("Student with this email already exists");
                }
                else
                {
                    var student = new Student
                    {
                        Email = studentDto.Email,
                        Name = studentDto.Name,
                        IsActive = true,
                        IsVerified = false,
                        PasswordHash = HashPassword("Password123"),
                    };
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                throw new Exception("Student with this email already exists");
            }
        }

        public async Task DeactivateStudent(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) throw new Exception("Student not found");
            student.IsActive = false;
            await _context.SaveChangesAsync();

        }

        public async Task ActivateStudent(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) throw new Exception("Student not found");
            student.IsActive = true;
            await _context.SaveChangesAsync();
        }

        public async Task AddBook(BookDto bookDto, IFormFile pdfFile, IFormFile videoFile)
        {
            var bookExists = await _context.Books
            .AsNoTracking()
            .AnyAsync(b => b.Title == bookDto.Title &&
                          b.Author == bookDto.Author &&
                          b.Publication == bookDto.Publication);
            if (bookExists)
            {
                throw new InvalidOperationException("A book with the same title, author, and publication already exists");
            }

            const long maxPdfSizeBytes = 5 * 1024 * 1024; 
            const long maxVideoSizeBytes = 50 * 1024 * 1024; 

            if (pdfFile != null && pdfFile.Length > maxPdfSizeBytes)
            {
                throw new ArgumentException("PDF file size exceeds 5 MB");
            }

            if (videoFile != null && videoFile.Length > maxVideoSizeBytes)
            {
                throw new ArgumentException("Video file size exceeds 50 MB");
            }

            var book = new Book
            {
                Title = bookDto.Title,
                Author = bookDto.Author,
                Publication = bookDto.Publication,
                Stock = bookDto.Stock,
                IsActive = true,
            };

            if (pdfFile != null && pdfFile.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(pdfFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(stream);
                }
                book.PdfUrl = Path.Combine("uploads", fileName).Replace("\\", "/");
            }

            if (videoFile != null && videoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(stream);
                }
                book.VideoUrl = Path.Combine("uploads", fileName).Replace("\\", "/");
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();
        }

        public async Task IssueBook(BookIssueDto issueDto)
        {
            var student = await _context.Students.FindAsync(issueDto.StudentId);
            var book = await _context.Books.FindAsync(issueDto.BookId);
            var settings = await _context.LibrarySettings.FirstOrDefaultAsync();

            if (student == null || book == null || settings == null)
                throw new ApplicationException("Invalid student, book, or settings");

            if (issueDto.DueDate <= issueDto.IssueDate)
                throw new ApplicationException("Due date must be after issue date");

            if (!student.IsActive || !student.IsVerified)
                throw new ApplicationException("Student account not active or verified");

            if (student.BooksIssued >= settings.MaxBookLimit)
                throw new ApplicationException("Book limit exceeded");

            if (book.Stock <= 0)
                throw new ApplicationException("Book out of stock");

            var issue = new BookIssue
            {
                StudentId = issueDto.StudentId,
                BookId = issueDto.BookId,
                IssueDate = issueDto.IssueDate,
                DueDate = issueDto.DueDate 
            };

            book.Stock--;
            student.BooksIssued++;
            _context.BookIssues.Add(issue);
            await _context.SaveChangesAsync();
        }

        public async Task<decimal?> ReturnBook(int issueId)
        {
            var issue = await _context.BookIssues
                .Include(i => i.Book)
                .Include(i => i.Student)
                .FirstOrDefaultAsync(i => i.Id == issueId);

            if (issue == null) throw new Exception("Issue not found");

            var settings = await _context.LibrarySettings.FirstOrDefaultAsync();
            if (settings == null) throw new Exception("Settings not found");

            issue.ReturnDate = DateTime.Now;
            decimal? penalty = null;

            if (issue.ReturnDate > issue.DueDate)
            {
                var daysLate = (issue.ReturnDate.Value - issue.DueDate).Days;
                penalty = daysLate * settings.PenaltyPerDay;
                issue.Penalty = penalty.Value;
                issue.Student.Penalty += penalty.Value;
            }

            issue.Book.Stock++;
            issue.Student.BooksIssued--;
            await _context.SaveChangesAsync();
            return penalty;
        }

        public async Task<List<BookDto>> SearchBooks(string query)
        {
            return await _context.Books
                .Where(b => string.IsNullOrEmpty(query) ||
                b.Title.Contains(query)
                || b.Author.Contains(query)
                || b.Publication.Contains(query))
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Publication = b.Publication,
                    Stock = b.Stock,
                    IsActive = b.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<StudentDto>> SearchStudents(string query)
        {
            return await _context.Students
                .Where(s => s.Name.Contains(query) || s.Email.Contains(query))
                .Select(s => new StudentDto
                {
                    Id= s.Id,
                    Email = s.Email,
                    Name = s.Name,
                    IsActive = s.IsActive,
                    IsVerified = s.IsVerified
                })
                .ToListAsync();
        }

        public async Task UpdateSettings(int maxBookLimit, decimal penaltyPerDay)
        {
            var settings = await _context.LibrarySettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new LibrarySettings { MaxBookLimit = maxBookLimit, PenaltyPerDay = penaltyPerDay };
                _context.LibrarySettings.Add(settings);
            }
            else
            {
                settings.MaxBookLimit = maxBookLimit;
                settings.PenaltyPerDay = penaltyPerDay;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<List<StudentDto>> StudentsList()
        {
            return await _context.Students
                 .OrderByDescending(s => s.Id)
                 .Select(s => new StudentDto
                 {
                     Id = s.Id,
                     Email = s.Email,
                     Name = s.Name,
                     IsActive = s.IsActive,
                     IsVerified = s.IsVerified
                 })
                .ToListAsync();
        }

        public async Task<List<BookDto>> BooksList()
        {
            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}/";
            return await _context.Books
                .OrderByDescending(b => b.Id)
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Publication = b.Publication,
                    Stock = b.Stock,
                    IsActive = b.IsActive,
                    PdfUrl = b.PdfUrl != null ? $"{baseUrl}{b.PdfUrl}" : null,
                    VideoUrl = b.VideoUrl != null ? $"{baseUrl}{b.VideoUrl}" : null
                })
                .ToListAsync();
        }


        public async Task<List<BookIssueDto>> GetIssuedBooks(string email)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student == null)
            {
                throw new Exception("Student not found");
            }

            var librarySettings = await _context.LibrarySettings.FirstOrDefaultAsync();
            if (librarySettings == null)
            {
                throw new Exception("Library settings not configured");
            }
            decimal penaltyPerDay = librarySettings.PenaltyPerDay;

            var currentDate = DateTime.Today;

            var issues = await _context.BookIssues
                .Include(i => i.Book)
                .Where(i => i.StudentId == student.Id && i.ReturnDate == null)
                .Select(i => new BookIssueDto
                {
                    Id = i.Id,
                    BookId = i.BookId,
                    BookTitle = i.Book.Title, 
                    IssueDate = i.IssueDate,
                    DueDate = i.DueDate,
                    Penalty = i.DueDate.Date < currentDate
                        ? (decimal)(currentDate - i.DueDate.Date).TotalDays * penaltyPerDay
                        : 0m
                })
                .ToListAsync();

            return issues;
        }

        public async Task<List<Notification>> Notifications()
        {
            return await _context.Notifications
                .Include(n=>n.Student)
                .Where(n => n.Reply != null)
                .ToListAsync();
        }

        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return null;

            var issuedBook = await _context.BookIssues
                .FirstOrDefaultAsync(i => i.StudentId == id && i.ReturnDate == null);
            if (issuedBook != null)
                throw new InvalidOperationException("Cannot delete student with unreturned books.");

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return new OkObjectResult(new
            {
                Success = true,
                Message = "Student deleted successfully"
            });
        }

        public async Task<StudentDto> StudentById(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return null;
            return new StudentDto
            {
                Id = student.Id,
                Email = student.Email,
                Name = student.Name,
                IsActive = student.IsActive,
                IsVerified = student.IsVerified
            };
        }

        public async Task<bool> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return false;
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BookDto> DeactivateBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return null;
            book.IsActive = false;
            await _context.SaveChangesAsync();
            return new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Publication = book.Publication,
                Stock = book.Stock,
                IsActive = book.IsActive
            };
        }

        public async Task<BookDto> ActivateBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return null;
            book.IsActive = true;
            await _context.SaveChangesAsync();
            return new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Publication = book.Publication,
                Stock = book.Stock,
                IsActive = book.IsActive
            };
        }


        public async Task<BookDto> GetBookById(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                throw new Exception("Book not found");
            }

            return new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Publication = book.Publication,
                Stock = book.Stock,
                IsActive = book.IsActive,
                PdfUrl = book.PdfUrl,
                VideoUrl = book.VideoUrl
            };
        }


        public async Task UpdateBook(int id, BookDto bookDto, IFormFile pdfFile, IFormFile videoFile)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                throw new Exception("Book not found");
            }

            // Update book fields
            book.Title = bookDto.Title;
            book.Author = bookDto.Author;
            book.Publication = bookDto.Publication;
            book.Stock = bookDto.Stock;
            book.IsActive = bookDto.IsActive;

            // Handle PDF file update
            if (pdfFile != null && pdfFile.Length > 0)
            {
                // Delete old PDF if it exists
                if (!string.IsNullOrEmpty(book.PdfUrl))
                {
                    var oldFilePath = Path.Combine("wwwroot", book.PdfUrl.Replace("/", "\\"));
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }

                // Save new PDF
                var uploadsFolder = Path.Combine("wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(pdfFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(stream);
                }
                book.PdfUrl = Path.Combine("uploads", fileName).Replace("\\", "/");
            }


            if (videoFile != null && videoFile.Length > 0)
            {
                // Delete old PDF if it exists
                if (!string.IsNullOrEmpty(book.VideoUrl))
                {
                    var oldFilePath = Path.Combine("wwwroot", book.VideoUrl.Replace("/", "\\"));
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }

                // Save new PDF
                var uploadsFolder = Path.Combine("wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(stream);
                }
                book.VideoUrl = Path.Combine("uploads", fileName).Replace("\\", "/");
            }

            _context.Books.Update(book);
            await _context.SaveChangesAsync();
        }

        public async Task<StudentDto> VerifyStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return null;
            student.IsVerified = true;
            await _context.SaveChangesAsync();
            return new StudentDto
            {
                Id = student.Id,
                Email = student.Email,
                Name = student.Name,
                IsActive = student.IsActive,
                IsVerified = student.IsVerified
            };
        }

        private string HashPassword(string password)
        {
            // Convert password to bytes
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Configure Argon2 (Argon2id is recommended for password hashing)
            using var hasher = new Argon2id(passwordBytes)
            {
                DegreeOfParallelism = 4, // Number of threads
                MemorySize = 65536,      // 64 MB memory
                Iterations = 4           // Number of iterations
            };

            // Generate a random salt (16 bytes recommended)
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            hasher.Salt = salt;

            // Compute the hash
            byte[] hash = hasher.GetBytes(32); // 32-byte hash

            // Combine salt and hash for storage (base64 for simplicity)
            byte[] hashBytes = new byte[salt.Length + hash.Length];
            Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
            Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);

            return Convert.ToBase64String(hashBytes);
        }

        public async Task<IActionResult> SendNotifications(string message)
        {
            // Fetch all students
            var students = await _context.Students.ToListAsync();
            if (students == null || !students.Any())
            {
                return new NotFoundResult();
            }

            // Delete all existing notifications
            var existingNotifications = await _context.Notifications.ToListAsync();
            if (existingNotifications.Any())
            {
                _context.Notifications.RemoveRange(existingNotifications);
            }

            // Create new notifications
            var notifications = new List<Notification>();
            foreach (var student in students)
            {
                var notification = new Notification
                {
                    StudentId = student.Id,
                    Message = message
                };
                notifications.Add(notification);
            }

            // Add new notifications
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return new OkObjectResult(notifications);
        }


        public async Task<List<LibrarySettingsDto>> GetSettings()
        {
            return await _context.LibrarySettings
                .Select(s => new LibrarySettingsDto
                {
                    MaxBookLimit = s.MaxBookLimit,
                    PenaltyPerDay = s.PenaltyPerDay
                })
                .ToListAsync();
        }
    }
}
