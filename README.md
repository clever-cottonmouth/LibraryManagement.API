# Library Management System

A comprehensive library management system built with ASP.NET Core Web API that provides functionality for managing books, students, librarians, and book lending operations.

## 🚀 Features

### Authentication & Authorization
- JWT-based authentication for students and librarians
- Role-based authorization (Student, Librarian)
- Secure password hashing using Argon2id
- Password reset functionality via email

### Student Features
- Student registration and login
- Book search and browsing
- View issued books
- Profile management
- Password change functionality
- Reply to notifications

### Librarian Features
- Student management (add, activate, deactivate, verify, delete)
- Book management (add, update, delete, activate/deactivate)
- Book lending and return operations
- Notification system
- Library settings management
- PDF file upload for books

### System Features
- Real-time book stock management
- Automatic penalty calculation for late returns
- Email notifications via SendGrid
- File upload and management
- Comprehensive API documentation with Swagger

## 🏗️ Project Structure

```
LibraryManagement/
├── LibraryManagement.API/           # Main API project
│   ├── Controllers/                 # API controllers
│   │   ├── LibrarianController.cs   # Librarian operations
│   │   └── StudentController.cs     # Student operations
│   ├── Data/                        # Database context
│   │   └── LibraryContext.cs        # Entity Framework context
│   ├── DTOs/                        # Data Transfer Objects
│   │   ├── AddBookFormDto.cs
│   │   ├── BookDto.cs
│   │   ├── BookIssueDto.cs
│   │   ├── LoginDto.cs
│   │   ├── NotificationDto.cs
│   │   ├── StudentDto.cs
│   │   └── UpdatePasswordDto.cs
│   ├── Models/                      # Entity models
│   │   ├── Book.cs
│   │   ├── BookIssue.cs
│   │   ├── Librarian.cs
│   │   ├── LibrarySettings.cs
│   │   ├── Notification.cs
│   │   └── Student.cs
│   ├── Services/                    # Business logic services
│   │   ├── AuthService.cs           # Authentication & authorization
│   │   ├── FileService.cs           # File upload operations
│   │   └── LibraryService.cs        # Core library operations
│   ├── Migrations/                  # Entity Framework migrations
│   ├── wwwroot/                     # Static files
│   │   └── uploads/                 # PDF file storage
│   ├── Program.cs                   # Application entry point
│   └── appsettings.json            # Configuration
└── LibraryManagement.sln           # Solution file
```

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core 9.0
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT Bearer Tokens
- **Password Hashing**: Argon2id (Konscious.Security.Cryptography)
- **Email Service**: SendGrid
- **API Documentation**: Swagger/OpenAPI
- **File Storage**: Local file system with PDF support

## 📋 Prerequisites

- .NET 9.0 SDK
- SQL Server (LocalDB or full instance)
- SendGrid account (for email functionality)
- Visual Studio 2022 or VS Code

## ⚙️ Installation & Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd LibraryManagement
   ```

2. **Configure the database connection**
   - Update the connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=LibraryDb;Trusted_Connection=True;"
   }
   ```

3. **Configure SendGrid (optional)**
   - Get your SendGrid API key from [SendGrid Dashboard](https://app.sendgrid.com/)
   - Update the API key in `appsettings.json`:
   ```json
   "SendGrid": {
     "ApiKey": "YourSendGridApiKey"
   }
   ```

4. **Run database migrations**
   ```bash
   cd LibraryManagement.API
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the API**
   - API Base URL: `https://localhost:7001` or `http://localhost:5001`
   - Swagger Documentation: `https://localhost:7001/swagger`

## 🔌 API Endpoints

### Authentication Endpoints

#### Student Authentication
- `POST /api/student/register` - Register a new student
- `POST /api/student/login` - Student login
- `POST /api/student/forgot-password` - Request password reset
- `PUT /api/student/change-password/{email}` - Change password

#### Librarian Authentication
- `POST /api/librarian/login` - Librarian login

### Student Endpoints

#### Book Operations
- `GET /api/student/books/search?query={query}` - Search books
- `GET /api/student/books/list` - Get all books
- `GET /api/student/issued-books/{email}` - Get issued books

#### Profile Management
- `PATCH /api/student/profile-update/{email}` - Update profile
- `POST /api/student/notifications/reply/{id}` - Reply to notification

### Librarian Endpoints

#### Student Management
- `GET /api/librarian/students` - Get all students
- `POST /api/librarian/students` - Add new student
- `PUT /api/librarian/students/{id}/activate` - Activate student
- `PUT /api/librarian/students/{id}/deactivate` - Deactivate student
- `PUT /api/librarian/students/{id}/verify` - Verify student
- `DELETE /api/librarian/students/{id}` - Delete student

#### Book Management
- `GET /api/librarian/books` - Get all books
- `POST /api/librarian/books` - Add new book (with PDF upload)
- `PUT /api/librarian/books/{id}` - Update book
- `DELETE /api/librarian/books/{id}` - Delete book
- `PUT /api/librarian/books/{id}/activate` - Activate book
- `PUT /api/librarian/books/{id}/deactivate` - Deactivate book

#### Library Operations
- `POST /api/librarian/issue-book` - Issue book to student
- `POST /api/librarian/return-book` - Return book
- `GET /api/librarian/notifications` - Get notifications
- `PUT /api/librarian/settings` - Update library settings

## 🔐 Authentication

The API uses JWT Bearer tokens for authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

### Token Claims
- `sub`: User email
- `role`: User role (Student/Librarian)
- `jti`: Unique token identifier

## 📊 Database Schema

### Core Entities
- **Students**: User accounts with email, password, name, verification status
- **Librarians**: Admin accounts for library management
- **Books**: Book information with title, author, publication, stock, PDF URL
- **BookIssues**: Lending records with issue/return dates and penalties
- **Notifications**: Communication system between librarians and students
- **LibrarySettings**: System configuration (book limits, penalties)

## 🚀 Key Features Explained

### Security
- **Argon2id Password Hashing**: Industry-standard password hashing with salt
- **JWT Authentication**: Secure token-based authentication
- **Role-based Authorization**: Different access levels for students and librarians

### File Management
- **PDF Upload**: Books can have associated PDF files
- **Secure Storage**: Files stored with unique GUIDs
- **File Cleanup**: Automatic cleanup when books are updated/deleted

### Business Logic
- **Stock Management**: Automatic stock tracking for book lending
- **Penalty System**: Automatic calculation of late return penalties
- **Book Limits**: Configurable maximum books per student
- **Email Notifications**: Password reset and system notifications

## 🧪 Testing

The API includes comprehensive Swagger documentation for testing:
1. Navigate to `/swagger` in your browser
2. Use the interactive documentation to test endpoints
3. Authenticate using the login endpoints first
4. Copy the JWT token and use the "Authorize" button

## 📝 Environment Variables

Key configuration settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your_Database_Connection_String"
  },
  "Jwt": {
    "Key": "Your_JWT_Secret_Key_32_Characters_Minimum",
    "Issuer": "Your_Issuer",
    "Audience": "Your_Audience"
  },
  "SendGrid": {
    "ApiKey": "Your_SendGrid_API_Key"
  }
}
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## 🆘 Support

For support and questions:
- Create an issue in the repository
- Check the Swagger documentation at `/swagger`
- Review the API endpoints documentation above

---

**Note**: This is a development-ready library management system. For production use, ensure proper security configurations, SSL certificates, and environment-specific settings. 