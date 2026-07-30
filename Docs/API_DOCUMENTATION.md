# HgsPortal API Documentation

## Base URL
```
https://api.hgs.vn:8445/ApiCore/
```
## TK Đã tạo
```
user:HK01
mk:123456
```
## Authentication

The API uses JWT (JSON Web Token) for authentication. 

### Rate Limiting
- **100 requests per minute** per user/host
- Returns HTTP 429 when limit exceeded
- Use the `Retry-After` header to wait before retrying

---

## Authentication Endpoints

### 1. Login
Authenticate user and receive access token.

**Endpoint:** `POST /auth/login`

**Request Body:**
```json
{
  "username": "string",
  "password": "string"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Login successful",
  "statusCode": 200,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "string",
    "expiresAt": "2024-01-15T12:00:00Z"
  }
}
```

**Error Responses:**
- `400 Bad Request` - Missing username or password
- `401 Unauthorized` - Invalid credentials or inactive user

---

### 2. Refresh Token
Refresh access token using refresh token.

**Endpoint:** `POST /auth/refresh-token`

**Request Body:**
```json
{
  "refreshToken": "string"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "statusCode": 200,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "string",
    "expiresAt": "2024-01-15T12:00:00Z"
  }
}
```

**Error Responses:**
- `400 Bad Request` - Missing refresh token
- `401 Unauthorized` - Invalid or expired refresh token

---

### 3. Logout
Invalidate refresh token.

**Endpoint:** `POST /auth/logout`

**Request Body:**
```json
{
  "refreshToken": "string"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Logout successful",
  "statusCode": 200
}
```

**Error Responses:**
- `400 Bad Request` - Missing refresh token
- `404 Not Found` - Refresh token not found

---

## Using Access Token

Include the access token in the Authorization header for authenticated requests:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Token Expiry:**
- Access token expires in **60 minutes**
- Refresh token expires in **7 days**
- Implement automatic token refresh before expiry

---

## Customer Satisfaction Endpoints

All customer satisfaction endpoints require authentication.

### Devices

#### Get All Devices
**Endpoint:** `GET /api/customersatisfaction/devices`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Devices retrieved successfully",
  "statusCode": 200,
  "data": [
    {
      "id": 1,
      "deviceName": "iPad Pro",
      "deviceIdentifier": "IPAD-001",
      "status": "active",
      "lastSeenAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

#### Get Device by ID
**Endpoint:** `GET /api/customersatisfaction/devices/{id}`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Device retrieved successfully",
  "statusCode": 200,
  "data": {
    "id": 1,
    "deviceName": "iPad Pro",
    "deviceIdentifier": "IPAD-001",
    "status": "active",
    "lastSeenAt": "2024-01-15T10:30:00Z"
  }
}
```

#### Create Device
**Endpoint:** `POST /api/customersatisfaction/devices`

**Request Body:**
```json
{
  "deviceName": "iPad Pro",
  "deviceIdentifier": "IPAD-001",
  "status": "active",
  "lastSeenAt": "2024-01-15T10:30:00Z"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "message": "Device created successfully",
  "statusCode": 201,
  "data": {
    "id": 1,
    "deviceName": "iPad Pro",
    "deviceIdentifier": "IPAD-001",
    "status": "active",
    "lastSeenAt": "2024-01-15T10:30:00Z"
  }
}
```

#### Update Device
**Endpoint:** `PUT /api/customersatisfaction/devices/{id}`

**Request Body:**
```json
{
  "deviceName": "iPad Pro Updated",
  "deviceIdentifier": "IPAD-001",
  "status": "inactive",
  "lastSeenAt": "2024-01-15T10:30:00Z"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Device updated successfully",
  "statusCode": 200,
  "data": {
    "id": 1,
    "deviceName": "iPad Pro Updated",
    "deviceIdentifier": "IPAD-001",
    "status": "inactive",
    "lastSeenAt": "2024-01-15T10:30:00Z"
  }
}
```

#### Delete Device
**Endpoint:** `DELETE /api/customersatisfaction/devices/{id}`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Device deleted successfully",
  "statusCode": 200
}
```

---

### Unsatisfied Reasons

#### Get All Reasons
**Endpoint:** `GET /api/customersatisfaction/reasons`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Reasons retrieved successfully",
  "statusCode": 200,
  "data": [
    {
      "id": 1,
      "reasonName": "Poor Service",
      "status": "active"
    }
  ]
}
```

#### Get Reason by ID
**Endpoint:** `GET /api/customersatisfaction/reasons/{id}`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Reason retrieved successfully",
  "statusCode": 200,
  "data": {
    "id": 1,
    "reasonName": "Poor Service",
    "status": "active"
  }
}
```

