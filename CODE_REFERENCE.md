# Code Reference - Task Filtering Issue Analysis

## File Locations & Line Numbers

### 1. ApiService.cs - URL Construction (Lines 165-170)
**File:** `/ApiService.cs`
**Method:** `GetTasksAsync(int userId, string status = "active")`

```csharp
// Line 160-164: Documentation
/// <summary>
/// Get tasks for a user with specific status
/// GET /getItems_action.php?user_id={userId}&status={status}
/// status: 'active' or 'inactive'
/// </summary>

// Line 165-170: Method signature and URL construction
public async Task<ApiResponse<List<TaskItem>>> GetTasksAsync(int userId, string status = "active")
{
    try
    {
        var encodedStatus = Uri.EscapeDataString(status);
        var url = $"{_baseUrl}/getItems_action.php?user_id={userId}&status={encodedStatus}";
        // ✅ CORRECT: URL includes both user_id and status parameters
```

**Analysis:**
- User ID is inserted directly into URL query string
- Status is URL-encoded for safety
- Both parameters are required for filtering
- Backend should use these parameters to filter results


### 2. ApiService.cs - API Response Parsing (Lines 172-194)
**File:** `/ApiService.cs`
**Method:** `GetTasksAsync()` - Response Processing

```csharp
// Line 172: Make HTTP GET request
var response = await _httpClient.GetAsync(url);
var responseContent = await response.Content.ReadAsStringAsync();

// Line 175: Debug logging of response
System.Diagnostics.Debug.WriteLine($"GetTasksAsync Response: {responseContent}");

// Line 177: Check if request was successful
if (response.IsSuccessStatusCode)
{
    // Line 180: Parse JSON response
    using var doc = JsonDocument.Parse(responseContent);
    var root = doc.RootElement;

    var tasks = new List<TaskItem>();

    // Line 185-194: Extract tasks from response
    if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
    {
        // Convert object with numbered keys to list
        foreach (var prop in dataElement.EnumerateObject())
        {
            var task = JsonSerializer.Deserialize<TaskItem>(prop.Value.GetRawText(), _jsonOptions);
            if (task != null)
                tasks.Add(task);  // ⚠️ CRITICAL: NO FILTERING HERE - ADDS ALL TASKS
        }
    }
```

**Analysis:**
- Client receives JSON response from backend
- **No filtering is performed on response**
- All tasks from backend are added to the list
- Client assumes backend already filtered by user_id
- **If backend returns all tasks, all tasks will be displayed**


### 3. ApiService.cs - TaskItem Model (Lines 558-573)
**File:** `/ApiService.cs`
**Class:** `TaskItem`

```csharp
/// <summary>
/// Task item from API
/// </summary>
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
    public string? Status { get; set; } // 'active' or 'inactive'
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }  // ✅ PROPERTY EXISTS
    [JsonPropertyName("timemodified")]
    public DateTime? UpdatedAt { get; set; }
}
```

**Analysis:**
- ✅ `UserId` property is present and properly mapped from JSON
- Property can store the user_id from API response
- **BUT: This property is NEVER validated in the UI layer**


