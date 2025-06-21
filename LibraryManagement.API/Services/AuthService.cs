using LibraryManagement.API.Data;
using LibraryManagement.API.DTOs;
using LibraryManagement.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Konscious.Security.Cryptography;
using System.Security.Cryptography;


namespace LibraryManagement.API.Services
{
    public class AuthService
    {
        private readonly LibraryContext _context;
        private readonly IConfiguration _configuration;
        private readonly ISendGridClient _sendGridClient;

        public AuthService(LibraryContext context, IConfiguration configuration, ISendGridClient sendGridClient)
        {
            _context = context;
            _configuration = configuration;
            _sendGridClient = sendGridClient;
        }

        public async Task<string> LibrarianLogin(LoginDto loginDto)
        {
            var librarian = await _context.Librarians
                .FirstOrDefaultAsync(l => l.Email == loginDto.Email);

            if (librarian == null || loginDto.Password != librarian.PasswordHash)
                throw new UnauthorizedAccessException("Invalid credentials");

            return GenerateJwtToken(librarian.Email, "Librarian");
        }

        public async Task<string> StudentRegister(LoginDto loginDto)
        {
            // Validate CAPTCHA (e.g., Google reCAPTCHA)
            if (!await ValidateCaptcha(loginDto.CaptchaToken))
                throw new Exception("Invalid CAPTCHA");

            var student = new Student
            {
                Email = loginDto.Email,
                PasswordHash = HashPassword(loginDto.Password),
                Name = loginDto.Email.Split('@')[0],
                IsActive = false,
                IsVerified = false
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return "Registration successful. Awaiting librarian verification.";
        }

        public async Task SendPasswordResetEmail(string email)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student == null) throw new Exception("Student not found");

            var token = GenerateJwtToken(email, "Student", TimeSpan.FromMinutes(30));
            var resetLink = $"https://yourapp.com/reset-password?token={token}";

            var message = new SendGridMessage
            {
                From = new EmailAddress("no-reply@library.com"),
                Subject = "Password Reset",
                PlainTextContent = $"Click here to reset your password: {resetLink}"
            };
            message.AddTo(new EmailAddress(email));
            await _sendGridClient.SendEmailAsync(message);
        }

        private string GenerateJwtToken(string email, string role, TimeSpan? expiry = null)
        {
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiry.HasValue ? DateTime.Now.Add(expiry.Value) : DateTime.Now.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        //private string HashPassword(string password)
        //{
        //    return BCrypt.Net.BCrypt.HashPassword(password);
        //}

        //private bool VerifyPassword(string password, string hash)
        //{
        //    return BCrypt.Net.BCrypt.Verify(password, hash);
        //}

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

        private bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                // Decode stored hash (salt + hash)
                byte[] hashBytes = Convert.FromBase64String(storedHash);
                if (hashBytes.Length < 48) // 16-byte salt + 32-byte hash
                    return false;

                // Extract salt and hash
                byte[] salt = new byte[16];
                byte[] storedHashBytes = new byte[32];
                Buffer.BlockCopy(hashBytes, 0, salt, 0, 16);
                Buffer.BlockCopy(hashBytes, 16, storedHashBytes, 0, 32);

                // Compute hash of provided password
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                using var hasher = new Argon2id(passwordBytes)
                {
                    DegreeOfParallelism = 4,
                    MemorySize = 65536,
                    Iterations = 4,
                    Salt = salt
                };

                byte[] computedHash = hasher.GetBytes(32);

                // Compare hashes (constant-time comparison)
                return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
            }
            catch
            {
                return false; // Invalid base64 or other errors
            }
        }

        private async Task<bool> ValidateCaptcha(string token)
        {
            // Implement reCAPTCHA validation using Google API
            return true; // Placeholder
        }

        public async Task<string> StudentLogin(LoginDto loginDto)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(l => l.Email == loginDto.Email);

            if (student == null || !VerifyPassword(loginDto.Password, student.PasswordHash))
                return null;

            return GenerateJwtToken(student.Email, "Student");
        }
    }
}