#### Create Reason
**Endpoint:** `POST /api/customersatisfaction/reasons`

**Request Body:**
```json
{
  "reasonName": "Poor Service",
  "status": "active"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "message": "Reason created successfully",
  "statusCode": 201,
  "data": {
    "id": 1,
    "reasonName": "Poor Service",
    "status": "active"
  }
}
```

#### Update Reason
**Endpoint:** `PUT /api/customersatisfaction/reasons/{id}`

**Request Body:**
```json
{
  "reasonName": "Poor Service Updated",
  "status": "inactive"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Reason updated successfully",
  "statusCode": 200,
  "data": {
    "id": 1,
    "reasonName": "Poor Service Updated",
    "status": "inactive"
  }
}
```

#### Delete Reason
**Endpoint:** `DELETE /api/customersatisfaction/reasons/{id}`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Reason deleted successfully",
  "statusCode": 200
}
```

---

### Evaluations

#### Get All Evaluations
**Endpoint:** `GET /api/customersatisfaction/evaluations`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Evaluations retrieved successfully",
  "statusCode": 200,
  "data": [
    {
      "id": 1,
      "flightId": 123,
      "deviceId": 1,
      "deviceName": "iPad Pro",
      "rating": 5,
      "comment": "Great service!",
      "createdAt": "2024-01-15T10:30:00Z",
      "reasonIds": [1, 2]
    }
  ]
}
```

#### Get Evaluation by ID
**Endpoint:** `GET /api/customersatisfaction/evaluations/{id}`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Evaluation retrieved successfully",
  "statusCode": 200,
  "data": {
    "id": 1,
    "flightId": 123,
    "deviceId": 1,
    "deviceName": "iPad Pro",
    "rating": 5,
    "comment": "Great service!",
    "createdAt": "2024-01-15T10:30:00Z",
    "reasonIds": [1, 2]
  }
}
```

#### Create Evaluation
**Endpoint:** `POST /api/customersatisfaction/evaluations`

**Request Body:**
```json
{
  "flightId": 123,
  "deviceId": 1,
  "rating": 5,
  "comment": "Great service!",
  "reasonIds": [1, 2]
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "message": "Evaluation created successfully",
  "statusCode": 201,
  "data": {
    "id": 1,
    "flightId": 123,
    "deviceId": 1,
    "rating": 5,
    "comment": "Great service!",
    "createdAt": "2024-01-15T10:30:00Z",
    "reasonIds": [1, 2]
  }
}
```

#### Update Evaluation
**Endpoint:** `PUT /api/customersatisfaction/evaluations/{id}`

**Request Body:**
```json
{
  "flightId": 123,
  "deviceId": 1,
  "rating": 4,
  "comment": "Good service",
  "reasonIds": [1]
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Evaluation updated successfully",
  "statusCode": 200,
  "data": {
    "id": 1,
    "flightId": 123,
    "deviceId": 1,
    "rating": 4,
    "comment": "Good service",
    "createdAt": "2024-01-15T10:30:00Z",
    "reasonIds": [1]
  }
}
```

#### Delete Evaluation
**Endpoint:** `DELETE /api/customersatisfaction/evaluations/{id}`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Evaluation deleted successfully",
  "statusCode": 200
}
```

---

## Flight Endpoints

All flight endpoints require authentication.
```

### Get Flight by ID
**Endpoint:** `GET /api/flight/{id}`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": {
    "id": 1,
    "flightId": 123,
    "flightNo": "VN123",
    "flightDate": "2024-01-15",
    "arrDep": "A",
    "route": "SGN-HAN",
    "flightDateTime": "2024-01-15T10:00:00Z",
    "nature": "Scheduled",
    "remark": null,
    "status": "On Time",
    "acno": "VN-A123",
    "actp": "A320"
  }
}
```

**Error Responses:**
- `404 Not Found` - Flight with specified ID not found

### Search Flights
**Endpoint:** `GET /api/flight/search`

**Query Parameters:**
- `flightNo` (optional) - Flight number (e.g., "VN123")
- `flightDate` (optional) - Flight date (e.g., "2024-01-15")

