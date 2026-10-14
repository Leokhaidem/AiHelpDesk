# AI Help Desk & Ticket Management System

An internal support application built with ASP.NET Core Web API, React,
SQL Server, and MongoDB.

## Current Status

The project is currently in the authentication and authorization phase.

Implemented: - ASP.NET Core Web API - Clean/layered project structure -
SQL Server + Entity Framework Core - ASP.NET Core Identity - User and
role storage - `Admin`, `Agent`, and `Employee` roles - Initial Admin
user seeding - Password hashing through ASP.NET Core Identity - JWT
authentication - Role-based authorization - Admin-protected test
endpoint - Local development secrets using ASP.NET Core User Secrets

## Tech Stack

### Backend

-   .NET 10
-   ASP.NET Core Web API
-   Entity Framework Core
-   ASP.NET Core Identity
-   JWT Bearer Authentication
-   SQL Server / LocalDB

### Planned / Project Technologies

-   React
-   MongoDB/Mircosoft SQL server
-   Gemini API
-   Docker
-   Azure
-   GitHub Actions

## Project Structure

``` text
AIHelpDesk/
├── backend/
│   ├── AIHelpDesk.API/
│   ├── AIHelpDesk.Application/
│   ├── AIHelpDesk.Domain/
│   ├── AIHelpDesk.Infrastructure/
│   └── AIHelpDesk.Tests/
└── README.md
```

The backend follows a layered architecture:

``` text
API
 ↓
Application
 ↓
Domain

Infrastructure → Application
```

## Running the Project Locally

### Prerequisites

Install: 1. .NET 10 SDK 2. SQL Server LocalDB or another SQL Server
instance 3. Git 4. Postman (recommended)

Check .NET:

``` bash
dotnet --version
```

### 1. Clone the repository

``` bash
git clone <YOUR_GITHUB_REPOSITORY_URL>
cd AIHelpDesk
```

### 2. Configure SQL Server

The current local development configuration uses:

``` text
(localdb)\MSSQLLocalDB
```

The database is:

``` text
AIHelpDeskDb
```

The connection string is stored in User Secrets and is not committed to
the repository.

### 3. Initialize User Secrets

``` bash
dotnet user-secrets init --project backend/AIHelpDesk.API
```

Set the SQL Server connection:

``` bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=AIHelpDeskDb;Trusted_Connection=True;TrustServerCertificate=True;" --project backend/AIHelpDesk.API
```

### 4. Configure the initial Admin account

Set the Admin email:

``` bash
dotnet user-secrets set "AdminSeed:Email" "admin@company.com" --project backend/AIHelpDesk.API
```

Set a strong password:

``` bash
dotnet user-secrets set "AdminSeed:Password" "YOUR_STRONG_PASSWORD" --project backend/AIHelpDesk.API
```

The password must satisfy the configured Identity requirements: - At
least 8 characters - At least one digit - At least one uppercase
letter - At least one lowercase letter - At least one non-alphanumeric
character

Do not commit the password to GitHub.

### 5. Configure JWT

Set a long random JWT signing key:

``` bash
dotnet user-secrets set "Jwt:Key" "YOUR_LONG_RANDOM_JWT_SECRET" --project backend/AIHelpDesk.API
```

Set the issuer:

``` bash
dotnet user-secrets set "Jwt:Issuer" "AIHelpDesk" --project backend/AIHelpDesk.API
```

Set the audience:

``` bash
dotnet user-secrets set "Jwt:Audience" "AIHelpDeskClient" --project backend/AIHelpDesk.API
```

Never commit the JWT secret.

### 6. Verify User Secrets

``` bash
dotnet user-secrets list --project backend/AIHelpDesk.API
```

You should have keys similar to:

``` text
ConnectionStrings:DefaultConnection
AdminSeed:Email
AdminSeed:Password
Jwt:Key
Jwt:Issuer
Jwt:Audience
```

### 7. Restore dependencies

``` bash
dotnet restore
```

### 8. Apply database migrations

``` bash
dotnet ef database update --project backend/AIHelpDesk.Infrastructure --startup-project backend/AIHelpDesk.API
```

This creates the `AIHelpDeskDb` database and Identity tables.

### 9. Run the API

