# InvServer - Inventory Management System

## Security Hardening

### 1. Authentication & Session Management
- **Refresh Token Entropy**: Generated using `System.Security.Cryptography.RandomNumberGenerator` (64 bytes), providing high collision resistance.
- **Token Hashing**: Refresh tokens are stored in the database hashed with `SHA256`. The raw token is never stored.
- **Cookie Security**: `refreshToken` is delivered via `HttpOnly`, `Secure`, and `SameSite=Strict` cookies to mitigate XSS and CSRF.
- **Origin Validation**: Mutation endpoints (`/refresh`, `/logout`) perform a server-side check on `Origin` and `Referer` headers.

### 2. Authorization (RBAC)
- **Scoped Permission Engine**: All permission checks require BOTH a permission code (e.g., `inventory.request.list`) and an optional scope evaluation.
- **Permission Caching**: Permissions are cached per user, keyed by `(UserId, PermVersion)`. Invalidation is instantaneous upon incrementing a user's `PermVersion`.
- **Query-Time Integrity**: List endpoints apply mandatory scope filters at the SQL query level (e.g., `WHERE WarehouseId IN (...)`) to prevent horizontal data leakage.

### 3. Hardening Recommendations (Production)
- **Rate Limiting**: Implement `Microsoft.AspNetCore.RateLimiting` on `/auth/login` to prevent brute-force attacks (e.g., 5 attempts per min per IP).
- **Account Lockout**: Implement a `FailedLoginCount` and `LockoutEnd` on the `USER` table.
- **Audit Coverage**: Ensure all state-changing actions (Stock Movements, Workflow Transitions) are logged with a `UserId` and `CorrelationId`.
- **Secret Storage**: In production, move JWT Secrets and Connection Strings to Azure Key Vault, AWS Secrets Manager, or Environment Variables.
