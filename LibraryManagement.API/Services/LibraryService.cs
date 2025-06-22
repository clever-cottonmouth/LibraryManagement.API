using LibraryManagement.API.Data;
using LibraryManagement.API.DTOs;
using LibraryManagement.API.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.API.Services
{
    public class LibraryService
    {
        private readonly LibraryContext _context;

        public LibraryService(LibraryContext context)
        {
            _context = context;
        }

        public async Task AddStudent(StudentDto studentDto)
        {
            var student = new Student
            {
                Email = studentDto.Email,
                Name = studentDto.Name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("DefaultPassword123"),
                IsActive = true,
                IsVerified = false
            };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
        }

        public async Task DeactivateStudent(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) throw new Exception("Student not found");
            student.IsActive = false;
            await _context.SaveChangesAsync();
        }

        public async Task AddBook(BookDto bookDto)
        {
            var book = new Book
            {
                Title = bookDto.Title,
                Author = bookDto.Author,
                Publication = bookDto.Publication,
                Stock = bookDto.Stock,
                IsActive = true,
                PdfUrl = bookDto.PdfUrl,

            };
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
        }

        public async Task IssueBook(int studentId, int bookId)
        {
            var student = await _context.Students.FindAsync(studentId);
            var book = await _context.Books.FindAsync(bookId);
            var settings = await _context.LibrarySettings.FirstOrDefaultAsync();

            if (student == null || book == null || settings == null)
                throw new Exception("Invalid student, book, or settings");

            if (!student.IsActive || !student.IsVerified)
                throw new Exception("Student account not active or verified");

            if (student.BooksIssued >= settings.MaxBookLimit)
                throw new Exception("Book limit exceeded");

            if (book.Stock <= 0)
                throw new Exception("Book out of stock");

            var issue = new BookIssue
            {
                StudentId = studentId,
                BookId = bookId,
                IssueDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14) // 2 weeks default
            };

            book.Stock--;
            student.BooksIssued++;
            _context.BookIssues.Add(issue);
            await _context.SaveChangesAsync();
        }

        public async Task ReturnBook(int issueId)
        {
            var issue = await _context.BookIssues
                .Include(i => i.Book)
                .Include(i => i.Student)
                .FirstOrDefaultAsync(i => i.Id == issueId);

            if (issue == null) throw new Exception("Issue not found");

            var settings = await _context.LibrarySettings.FirstOrDefaultAsync();
            if (settings == null) throw new Exception("Settings not found");

            issue.ReturnDate = DateTime.Now;
            if (issue.ReturnDate > issue.DueDate)
            {
                var daysLate = (issue.ReturnDate.Value - issue.DueDate).Days;
                issue.Penalty = daysLate * settings.PenaltyPerDay;
                issue.Student.Penalty += issue.Penalty;
            }

            issue.Book.Stock++;
            issue.Student.BooksIssued--;
            await _context.SaveChangesAsync();
        }

        public async Task<List<BookDto>> SearchBooks(string query)
        {
            return await _context.Books
                .Where(b => b.Title.Contains(query) || b.Author.Contains(query) || b.Publication.Contains(query))
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Publication = b.Publication,
                    Stock = b.Stock,
                    PdfUrl = b.PdfUrl,

                })
                .ToListAsync();
        }

        public async Task<List<StudentDto>> SearchStudents(string query)
        {
            return await _context.Students
                .Where(s => s.Name.Contains(query) || s.Email.Contains(query))
                .Select(s => new StudentDto
                {

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
            return await _context.Books
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Publication = b.Publication,
                    Stock = b.Stock,
                    PdfUrl = b.PdfUrl
                })
                .ToListAsync();
        }
    }
}
