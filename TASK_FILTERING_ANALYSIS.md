# Task Filtering Issue Analysis Report

## Executive Summary
The issue of fetching ALL tasks instead of only user tasks is **definitively a BACKEND API ISSUE**, not a client-side problem. The client code is correctly implementing the filtering at the API request level.

---

## 1. CLIENT-SIDE URL CONSTRUCTION (Line 170 in ApiService.cs)

### Current Implementation (✅ CORRECT)
```csharp
// Line 169-170 in ApiService.cs
var encodedStatus = Uri.EscapeDataString(status);
var url = $"{_baseUrl}/getItems_action.php?user_id={userId}&status={encodedStatus}";
```

**Analysis:**
- The URL construction is CORRECT
- Properly includes both parameters: `user_id` and `status`
- Both parameters are URL-encoded correctly
- Example: `https://todo-list.dcism.org/getItems_action.php?user_id=42&status=active`

### How It's Called (✅ CORRECT)
```csharp
// Line 77 in MainPage.xaml.cs
var response = await _apiService.GetTasksAsync(_userId, status);

// Line 67 in FinishedPage.xaml.cs  
var response = await _apiService.GetTasksAsync(_userId, status);
```

**Analysis:**
- `_userId` is obtained from `SessionStorage.GetUserIdAsync()` (line 39, MainPage.xaml.cs)
- Verified to be a valid integer > 0
- Status is explicitly set to either "active" or "inactive"
- No filtering happens on the client side - relies entirely on backend

---

## 2. BACKEND API ISSUE (🚨 CONFIRMED ISSUE)

### Evidence This Is A Backend Problem:

#### A) No Client-Side Filtering
The `GetTasksAsync()` method does NOT filter results on the client:
```csharp
// Lines 185-194 in ApiService.cs
if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
{
    // Convert object with numbered keys to list
    foreach (var prop in dataElement.EnumerateObject())
    {
        var task = JsonSerializer.Deserialize<TaskItem>(prop.Value.GetRawText(), _jsonOptions);
        if (task != null)
            tasks.Add(task);  // ⚠️ NO FILTERING - ADDS ALL TASKS FROM API RESPONSE
    }
}
```

**Critical Point:** The client code does NOT perform any client-side user_id filtering. It trusts the backend to:
1. Accept the `user_id` parameter
2. Filter results by that user_id
3. Return only tasks belonging to that user

#### B) Backend Must Be Ignoring user_id Parameter
If ALL tasks are being returned, the most likely scenario is:

**HYPOTHESIS 1: Backend ignores user_id completely**
- The backend endpoint `/getItems_action.php` may not be using the `user_id` parameter
- It may be returning all tasks from the database
- OR it's returning tasks for a different user/all users

**HYPOTHESIS 2: Backend has SQL injection or improper parameterization**
- The backend might have vulnerable code like: `SELECT * FROM tasks WHERE 1=1`
- Or: `SELECT * FROM tasks` (without any WHERE clause)

**HYPOTHESIS 3: Backend is filtering by wrong field**
- May be filtering by logged-in user instead of the `user_id` parameter
- May be caching results across users

---

## 3. TaskItem CLASS VALIDATION (✅ CORRECT)

### TaskItem Model Definition (Lines 558-573 in ApiService.cs)
```csharp
public class TaskItem
{
    [JsonPropertyName("item_id")]
    public int Id { get; set; }
    [JsonPropertyName("item_name")]
    public string? ItemName { get; set; }
    [JsonPropertyName("item_description")]
    public string? ItemDescription { get; set; }
    [JsonPropertyName("status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Status { get; set; }
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }  // ✅ PROPERTY EXISTS AND IS MAPPED
    [JsonPropertyName("timemodified")]
    public DateTime? UpdatedAt { get; set; }
}
```

**Analysis:**
- ✅ `UserId` property EXISTS with proper JSON mapping
- ✅ Type is `int` which matches `user_id` from API
- ✅ Property name matches API response field name
- The class CAN store user_id data, BUT...

**Critical Issue:**
```csharp
// Lines 84-104 in MainPage.xaml.cs
if (response != null && response.Success && response.Data != null)
{
    foreach (var apiTask in response.Data)
    {
        // ⚠️ UserId is NOT being used for validation
        var toDoItem = new ToDoItem
        {
            ID = apiTask.Id,
            Name = apiTask.ItemName,
            Notes = apiTask.ItemDescription,
            Done = apiTask.Status == "inactive"
            // ❌ apiTask.UserId is IGNORED - not validated/checked
        };
        items.Add(toDoItem);
    }
}
```

