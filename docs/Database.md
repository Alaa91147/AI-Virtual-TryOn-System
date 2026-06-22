# Database Design

## Users Table

Fields:
- Id: primary key
- FullName
- Email
- PasswordHash
- Role: User or Admin
- CreatedAt

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