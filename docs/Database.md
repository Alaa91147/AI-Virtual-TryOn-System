# Database Design

Current provider: SQL Server.

Default local database:

```text
Server=localhost;Database=VirtualTryOnDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

If using SQL authentication with `sa`, do not commit the password. Set the connection string locally before running migrations:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Database=VirtualTryOnDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet ef database update
```

Migration commands:

```powershell
cd backend
dotnet ef migrations add MigrationName
dotnet ef database update
```

Reset command:

```powershell
cd backend
dotnet ef database drop --force
dotnet ef database update
```

## Users Table

Fields:
- Id: primary key
- FullName
- Email
- NormalizedEmail
- PasswordHash
- Gender
- Role: Customer or Admin
- IsEmailVerified
- DateOfBirth
- CreatedAt
- UpdatedAt

## RefreshTokens Table

Fields:
- Id: primary key
- UserId: foreign key to Users
- Token
- ExpiresAt
- CreatedAt
- RevokedAt

Relationship:
One user can have many refresh tokens.

## UserImages Table

Fields:
- Id: primary key
- UserId: foreign key to Users
- FrontBodyImageUrl
- FaceImageUrl
- SideBodyImageUrl
- BackupBodyImageUrl

Relationship:
One user has one set of images.

## UserMeasurements Table

Fields:
- Id: primary key
- UserId: foreign key to Users
- Gender
- Height
- Chest
- Waist
- Hips

Relationship:
One user has one measurement profile.

## Clothes Table

Fields:
- Id: primary key
- Name
- Category: top, bottom, dress, shoes, etc.
- Color
- AvailableSizes: S, M, L, XL
- Description
- GenderTarget
- ImageUrl
- IsActive
- CreatedAt

Relationship:
Admin manages clothes.
Users select clothes.

## TryOnResults Table

Fields:
- Id: primary key
- UserId: foreign key to Users
- ClothingId: foreign key to Clothes
- GeneratedImageUrl
- RecommendedSize
- SuitabilityPercentage
- Comment
- CreatedAt

Relationship:
One user can have many try-on results.

## Approvals Table

Fields:
- Id: primary key
- UserId: foreign key to Users
- TryOnResultId: foreign key to TryOnResults
- Action: Approved or Rejected
- CreatedAt

Relationship:
Each try-on result can have one approval/rejection action.
