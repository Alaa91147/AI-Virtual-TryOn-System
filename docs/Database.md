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

Authentication/account root only.

Fields:
- Id: primary key
- Email
- NormalizedEmail
- PasswordHash
- GoogleSubject
- Role: Customer or Admin
- IsEmailVerified
- CreatedAt
- UpdatedAt

## UserProfiles Table

Profile identity and contact details.

Fields:
- UserId: primary key and foreign key to Users
- FullName
- Gender
- PhoneNumber
- ProfilePhotoUrl
- DateOfBirth

Relationship:
One user has one profile row.

## UserFitProfiles Table

Fit and size data used for try-on recommendations.

Fields:
- UserId: primary key and foreign key to Users
- HeightCm
- WeightKg
- PreferredSize
- BodyShape
- ShoeSize
- TopSize
- BottomSize

Relationship:
One user has one fit profile row.

## UserTryOnPhotos Table

Photos used by virtual try-on workflows.

Fields:
- UserId: primary key and foreign key to Users
- FullBodyPhotoUrl
- UpperBodyPhotoUrl
- LowerBodyPhotoUrl
- FacePhotoUrl

Relationship:
One user has one try-on photo row.

## UserDeliveryAddresses Table

Delivery address details for shopping checkout.

Fields:
- UserId: primary key and foreign key to Users
- Country
- City
- Street
- Building
- PhoneNumber

Relationship:
One user has one delivery address row.

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
