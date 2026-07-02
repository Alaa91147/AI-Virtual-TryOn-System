# AI Virtual Try-On Shop System

Auth-first implementation for an AI-powered clothing store. The current version includes React sign up/sign in pages, protected profile routing, ASP.NET Core auth APIs, JWT authentication, hashed passwords, and EF Core migrations for the auth database.

## Local Setup

### Backend

```powershell
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

The backend runs on `http://localhost:5113` by default.

The default database is SQL Server:

```text
Server=localhost;Database=VirtualTryOnDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

If you connect with `sa` in SQL Server Management Studio, keep the password out of Git and set it only in your local shell:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Database=VirtualTryOnDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet ef database update
dotnet run
```

### Frontend

```powershell
cd frontend
npm install
npm run dev
```

The frontend runs on `http://127.0.0.1:5173` or `http://localhost:5173`.

If your backend URL changes, create `frontend/.env`:

```env
VITE_API_BASE_URL=http://localhost:5113
```

## Auth Routes

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/me`

Frontend pages:

- `/auth/signup`
- `/auth/login`
- `/auth/forgot-password`
- `/profile`

## Database Workflow

Use EF Core migrations only. Do not create or change tables manually from a database UI.

Create a migration after changing C# models:

```powershell
cd backend
dotnet ef migrations add MigrationName
```

Apply migrations:

```powershell
cd backend
dotnet ef database update
```

Reset the local SQL Server database:

```powershell
cd backend
dotnet ef database drop --force
dotnet ef database update
```

Commit migration files to GitHub so teammates can run `dotnet ef database update` and recreate the same schema.

## Security Notes

- Passwords are hashed with ASP.NET Core Identity password hashing.
- JWT settings are in `backend/appsettings.json` for local development.
- Override `Jwt__Secret` with an environment variable in production.
- Keep `.env` files, local database backups, and real SQL Server passwords out of Git.