``` bash
dotnet run --project backend/AIHelpDesk.API
```

The API should start at:

``` text
http://localhost:5045
```

### 10. Admin seeding

On startup, the application checks for the roles:

``` text
Admin
Agent
Employee
```

It then checks whether the configured Admin email already exists. If it
does not, an Admin user is created using the User Secrets credentials.

The password is passed to ASP.NET Core Identity, which hashes it before
storing it in SQL Server. The plain-text password is not stored.

Running the application again does not create duplicate Admin users.

## Testing Authentication

### Login

Request:

``` http
POST http://localhost:5045/api/auth/login
Content-Type: application/json
```

Body:

``` json
{
  "email": "admin@company.com",
  "password": "YOUR_ADMIN_PASSWORD"
}
```

Successful response:

``` json
{
  "accessToken": "YOUR_JWT_TOKEN",
  "expiresAt": "..."
}
```

## Testing Admin Authorization

After logging in, copy the `accessToken`.

Request:

``` http
GET http://localhost:5045/api/admin/test
```

In Postman:

``` text
Authorization
Type: Bearer Token
Token: <your-access-token>
```

A valid Admin token should return:

``` json
{
  "message": "You are authorized as an Admin!"
}
```

This verifies that the user, password, role, JWT signature, and Admin
authorization are all working.

## Authentication Flow

``` text
Client
  │
  │ POST /api/auth/login
  │ email + password
  ▼
AuthController
  │
  ├── Find user
  ├── Verify password using ASP.NET Core Identity
  ├── Get user roles
  └── Generate JWT
          │
          ▼
       Client
          │
          │ Authorization: Bearer <token>
          ▼
    ASP.NET Core
          │
          ├── Validate JWT
          ├── Validate signature
          ├── Validate issuer
          ├── Validate audience
          ├── Validate expiration
          └── Check role
                  │
                  ▼
            Protected endpoint
```

## Security Notes

Never commit the following to GitHub: - Admin passwords - JWT signing
keys - Database passwords - API keys - Production connection strings
containing credentials - Other secrets

Use ASP.NET Core User Secrets for local development. For production, use
environment variables or a secret-management service such as Azure Key
Vault.

## Useful Commands

Build:

``` bash
dotnet build
```

Run:

``` bash
dotnet run --project backend/AIHelpDesk.API
```

Restore:

``` bash
dotnet restore
```

Create migration:

``` bash
dotnet ef migrations add MigrationName --project backend/AIHelpDesk.Infrastructure --startup-project backend/AIHelpDesk.API
```

Apply migrations:

``` bash
dotnet ef database update --project backend/AIHelpDesk.Infrastructure --startup-project backend/AIHelpDesk.API
```

List secrets:

``` bash
dotnet user-secrets list --project backend/AIHelpDesk.API
```

## Current API

Authentication:

``` http
POST /api/auth/login
```

Admin test:

``` http
GET /api/admin/test
```

The Admin test endpoint is currently used to verify JWT authentication
and role-based authorization. It will later be replaced or expanded with
the actual Admin management endpoints.

## Roadmap

### Authentication & Authorization

-   [x] ASP.NET Core Identity
-   [x] SQL Server Identity database
-   [x] Admin / Agent / Employee roles
-   [x] Admin user seeding
-   [x] Password hashing through Identity
-   [x] JWT authentication
-   [x] Role-based authorization
-   [x] Admin authorization test endpoint
-   [ ] Refactor authentication logic into Application service

### Admin

-   [ ] User management
-   [ ] Employee management
-   [ ] Support Agent management
-   [ ] Category management
-   [ ] Priority management
-   [ ] Admin dashboard

### Support System

-   [ ] Ticket management
-   [ ] Comments
-   [ ] Attachments
-   [ ] Ticket history
-   [ ] Agent assignment

### AI

-   [ ] AI ticket classification
-   [ ] AI priority suggestion
-   [ ] AI response assistance
-   [ ] AI knowledge/search functionality

### Frontend

-   [ ] React application
-   [ ] Login page
-   [ ] Admin dashboard
-   [ ] Employee interface
-   [ ] Support Agent interface

## Contributors

This project is being developed collaboratively as an AI Help Desk &
Ticket Management System.