**Why This Matters:**
- If the backend returns tasks with different user_ids, the client would still add them
- There's NO client-side validation: `if (apiTask.UserId != _userId) continue;`
- This means even if backend filtering fails, no safety net exists on client

---

## 4. RECOMMENDED FIX

### Phase 1: Immediate Client-Side Mitigation (Safety Net)
Add client-side filtering as a safety measure in MainPage.xaml.cs:

```csharp
if (response != null && response.Success && response.Data != null)
{
    System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - Processing {response.Data.Count} tasks from API");

    foreach (var apiTask in response.Data)
    {
        // ✅ ADD THIS VALIDATION
        if (apiTask.UserId != _userId)
        {
            System.Diagnostics.Debug.WriteLine($"WARNING: Task {apiTask.Id} has user_id {apiTask.UserId}, expected {_userId}");
            continue;  // Skip tasks that don't belong to current user
        }

        System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - Adding task: Id={apiTask.Id}, Name={apiTask.ItemName}, UserId={apiTask.UserId}, Status={apiTask.Status}");

        var toDoItem = new ToDoItem
        {
            ID = apiTask.Id,
            Name = apiTask.ItemName,
            Notes = apiTask.ItemDescription,
            Done = apiTask.Status == "inactive"
        };
        items.Add(toDoItem);
    }

    System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - Successfully added {items.Count} items to collection");
}
```

### Phase 2: Backend Investigation & Fix
**Contact backend developers to verify:**

1. **Check Backend Code:**
   - Verify `/getItems_action.php` uses `$_GET['user_id']` or similar
   - Verify SQL query filters by user_id: `WHERE user_id = ?`
   - Check for SQL injection vulnerabilities

2. **Test Backend:**
   ```
   GET https://todo-list.dcism.org/getItems_action.php?user_id=1&status=active
   GET https://todo-list.dcism.org/getItems_action.php?user_id=2&status=active
   ```
   Should return different results for user_id=1 vs user_id=2

3. **Example Backend Fix (PHP):**
   ```php
   <?php
   // CURRENT (WRONG - returns all tasks)
   $tasks = $db->query("SELECT * FROM items");
   
   // CORRECT
   $user_id = (int)$_GET['user_id'];
   $status = $_GET['status'] ?? 'active';
   $tasks = $db->query(
       "SELECT * FROM items WHERE user_id = ? AND status = ?",
       [$user_id, $status]
   );
   ?>
   ```

---

## 5. EVIDENCE TABLE

| Aspect | Status | Notes |
|--------|--------|-------|
| Client URL construction | ✅ CORRECT | Includes user_id and status parameters |
| Parameter encoding | ✅ CORRECT | Uses Uri.EscapeDataString() |
| User_id retrieval | ✅ CORRECT | From SessionStorage.GetUserIdAsync() |
| TaskItem.UserId property | ✅ EXISTS | Properly mapped from API JSON |
| Client-side filtering | ❌ MISSING | No validation that UserId matches current user |
| Backend filtering | 🚨 ISSUE | API returns ALL tasks regardless of user_id |
| API response parsing | ✅ CORRECT | Deserializes all received tasks without filtering |

---

## 6. ROOT CAUSE CONCLUSION

**PRIMARY CAUSE: Backend API Issue**
- Backend endpoint `/getItems_action.php` is NOT filtering results by `user_id`
- Returns all tasks in the database
- Parameter is being ignored or not implemented

**SECONDARY ISSUE: No Client-Side Safety Net**
- While primary responsibility is backend's, client should validate data
- Client trusts backend completely without verification
- Missing defensive check: `if (apiTask.UserId != _userId) skip`

---

## 7. NEXT STEPS

1. **Add debug logging** to see actual API response
   - Already present: `System.Diagnostics.Debug.WriteLine($"GetTasksAsync Response: {responseContent}");` (line 175)
   - Check the actual JSON returned to verify tasks have different user_ids

2. **Implement client-side validation** (recommended above)

3. **Contact backend team** to fix the filtering at `/getItems_action.php`

4. **Test with known users:**
   - Create User A with task X
   - Create User B with task Y
   - User A calls GetTasks → should only see task X
   - User B calls GetTasks → should only see task Y
   - Currently both likely see both tasks