### 4. MainPage.xaml.cs - Task Loading (Lines 30-66)
**File:** `/MainPage.xaml.cs`
**Method:** `OnAppearing()`

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();

    try
    {
        System.Diagnostics.Debug.WriteLine("MainPage.OnAppearing called");

        // Line 39: Get current user ID from secure storage
        var userId = await SessionStorage.GetUserIdAsync();
        if (!userId.HasValue || userId <= 0)
        {
            System.Diagnostics.Debug.WriteLine("MainPage: No valid user ID found");
            return;
        }

        // Line 46: Store user ID in instance variable
        _userId = userId.Value;
        System.Diagnostics.Debug.WriteLine($"MainPage: UserId = {_userId}, loading tasks...");

        // Line 55: Load active tasks
        await LoadTasksAsync("active");

        // Line 58: Show/hide empty state
        emptyLabel.IsVisible = items.Count == 0;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"MainPage.OnAppearing Error: {ex}");
        await DisplayAlert("Error", $"Failed to load page: {ex.Message}", "OK");
    }
}
```

**Analysis:**
- User ID is retrieved from SessionStorage (secure storage)
- Verified to be > 0
- Stored in `_userId` instance variable
- Passed to `LoadTasksAsync()` for API call


### 5. MainPage.xaml.cs - LoadTasksAsync Method (Lines 71-125)
**File:** `/MainPage.xaml.cs`
**Method:** `LoadTasksAsync(string status)`

```csharp
private async Task LoadTasksAsync(string status)
{
    try
    {
        System.Diagnostics.Debug.WriteLine($"LoadTasksAsync START - Loading {status} tasks for userId {_userId}");

        // Line 77: Call API with user_id parameter
        var response = await _apiService.GetTasksAsync(_userId, status);

        System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - API Response: Success={response?.Success}, Data Count={response?.Data?.Count}, Message={response?.Message}");

        // Line 81: Clear existing items
        items.Clear();

        if (response != null && response.Success && response.Data != null)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - Processing {response.Data.Count} tasks from API");

            // Line 88-101: Process each task from API
            foreach (var apiTask in response.Data)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - Adding task: Id={apiTask.Id}, Name={apiTask.ItemName}, Status={apiTask.Status}");

                // ⚠️ CRITICAL: NO VALIDATION THAT apiTask.UserId == _userId
                var toDoItem = new ToDoItem
                {
                    ID = apiTask.Id,
                    Name = apiTask.ItemName,
                    Notes = apiTask.ItemDescription,
                    Done = apiTask.Status == "inactive"
                    // ❌ MISSING: if (apiTask.UserId != _userId) continue;
                };
                items.Add(toDoItem);  // Task added regardless of user_id
            }

            System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - Successfully added {items.Count} items to collection");
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"LoadTasksAsync ERROR: {ex.Message}");
    }
}
```

**Critical Issues:**
1. **Line 77:** User ID is properly sent to API ✅
2. **Lines 88-101:** All tasks from API are added WITHOUT validation ❌
3. **Missing check:** `if (apiTask.UserId != _userId) continue;` ❌


### 6. FinishedPage.xaml.cs - Similar Pattern (Lines 63-106)
**File:** `/FinishedPage.xaml.cs`
**Method:** `LoadTasksAsync(string status)`

```csharp
private async Task LoadTasksAsync(string status)
{
    try
    {
        // Line 67: Call API with user_id parameter
        var response = await _apiService.GetTasksAsync(_userId, status);

        items.Clear();

        if (response != null && response.Success && response.Data != null)
        {
            // Line 77-89: Process tasks WITHOUT validation
            foreach (var apiTask in response.Data)
            {
                System.Diagnostics.Debug.WriteLine($"Task: Id={apiTask.Id}, Name={apiTask.ItemName}, Status={apiTask.Status}");

                var toDoItem = new ToDoItem
                {
                    ID = apiTask.Id,
                    Name = apiTask.ItemName,
                    Notes = apiTask.ItemDescription,
                    Done = true
                    // ❌ MISSING: if (apiTask.UserId != _userId) continue;
                };
                items.Add(toDoItem);
            }
        }
    }
}
```

**Same Issue:** No validation of UserId ❌


### 7. SessionStorage.cs - User ID Retrieval (Lines 44-57)
**File:** `/SessionStorage.cs`
**Method:** `GetUserIdAsync()`

```csharp
/// <summary>
/// Get stored user ID
/// Returns null if not logged in
/// </summary>
public static async Task<int?> GetUserIdAsync()
{
    try
    {
        var value = await SafeGetAsync(UserIdKey);
        if (int.TryParse(value, out var userId))
            return userId;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error retrieving user ID: {ex.Message}");
    }
    return null;
}
```

**Analysis:**
- User ID is retrieved from device secure storage
- Converted to int and returned
- Safe fallback if conversion fails
- Properly used in MainPage and FinishedPage


---

## Data Flow Diagram

```
┌─────────────────────────────────┐
│  MainPage.OnAppearing()          │
│  Line 39-46                       │
│  Gets _userId from SessionStorage │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  LoadTasksAsync(status)          │
│  Line 71-125                      │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  ApiService.GetTasksAsync(       │
│    userId, status)              │
│  Line 165-223                     │
│                                  │
│  URL: /getItems_action.php       │
│  ?user_id={userId}              │
│  &status={status}               │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  HTTPS Request to Backend        │
│  https://todo-list.dcism.org    │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  Backend (getItems_action.php)  │
│  SHOULD filter by user_id       │
│  ⚠️ CURRENTLY RETURNS ALL TASKS  │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  JSON Response (all tasks)       │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  ApiService parses response      │
│  Line 185-194                     │
│  ❌ NO FILTERING - adds all tasks │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  MainPage.LoadTasksAsync()       │
│  Line 88-101                      │
│  ❌ NO VALIDATION - adds all tasks│
│                                  │
│  Result: USER SEES ALL TASKS!   │
└─────────────────────────────────┘
```

---

## Summary of Code Issues

| Location | Issue | Line(s) | Severity |
|----------|-------|---------|----------|
| ApiService.cs | No client-side filtering in response parsing | 185-194 | Medium |
| MainPage.xaml.cs | No UserId validation when adding tasks | 88-101 | Medium |
| FinishedPage.xaml.cs | No UserId validation when adding tasks | 77-89 | Medium |
| getItems_action.php (Backend) | Not using user_id parameter in query | N/A | **CRITICAL** |

---

## Quick Fix Code Snippet

Add this validation in both MainPage.xaml.cs and FinishedPage.xaml.cs:

```csharp
foreach (var apiTask in response.Data)
{
    // ADD THIS VALIDATION
    if (apiTask.UserId != _userId)
    {
        System.Diagnostics.Debug.WriteLine(
            $"WARNING: Skipping task {apiTask.Id} - user_id mismatch. " +
            $"Task UserId={apiTask.UserId}, Current UserId={_userId}"
        );
        continue;  // Skip this task
    }

    // Original code continues here
    var toDoItem = new ToDoItem
    {
        ID = apiTask.Id,
        Name = apiTask.ItemName,
        Notes = apiTask.ItemDescription,
        Done = apiTask.Status == "inactive"
    };
    items.Add(toDoItem);
}
```

