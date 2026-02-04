# API Endpoint Review Checklist

**Generated**: 2026-01-30
**Spec Version**: OpenAPI 3.0.3 (Contract-Complete)

## A) Authentication Module

### 1. Login
- **Endpoint**: `POST /api/auth/login`
- **Auth**: Public
- **Headers**: None
- **Validation**:
  - `username` and `password` required.
- **QA Test Cases**:
  - [ ] **Happy Path**: Valid credentials -> 200 OK + `accessToken` in body + `refreshToken` HttpOnly Cookie.
  - [ ] **Failure**: Invalid credentials -> 401 Unauthorized (`Invalid credentials`).
  - [ ] **Validation**: Empty body -> 400 BadRequest.

### 2. Refresh Token
- **Endpoint**: `POST /api/auth/refresh`
- **Auth**: Public (Cookie-based)
- **Headers**: Cookie `refreshToken`
- **Validation**:
  - Valid, non-expired cookie required.
  - Rate limited (`auth_refresh`).
- **QA Test Cases**:
  - [ ] **Happy Path**: Valid cookie -> 200 OK + New `accessToken`.
  - [ ] **Failure**: No cookie -> 401 Unauthorized.
  - [ ] **Failure**: Expired/Revoked cookie -> 401 Unauthorized.

### 3. Logout
- **Endpoint**: `POST /api/auth/logout`
- **Auth**: Public
- **Headers**: Cookie `refreshToken` (Optional)
- **QA Test Cases**:
  - [ ] **Happy Path**: Call -> 200 OK + Cookie cleared.

### 4. Get Current User
- **Endpoint**: `GET /api/auth/me`
- **Auth**: Bearer
- **Headers**: `Authorization`
- **QA Test Cases**:
  - [ ] **Happy Path**: Valid token -> 200 OK + User Profile (Roles, Departments).
  - [ ] **Failure**: Invalid/Missing token -> 401 Unauthorized.

---

## B) Inventory Requests

### 5. List Requests
- **Endpoint**: `GET /api/inventory/requests`
- **Auth**: Bearer
- **Permission**: `inventory.request.view`
- **Logic**: Scoped by `OWN`, `DEPT`, `WAREHOUSE`, or `GLOBAL`.
- **QA Test Cases**:
  - [ ] **Scope Check**: User (OWN) sees only their requests.
  - [ ] **Scope Check**: Admin (GLOBAL) sees all requests.
  - [ ] **Pagination**: Verify `page` and `pageSize` work.

### 6. Create Draft
- **Endpoint**: `POST /api/inventory/requests`
- **Auth**: Bearer
- **Permission**: `inventory.request.create`
- **Validation**:
  - `warehouseId` must exist.
  - `departmentId` must match user's department (if restricted).
  - `lines` > 0.
- **QA Test Cases**:
  - [ ] **Happy Path**: Valid payload -> 200 OK + New ID + Status `DRAFT`.
  - [ ] **Validation**: Invalid data -> 400 BadRequest.

### 7. Get Request Detail
- **Endpoint**: `GET /api/inventory/requests/{requestId}`
- **Auth**: Bearer
- **Permission**: `inventory.request.view` (Scoped)
- **QA Test Cases**:
  - [ ] **Happy Path**: Valid ID (Allowed scope) -> 200 OK.
  - [ ] **Security**: Valid ID (Restricted scope) -> 403 Forbidden (or 404).

### 8. Update Draft
- **Endpoint**: `PUT /api/inventory/requests/{requestId}`
- **Auth**: Bearer
- **Permission**: `inventory.request.edit`
- **Validation**:
  - Request Status MUST be `DRAFT`.
  - User MUST be owner (or Admin).
- **QA Test Cases**:
  - [ ] **Happy Path**: DRAFT status -> 200 OK + Updates saved.
  - [ ] **Logic**: Status `IN_WORKFLOW` -> 400 BadRequest (`Cannot update...`).

### 9. Submit Request
- **Endpoint**: `POST /api/inventory/requests/{requestId}/submit`
- **Auth**: Bearer
- **Permission**: `inventory.request.edit`
- **Validation**: Request Status must be `DRAFT`.
- **QA Test Cases**:
  - [ ] **Happy Path**: DRAFT -> 200 OK + Status `IN_WORKFLOW` + Workflow Instance active.
  - [ ] **Double Submit**: Already submitted -> 400 BadRequest.