**Examples:**
- `/api/flight/search?flightNo=VN123` - Search by flight number
- `/api/flight/search?flightDate=15/05/2026` - Search by date
- `/api/flight/search?flightNo=VN123&flightDate=15/01/2026` - Search by both
- `/api/flight/search` - Get all flights (no filters)

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": [
    {
      "id": 1,
      "flightId": 123,
      "flightNo": "VN123",
      "flightDate": "2024-01-15",
      "arrDep": "A",
      "route": "SGN-HAN",
      "flightDateTime": "2024-01-15T10:00:00Z",
      "nature": "Scheduled",
      "remark": null,
      "status": "On Time",
      "acno": "VN-A123",
      "actp": "A320"
    }
  ]
}
```

---

## User Roles Endpoints

All user role endpoints require authentication.

### Get All User Role Assignments
**Endpoint:** `GET /api/userroles`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": [
    {
      "id": 1,
      "userId": 1,
      "username": "john.doe",
      "userFullName": "John Doe",
      "roleId": 2,
      "roleCode": "ADMIN",
      "roleName": "Administrator",
      "assignedAt": "2024-01-15T10:00:00Z",
      "assignedBy": 1,
      "expiredAt": null
    }
  ]
}
```

### Get User Role Assignment by ID
**Endpoint:** `GET /api/userroles/{id}`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": {
    "id": 1,
    "userId": 1,
    "username": "john.doe",
    "userFullName": "John Doe",
    "roleId": 2,
    "roleCode": "ADMIN",
    "roleName": "Administrator",
    "assignedAt": "2024-01-15T10:00:00Z",
    "assignedBy": 1,
    "expiredAt": null
  }
}
```

**Error Responses:**
- `404 Not Found` - User role assignment with specified ID not found

### Get User Roles by User ID
**Endpoint:** `GET /api/userroles/by-user/{userId}`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": [
    {
      "id": 1,
      "userId": 1,
      "username": "john.doe",
      "userFullName": "John Doe",
      "roleId": 2,
      "roleCode": "ADMIN",
      "roleName": "Administrator",
      "assignedAt": "2024-01-15T10:00:00Z",
      "assignedBy": 1,
      "expiredAt": null
    }
  ]
}
```

### Get User Roles by Role ID
**Endpoint:** `GET /api/userroles/by-role/{roleId}`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": [
    {
      "id": 1,
      "userId": 1,
      "username": "john.doe",
      "userFullName": "John Doe",
      "roleId": 2,
      "roleCode": "ADMIN",
      "roleName": "Administrator",
      "assignedAt": "2024-01-15T10:00:00Z",
      "assignedBy": 1,
      "expiredAt": null
    }
  ]
}
```

### Assign Role to User
**Endpoint:** `POST /api/userroles`

**Request Body:**
```json
{
  "userId": 1,
  "roleId": 2,
  "expiredAt": "2024-12-31T23:59:59Z"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": {
    "id": 1,
    "userId": 1,
    "roleId": 2,
    "assignedAt": "2024-01-15T10:00:00Z",
    "expiredAt": "2024-12-31T23:59:59Z"
  }
}
```

**Error Responses:**
- `404 Not Found` - User or role not found
- `409 Conflict` - User already has this role assigned

### Update User Role Assignment
**Endpoint:** `PUT /api/userroles/{id}`

**Request Body:**
```json
{
  "expiredAt": "2024-12-31T23:59:59Z"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": {
    "id": 1,
    "userId": 1,
    "roleId": 2,
    "assignedAt": "2024-01-15T10:00:00Z",
    "expiredAt": "2024-12-31T23:59:59Z"
  }
}
```

**Error Responses:**
- `404 Not Found` - User role assignment not found

### Remove Role from User
**Endpoint:** `DELETE /api/userroles/{id}`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": true
}
```

**Error Responses:**
- `404 Not Found` - User role assignment not found
- `409 Conflict` - Cannot remove the last role from a user

### Assign Multiple Roles to User
**Endpoint:** `POST /api/userroles/assign-multiple`

**Request Body:**
```json
{
  "userId": 1,
  "roleIds": [2, 3, 4],
  "expiredAt": "2024-12-31T23:59:59Z"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Roles assigned successfully",
  "statusCode": 200,
  "data": true
}
```

**Error Responses:**
- `404 Not Found` - User or role not found

### Remove Multiple Roles from User
**Endpoint:** `POST /api/userroles/remove-multiple`

**Request Body:**
```json
{
  "userId": 1,
  "roleIds": [2, 3]
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Roles removed successfully",
  "statusCode": 200,
  "data": true
}
```

**Error Responses:**
- `404 Not Found` - User not found
- `409 Conflict` - Cannot remove the last role from a user

---

## Standard Response Format

All API responses follow this format:

```json
{
  "success": true,
  "message": "Operation message",
  "statusCode": 200,
  "data": { ... }
}
```

For error responses:

```json
{
  "success": false,
  "message": "Error message",
  "statusCode": 400,
  "data": null
}
```

---



