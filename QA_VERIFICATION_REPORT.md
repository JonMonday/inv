# QA Verification Report

**Status**: ⚠️ **Manual Verification Required**
**Reason**: Automated DB environment setup on local agent timed out (Docker input stream limitations).

## Frontend Readiness Status
✅ **Contract Complete**: The `swagger.json` accurately reflects the implemented backend controllers.
- **Strict Typing**: All DTOs and Response headers defined.
- **Idempotency**: `X-Idempotency-Key` enforced in spec and code.
- **RBAC**: Permissions documented.

## 1. Manual Execution Guide
Since the automated agent could not complete the full flow, please run the following on your local environment:

### Step 1: Database Setup
Run the SQL scripts strictly in this order using your preferred SQL tool (SSMS, Azure Data Studio, or sqlcmd):

1. **Schema**: Run `schema.sql` (Creates tables).
2. **Lookup Data**: Run `mnt/data/static-seeder.sql`.
3. **Workflows**: Run `mnt/data/template-seeder.sql`.
4. **Sample Data**: Run `mnt/data/realworld-seeder.sql`.

### Step 2: Fix User Passwords
The seeders insert users *without* password hashes. You must run this SQL to verify:
```sql
UPDATE dbo.[USER] 
SET PasswordHash = '$2a$11$jMWFDIa/D7DTSxQIhIEqo.rWZu.rZfPqv8PGel1ZcX8ewlmrV7l56' 
WHERE Username = 'admin';
-- Password is now: admin123
```

### Step 3: Run Verification Script
I have generated a specialized Python E2E script that performs the full QA loop.

1. Ensure API is running: `dotnet run` (Port 5119).
2. Run script:
   ```bash
   python3 qa_verify.py
   ```

**Expected Output**:
```text
[PASS] Login success
[PASS] User: admin
[PASS] Request Created: {ID}
[PASS] Submitted
[PASS] Found Task: {TaskID}
[PASS] Claimed
[PASS] Approved
[PASS] Reservation Success
[PASS] Replay Success (200 OK)
[PASS] E2E Verification Complete
```

## 2. Troubleshooting Matrix

| Issue | Symptom | Fix |
|-------|---------|-----|
| **Login Failed** | `401 Unauthorized` | Ensure you ran the `UPDATE PasswordHash` SQL command. |
| **Pending Migration** | API crashes on start | Check connection string in `appsettings.json`. |
| **Idempotency 400** | `BadRequest` on Reserve | Ensure Request is in `FULFILLMENT` step (Step 3 in output should say "Submitted"). |
| **Task Claim 409** | `Conflict` | Task already claimed. Run `GET /workflow/tasks/my` to check current assignee. |

## 3. Artifact Validation
- **Swagger**: Verified against `InventoryController`, `WorkflowController` (Permissions, Headers).
- **Checklist**: `ENDPOINTS_TO_REVIEW.md` covers all negative cases.
