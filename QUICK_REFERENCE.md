# API Integration Quick Reference

## Files Added/Modified

### ✅ NEW FILES
1. **ApiService.cs** - REST API client
   - 200+ lines of well-commented code
   - Handles all 7 API endpoints
   - Includes response models and error handling

2. **SessionStorage.cs** - Session management
   - Uses MAUI SecureStorage for safe user data storage
   - Methods to save/retrieve/clear user session

3. **API_INTEGRATION_GUIDE.md** - Comprehensive documentation

### 🔄 MODIFIED FILES
1. **AuthPage.xaml.cs**
   - Replaced `UserDB` with `ApiService`
   - Added session storage on successful login
   - Improved error messages

2. **MainPage.xaml.cs**
   - Replaced `TodoItemDB` with `ApiService`
   - Added session check on page load
   - All CRUD operations now use REST API

## Quick Start

### 1. Install NuGet Package (if needed to update)
For newest HTTP handling in MAUI projects.

### 2. Create Instance
```csharp
var apiService = new ApiService();
```

### 3. Call Methods
```csharp
// Sign In
var response = await apiService.SignInAsync(email, password);

// Get Tasks
var tasks = await apiService.GetTasksAsync(userId, "active");

// Add Task
var newTask = await apiService.AddTaskAsync(userId, name, description);

// Update Task
var updated = await apiService.UpdateTaskAsync(taskId, name, description);

// Change Status
var changed = await apiService.ChangeTaskStatusAsync(taskId, "inactive");

// Delete Task
var deleted = await apiService.DeleteTaskAsync(taskId);
```

## API Response Structure

All methods return `ApiResponse<T>`:
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }          // true if successful
    public T? Data { get; set; }               // response data
    public string Message { get; set; }        // success/error message
}
```

## Constants & Configuration

Base URL: `https://todo-list.dcism.org`
Timeout: 30 seconds (in ApiService constructor)

To change, edit ApiService.cs:
```csharp
private readonly string _baseUrl = "https://todo-list.dcism.org";
```

## Session Management Cheat Sheet

```csharp
// Save after login
await SessionStorage.SaveUserSessionAsync(id, firstName, lastName, email);

// Check if logged in
bool isLoggedIn = await SessionStorage.IsUserLoggedInAsync();

// Get user info
int? userId = await SessionStorage.GetUserIdAsync();
string? name = await SessionStorage.GetUserDisplayNameAsync();

// Clear on logout
await SessionStorage.ClearSessionAsync();
```

## Error Handling Pattern

```csharp
try
{
    var response = await _apiService.OperationAsync();

    if (response.Success)
    {
        // Use response.Data
    }
    else
    {
        // Show response.Message
        await DisplayAlert("Error", response.Message, "OK");
    }
}
catch (Exception ex)
{
    await DisplayAlert("Error", $"Exception: {ex.Message}", "OK");
}
```

## API Endpoint Reference

| Operation | Method | Endpoint | Params |
|-----------|--------|----------|--------|
| Sign Up | POST | `/signup_action.php` | first_name, last_name, email, password, confirm_password |
| Sign In | GET | `/signin_action.php` | email, password |
| Get Tasks | GET | `/getItems_action.php` | user_id, status |
| Add Task | POST | `/addItem_action.php` | item_name, item_description, user_id |
| Update Task | PUT | `/editItem_action.php` | item_id, item_name, item_description |
| Change Status | PUT | `/statusItem_action.php` | item_id, status |
| Delete Task | DELETE | `/deleteItem_action.php` | item_id |

## Status Values

Tasks have status: `"active"` or `"inactive"`
- Mapping: `status == "inactive"` → `Done = true`
- Alternative: `status == "active"` → `Done = false`

## Important Notes

🔒 **Security**
- Passwords are NOT saved locally
- User data stored in SecureStorage (encrypted)
- All requests use HTTPS

⏱️ **Timeout**
- 30 seconds for all HTTP requests
- Prevents app from hanging on slow network

🔄 **Offline**
- App requires internet connection
- No offline mode currently implemented

✨ **Future Improvements**
- Add offline queue for requests
- Implement caching
- Add retry logic for failed requests
- Add progress indicators
- Auto-refresh on page focus

## Testing Checklist

- [ ] Sign up with new account
- [ ] Sign in with existing account
- [ ] Add a new task
- [ ] Edit task name/description
- [ ] Mark task as done
- [ ] Delete a task
- [ ] Verify tasks load on MainPage
- [ ] Test with slow network (App timeout works)
- [ ] Logout and verify redirect to AuthPage
- [ ] Session persists after app restart

## Debugging Tips

1. **Check HttpClient Logs**
   ```csharp
   System.Diagnostics.Debug.WriteLine($"API Response: {responseContent}");
   ```

2. **Verify Network**
   - Check device has internet
   - Test API URL in browser

3. **Check Response Format**
   - API might return different JSON structure
   - May need to update response models

4. **Session Issues**
   - Manually clear SecureStorage in device settings
   - Reinstall app to reset all data

## Integration Points in Code

### AuthPage.xaml.cs
- Line 7: `private readonly ApiService _apiService = new();`
- Line 22: `await _apiService.SignInAsync(...)`
- Line 27: `await SessionStorage.SaveUserSessionAsync(...)`

### MainPage.xaml.cs
- Line 18: `private readonly ApiService _apiService = new();`
- Line 39: `var response = await _apiService.GetTasksAsync(...)`
- Line 59: `await _apiService.AddTaskAsync(...)`
- Line 92: `await _apiService.UpdateTaskAsync(...)`
- Line 122: `await _apiService.DeleteTaskAsync(...)`
- Line 150: `await _apiService.ChangeTaskStatusAsync(...)`

## Support & Debugging

For API errors, check:
1. API response message (included in ApiResponse)
2. Network connectivity
3. User credentials
4. Task/User IDs validity
5. Request timeout (30 seconds)

All exceptions are caught and returned in `ApiResponse.Message`
