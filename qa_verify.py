import urllib.request
import urllib.parse
import json
import uuid
import sys

BASE_URL = "http://localhost:5119/api"

def log(msg, success=None):
    mark = "[?]"
    if success is True: mark = "[PASS]"
    elif success is False: mark = "[FAIL]"
    print(f"{mark} {msg}")

def req(method, endpoint, data=None, token=None, headers=None):
    url = f"{BASE_URL}{endpoint}"
    if headers is None: headers = {}
    
    headers['Content-Type'] = 'application/json'
    if token:
        headers['Authorization'] = f"Bearer {token}"
    
    body = None
    if data:
        body = json.dumps(data).encode('utf-8')
    
    request = urllib.request.Request(url, data=body, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request) as response:
            res_body = response.read().decode('utf-8')
            return response.status, json.loads(res_body) if res_body else {}
    except urllib.error.HTTPError as e:
        res_body = e.read().decode('utf-8')
        try:
            return e.code, json.loads(res_body)
        except:
            return e.code, res_body

def run():
    log("Starting E2E Verification")
    
    # 1. Login
    log("Testing Login...")
    status, res = req("POST", "/auth/login", {"username":"admin", "password":"admin123"})
    if status != 200:
        log(f"Login failed: {status} {res}", False)
        return
    token = res['data']['accessToken']
    log("Login success", True)
    
    # 2. Get Me
    log("Testing GetMe...")
    status, res = req("GET", "/auth/me", token=token)
    if status == 200:
        log(f"User: {res['data']['username']}", True)
    else:
        log("GetMe failed", False)

    # 3. Create Request
    log("Creating Inventory Request...")
    # Need warehouseId - assuming 1 from seeders
    payload = {
        "warehouseId": 1,
        "departmentId": 1, # assuming 1 exists
        "notes": "E2E Test",
        "lines": [{"productId": 1, "qtyRequested": 10}]
    }
    status, res = req("POST", "/inventory/requests", payload, token=token)
    if status != 200:
        log(f"Create failed: {res}", False)
        return
    
    req_id = res['data']
    log(f"Request Created: {req_id}", True)
    
    # 4. Submit
    log("Submitting Request...")
    status, res = req("POST", f"/inventory/requests/{req_id}/submit", token=token)
    if status == 200:
        log("Submitted", True)
    else:
        log(f"Submit failed: {res}", False)
        return

    # 5. Workflow: List My Tasks
    log("Checking Tasks...")
    status, res = req("GET", "/workflow/tasks/my", token=token)
    tasks = res.get('data', [])
    target_task = next((t for t in tasks if str(t.get('instanceDetails')) == str(req_id) or t.get('workflowInstanceId') == req_id), None) # simplified matching
    # Actually instanceDetails is probably the key "REQ-1001", not the ID.
    # Let's just create a new one and pick the latest task.
    if tasks:
        target_task = tasks[-1] 
        # CAUTION: might be risky in shared env, but OK for local.
        task_id = target_task['workflowTaskId']
        log(f"Found Task: {task_id}", True)
    else:
        log("No tasks found. User might not be assignee.", False)
        # If admin isn't assignee, we can't proceed with Claim.
        return

    # 6. Claim
    log(f"Claiming Task {task_id}...")
    status, res = req("POST", f"/workflow/tasks/{task_id}/claim", token=token)
    if status == 200:
        log("Claimed", True)
    elif status == 409:
        log("Already claimed (retry?)", False)
    else:
        log(f"Claim failed: {res}", False)
        return

    # 7. Approve
    log("Approving Task...")
    status, res = req("POST", f"/workflow/tasks/{task_id}/action", {"actionCode": "APPROVE", "notes": "QA Approved"}, token=token)
    if status == 200:
        log("Approved", True)
    else:
        log(f"Approve failed: {res}", False)
        return

    # 8. Fulfillment (Check if in fulfillment)
    # We might need to refresh request to see status.
    status, res = req("GET", f"/inventory/requests/{req_id}", token=token)
    # Assuming status logic... skipping to forceful fulfillment attempt.
    
    # Reserve
    idemp_key = str(uuid.uuid4())
    log(f"Reserving Stock (Key: {idemp_key})...")
    f_payload = {
        "movementTypeCode": "RESERVE",
        "warehouseId": 1,
        "requestId": req_id,
        "lines": [{"productId": 1, "qtyDeltaReserved": 5, "qtyDeltaOnHand": 0}]
    }
    status, res = req("POST", f"/inventory/requests/{req_id}/fulfillment/reserve", f_payload, token=token, headers={"X-Idempotency-Key": idemp_key})
    
    if status == 200:
        log("Reservation Success", True)
    elif status == 400:
        log("Reservation Failed (Likely not in Fulfillment step yet - Workflow multi-step?)", False)
        log(f"Details: {res}")
        return
    else:
        log(f"Reservation Error: {res}", False)
        return

    # Replay
    log("Testing Idempotency Replay...")
    status, res = req("POST", f"/inventory/requests/{req_id}/fulfillment/reserve", f_payload, token=token, headers={"X-Idempotency-Key": idemp_key})
    if status == 200:
        log("Replay Success (200 OK)", True)
    else:
        log(f"Replay Failed: {status}", False)

    log("E2E Verification Complete")

if __name__ == "__main__":
    run()
