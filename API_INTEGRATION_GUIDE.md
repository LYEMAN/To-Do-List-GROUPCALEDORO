# REST API Integration Guide for MAUI To-Do Application

## Overview
This document explains the REST API integration added to your MAUI To-Do Application. The app now communicates with a remote REST API at `https://todo-list.dcism.org` instead of using only local SQLite database.

## New Files Added

### 1. **ApiService.cs**
The main API service that handles all REST API calls. Contains:
- **Authentication Methods:**
  - `SignUpAsync()` - Create new user account
  - `SignInAsync()` - Login with email and password

- **Task Methods:**
  - `GetTasksAsync()` - Fetch tasks for a user with filtering
  - `AddTaskAsync()` - Create a new task
  - `UpdateTaskAsync()` - Edit task details
  - `ChangeTaskStatusAsync()` - Mark task as active/inactive
  - `DeleteTaskAsync()` - Remove a task

- **Response Models:**
  - `ApiResponse<T>` - Generic wrapper for all API responses
  - `UserSignUpResponse` - Sign up response data
  - `UserSignInResponse` - Sign in response data
  - `TaskItem` - Task data model from API

### 2. **SessionStorage.cs**
Manages user session storage using MAUI's SecureStorage. Handles:
- Saving user info after login (ID, name, email)
- Retrieving user data
- Checking if user is logged in
- Clearing session on logout
- Getting user display name

## API Endpoints Implemented

### Authentication
| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/signup_action.php` | Create new user account |
| GET | `/signin_action.php` | Login user with email/password |

### Tasks
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/getItems_action.php` | Fetch tasks with status filter |
| POST | `/addItem_action.php` | Create new task |
| PUT | `/editItem_action.php` | Update task details |
| PUT | `/statusItem_action.php` | Change task status (active/inactive) |
| DELETE | `/deleteItem_action.php` | Delete a task |

## How It Works

### Authentication Flow
1. User enters email and password on AuthPage
2. `OnLoginClicked()` calls `ApiService.SignInAsync()`
3. API validates credentials and returns user data (id, fname, lname, email)
4. `SessionStorage.SaveUserSessionAsync()` stores the user info securely
5. App navigates to MainPage

### Task Operations Flow
1. `MainPage.OnAppearing()` checks if user is logged in via `SessionStorage.GetUserIdAsync()`
2. Loads active tasks via `ApiService.GetTasksAsync(userId, "active")`
3. User can:
   - **Add Task:** Form submission → `ApiService.AddTaskAsync()` → Update local list
   - **Edit Task:** Select task, modify, click edit → `ApiService.UpdateTaskAsync()` → Update local item
   - **Mark Done:** Click mark done → `ApiService.ChangeTaskStatusAsync(id, "inactive")` → Remove from list
   - **Delete Task:** Click delete → `ApiService.DeleteTaskAsync()` → Remove from list

## Modified Files

### AuthPage.xaml.cs
- **Before:** Used local `UserDB` for authentication
- **After:** Uses `ApiService.SignInAsync()` and `ApiService.SignUpAsync()`
- Saves user session using `SessionStorage.SaveUserSessionAsync()`
- Better error handling with API response messages

### MainPage.xaml.cs
- **Before:** All operations used local `TodoItemDB`
- **After:** All operations call REST API methods
- Checks if user is logged in using `SessionStorage.GetUserIdAsync()`
- Loads tasks from API on page load
- All CRUD operations sync with remote API

## Usage Examples

### Sign In
```csharp
var apiService = new ApiService();
var response = await apiService.SignInAsync("user@email.com", "password");

if (response.Success)
{
    // Save session
    await SessionStorage.SaveUserSessionAsync(
        response.Data.Id,
        response.Data.FirstName,
        response.Data.LastName,
        response.Data.Email
    );
    // Navigate to MainPage
}
```

### Get Tasks
```csharp
var userId = await SessionStorage.GetUserIdAsync();
var response = await _apiService.GetTasksAsync(userId, "active");

if (response.Success)
{
    foreach (var task in response.Data)
    {
        // Use task data
    }
}
```

### Add Task
```csharp
var response = await _apiService.AddTaskAsync(userId, "Task Name", "Task Description");

if (response.Success)
{
    var newTask = response.Data; // Contains id from server
}
```

### Change Task Status
```csharp
var response = await _apiService.ChangeTaskStatusAsync(taskId, "inactive");

if (response.Success)
{
    // Task marked as completed
}
```

## Error Handling

All API methods return `ApiResponse<T>` with:
- `Success` (bool) - Whether operation succeeded
- `Data` (T) - Response data if successful
- `Message` (string) - Success or error message

Example:
```csharp
var response = await _apiService.SignInAsync(email, password);

if (response.Success)
{
    // Use response.Data
}
else
{
    // Display response.Message to user
    await DisplayAlert("Error", response.Message, "OK");
}
```

## Network Timeout
All HTTP requests have a 30-second timeout to prevent hanging requests.

## Session Management

### During Login
User data is stored in SecureStorage:
- `user_id`
- `first_name`
- `last_name`
- `email`

### During Navigation
`MainPage.OnAppearing()` checks:
- If user is logged in
- If not → redirects to AuthPage
- If yes → loads tasks

### On Logout
Call `SessionStorage.ClearSessionAsync()` to remove stored user data

## Security Considerations

1. **SecureStorage** - User credentials stored securely on device
2. **HTTPS** - All API calls use HTTPS
3. **No Password Storage** - Passwords are never saved locally
4. **Timeout Protection** - 30-second timeout prevents indefinite waits
5. **Error Messages** - API error messages shown to user for transparency

## Testing the Integration

1. Build and run the MAUI app
2. On AuthPage:
   - Sign up with email, first name, last name, password
   - Sign in with email and password
3. On MainPage:
   - Add tasks
   - Edit task details
   - Mark tasks as done
   - Delete tasks
4. TasksList updates in real-time from API

## Future Enhancements

- Add filter for completed tasks
- Implement pull-to-refresh
- Add offline mode with sync
- Add due dates to tasks
- Add task categories/tags
- Implement search functionality
- Add notifications for tasks

## Troubleshooting

### "Network Request Failed"
- Check internet connection
- Verify API URL is correct: `https://todo-list.dcism.org`
- Check if API server is online

### "Invalid Credentials"
- Verify email exists in system
- Check password is correct
- API returns "Account does not exist" if credentials invalid

### "Task Operations Failed"
- Ensure user is logged in
- Check if user_id is valid
- Verify task ID exists

### Session Lost
- User data automatically cleared on logout
- Stored in SecureStorage (survives app restart)
- Override `OnAppearing()` to restore session
