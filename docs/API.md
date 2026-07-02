# API Endpoints

## Authentication APIs

POST /api/auth/register
Purpose:
Create a new user account.

Request:
{
  "fullName": "Alaa Awali",
  "email": "alaa@example.com",
  "password": "StrongPass123!",
  "confirmPassword": "StrongPass123!",
  "gender": "female",
  "dateOfBirth": "2000-01-01",
  "acceptTerms": true
}

Response:
{
  "token": "jwt_token_here",
  "refreshToken": "refresh_token_here",
  "expiresAt": "2026-07-02T10:00:00Z",
  "user": {
    "id": "user_id",
    "fullName": "Alaa Awali",
    "email": "alaa@example.com",
    "gender": "female",
    "role": "Customer",
    "isEmailVerified": false
  }
}

POST /api/auth/login
Purpose:
Sign in user or admin.

Request:
{
  "email": "alaa@example.com",
  "password": "StrongPass123!",
  "rememberMe": true
}

Response:
{
  "token": "jwt_token_here",
  "refreshToken": "refresh_token_here",
  "expiresAt": "2026-07-02T10:00:00Z",
  "user": {
    "id": "user_id",
    "fullName": "Alaa Awali",
    "email": "alaa@example.com",
    "gender": "female",
    "role": "Customer"
  }
}

POST /api/auth/logout
Purpose:
Revoke the current refresh token. Requires Bearer token.

Request:
{
  "refreshToken": "refresh_token_here"
}

Response:
{
  "message": "Signed out successfully."
}

GET /api/auth/me
Purpose:
Return the currently signed-in user. Requires Bearer token.

## User Profile APIs

GET /api/user/profile
Purpose:
Get logged-in user profile.

POST /api/user/measurements
Purpose:
Save user measurements.

Request:
{
  "gender": "Female",
  "height": 165,
  "chest": 85,
  "waist": 70,
  "hips": 95
}

POST /api/user/images
Purpose:
Upload 4 user images.

Data:
- frontBodyImage
- faceImage
- sideBodyImage
- backupBodyImage

## Clothes APIs

GET /api/clothes
Purpose:
Get all active clothes.

GET /api/clothes/{id}
Purpose:
Get one clothing item by ID.

## Admin APIs

POST /api/admin/clothes
Purpose:
Admin adds a clothing item.

Request:
{
  "name": "White T-Shirt",
  "category": "Top",
  "color": "White",
  "availableSizes": "S,M,L,XL",
  "description": "Basic white cotton t-shirt",
  "genderTarget": "Unisex"
}

PUT /api/admin/clothes/{id}
Purpose:
Admin edits clothing item.

DELETE /api/admin/clothes/{id}
Purpose:
Admin deactivates/deletes clothing item.

GET /api/admin/actions
Purpose:
Admin views approvals and rejections.

GET /api/admin/statistics
Purpose:
Admin views dashboard statistics.

## Try-On APIs

POST /api/tryon/generate
Purpose:
Generate CAT-VTON try-on image and size recommendation.

Request:
{
  "clothingId": 1
}

Response:
{
  "generatedImageUrl": "uploads/results/result1.png",
  "recommendedSize": "M",
  "suitabilityPercentage": 86,
  "comment": "Size M is the closest match based on the entered measurements."
}

GET /api/tryon/history
Purpose:
Get user try-on history.

POST /api/tryon/{id}/approve
Purpose:
Approve try-on result.

POST /api/tryon/{id}/reject
Purpose:
Reject try-on result.

## AI Service API

POST /generate-tryon
Purpose:
Python FastAPI receives user image and clothing image, runs CAT-VTON, and returns generated image.

Request:
{
  "personImagePath": "uploads/users/front.jpg",
  "clothImagePath": "uploads/clothes/tshirt.png"
}

Response:
{
  "generatedImagePath": "outputs/result.png"
}