### 10. Cancel Request
- **Endpoint**: `POST /api/inventory/requests/{requestId}/cancel`
- **Auth**: Bearer
- **Permission**: `inventory.request.edit`
- **Validation**: No active reservations exist.
- **QA Test Cases**:
  - [ ] **Happy Path**: No reservations -> 200 OK + Status `CANCELLED`.
  - [ ] **Logic**: Active reservations exist -> 400 BadRequest (`Must release valid reservations...`).

---

## C) Workflow Tasks

### 11. My Tasks
- **Endpoint**: `GET /api/workflow/tasks/my`
- **Auth**: Bearer
- **Logic**: Returns tasks where User is `Assignee` (PENDING) OR `Claimer` (CLAIMED).
- **QA Test Cases**:
  - [ ] **Filter**: Verify only relevant tasks returned.

### 12. Claim Task
- **Endpoint**: `POST /api/workflow/tasks/{taskId}/claim`
- **Auth**: Bearer
- **Logic**: Atomic claim.
- **Validation**:
  - Task Status `AVAILABLE`.
  - User is in candidate list.
- **QA Test Cases**:
  - [ ] **Happy Path**: Available -> 200 OK + Status `CLAIMED` + `ClaimedByUserId` set.
  - [ ] **Race Condition**: 2 users claim simultaneously -> One gets 200, other gets 409 Conflict.
  - [ ] **Logic**: Already claimed -> 409 Conflict.

### 13. Process Action
- **Endpoint**: `POST /api/workflow/tasks/{taskId}/action`
- **Auth**: Bearer
- **Validation**:
  - Task Status `CLAIMED`.
  - `ClaimedByUserId` matches current user.
- **QA Test Cases**:
  - [ ] **Happy Path** (Approve): 200 OK + Workflow advances.
  - [ ] **Happy Path** (Reject): 200 OK + Workflow terminates/transitions.
  - [ ] **Security**: User attempts action on task claimed by SOMEONE ELSE -> 403 Forbidden / Error.

---

## D) Fulfillment (Idempotent)

### 14. Reserve Stock
- **Endpoint**: `POST /api/inventory/requests/{requestId}/fulfillment/reserve`
- **Auth**: Bearer
- **Permission**: `inventory.fulfillment.execute`
- **Headers**: `X-Idempotency-Key` (REQUIRED)
- **Validation**:
  - Workflow Step MUST be `FULFILLMENT`.
- **QA Test Cases**:
  - [ ] **Happy Path**: First call -> 200 OK + Reservation Created + Stock Reserved.
  - [ ] **Idempotency Replay**: Second call (Same Key) -> 200 OK + Same Response + NO SIDE EFFECT.
  - [ ] **Logic**: Wrong Workflow Step -> 400 BadRequest.

### 15. Release Stock
- **Endpoint**: `POST /api/inventory/requests/{requestId}/fulfillment/release`
- **Auth**: Bearer
- **Permission**: `inventory.fulfillment.execute`
- **Headers**: `X-Idempotency-Key` (REQUIRED)
- **Validation**: Active reservation exists.
- **QA Test Cases**:
  - [ ] **Happy Path**: 200 OK + Reservation Cancelled + Stock Released.
  - [ ] **Idempotency Replay**: Verified.

### 16. Issue Stock
- **Endpoint**: `POST /api/inventory/requests/{requestId}/fulfillment/issue`
- **Auth**: Bearer
- **Permission**: `inventory.fulfillment.execute`
- **Headers**: `X-Idempotency-Key` (REQUIRED)
- **Validation**: Workflow Step MUST be `FULFILLMENT`.
- **QA Test Cases**:
  - [ ] **Scenario A**: Active Reservation -> Consumes Reservation + Reduces OnHand.
  - [ ] **Scenario B**: No Reservation -> Reduces OnHand directly.
  - [ ] **Idempotency Replay**: Verified.

---

## E) Reference & Admin

### 17. Reference Data
- **Endpoints**: Products, Warehouses, Categories, Stock Levels
- **Auth**: Bearer
- **QA Test Cases**:
  - [ ] **List**: 200 OK + Data returned.
  - [ ] **Filters**: Search query works.

### 18. Admin - User/Role Management
- **Endpoints**: `Roles`, `Users`, `Permissions`
- **Auth**: Bearer
- **Permission**: `admin.rbac.manage` / `admin.users.manage`
- **QA Test Cases**:
  - [ ] **CRUD**: Create/Update works.
  - [ ] **Validation**: Duplicate username checks.
  - [ ] **Security**: Non-admin user -> 403 Forbidden.

### 19. System
- **Endpoints**: Health, Version
- **Auth**: Public
- **QA Test Cases**:
  - [ ] **Health**: Returns "Healthy".
