# Customer Satisfaction API - Frontend Integration Guide

## Overview
Customer Satisfaction API provides endpoints for managing devices, unsatisfied reasons, and customer evaluations. Base URL: `/api/customersatisfaction`

## Authentication
All endpoints require authentication via JWT Bearer token in Authorization header:
```
Authorization: Bearer {access_token}
```

## Response Format
All responses follow the standard `ApiResponse` format:
```json
{
  "success": true,
  "message": "Operation successful",
  "data": { ... },
  "statusCode": 200
}
```

---

## Devices Endpoints

### Get All Devices
**Endpoint:** `GET /api/customersatisfaction/devices`

**Response:**
```json
{
  "success": true,
  "message": "Devices retrieved successfully",
  "data": [
    {
      "id": 1,
      "deviceName": "Tablet 01",
      "deviceIdentifier": "TAB-001",
      "status": "Active",
      "lastSeenAt": "2026-07-31T10:00:00Z"
    }
  ],
  "statusCode": 200
}
```

### Get Device by ID
**Endpoint:** `GET /api/customersatisfaction/devices/{id}`

### Create Device
**Endpoint:** `POST /api/customersatisfaction/devices`

**Request Body:**
```json
{
  "deviceName": "Tablet 02",
  "deviceIdentifier": "TAB-002",
  "status": "Active"
}
```

### Update Device
**Endpoint:** `PUT /api/customersatisfaction/devices/{id}`

**Request Body:**
```json
{
  "deviceName": "Tablet 02 Updated",
  "deviceIdentifier": "TAB-002",
  "status": "Inactive"
}
```

### Delete Device
**Endpoint:** `DELETE /api/customersatisfaction/devices/{id}`

---

## Reasons Endpoints

### Get All Reasons
**Endpoint:** `GET /api/customersatisfaction/reasons`

**Response:**
```json
{
  "success": true,
  "message": "Reasons retrieved successfully",
  "data": [
    {
      "id": 1,
      "reasonName": "Slow service",
      "status": "Active"
    }
  ],
  "statusCode": 200
}
```

### Get Reason by ID
**Endpoint:** `GET /api/customersatisfaction/reasons/{id}`

### Create Reason
**Endpoint:** `POST /api/customersatisfaction/reasons`

**Request Body:**
```json
{
  "reasonName": "Rude staff",
  "status": "Active"
}
```

### Update Reason
**Endpoint:** `PUT /api/customersatisfaction/reasons/{id}`

**Request Body:**
```json
{
  "reasonName": "Rude staff behavior",
  "status": "Active"
}
```

### Delete Reason
**Endpoint:** `DELETE /api/customersatisfaction/reasons/{id}`

---

## Evaluations Endpoints

### Get All Evaluations
**Endpoint:** `GET /api/customersatisfaction/evaluations`

**Response:**
```json
{
  "success": true,
  "message": "Evaluations retrieved successfully",
  "data": [
    {
      "id": 1,
      "flightId": "FL-001",
      "staffUserId": 10,
      "deviceId": 1,
      "deviceName": "Tablet 01",
      "checkinCounterName": "Counter A",
      "ratingLevel": 5,
      "evaluationType": "Positive",
      "reasonIds": []
    }
  ],
  "statusCode": 200
}
```

### Get Evaluation by ID
**Endpoint:** `GET /api/customersatisfaction/evaluations/{id}`

### Create Evaluation
**Endpoint:** `POST /api/customersatisfaction/evaluations`

**Request Body:**
```json
{
  "flightId": "FL-002",
  "staffUserId": 10,
  "deviceId": 1,
  "checkinCounterName": "Counter B",
  "ratingLevel": 3,
  "evaluationType": "Neutral",
  "reasonIds": [1, 2]
}
```

### Update Evaluation
**Endpoint:** `PUT /api/customersatisfaction/evaluations/{id}`

**Request Body:**
```json
{
  "flightId": "FL-002",
  "staffUserId": 10,
  "deviceId": 1,
  "checkinCounterName": "Counter B",
  "ratingLevel": 4,
  "evaluationType": "Positive",
  "reasonIds": [1]
}
```

### Delete Evaluation
**Endpoint:** `DELETE /api/customersatisfaction/evaluations/{id}`

---

## HTTP Status Codes

- **200 OK** - Successful GET, PUT, DELETE
- **201 Created** - Successful POST
- **400 Bad Request** - Invalid input data
- **404 Not Found** - Resource not found
- **409 Conflict** - Duplicate resource or constraint violation
