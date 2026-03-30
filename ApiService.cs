using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MauiApp2
{
    /// <summary>
    /// API Service for communicating with the To-Do REST API
    /// Base URL: https://todo-list.dcism.org
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://todo-list.dcism.org";

        // JSON serializer options for consistency
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiService()
        {
            // Initialize HttpClient with timeout and default headers
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        // ============================================
        // AUTHENTICATION ENDPOINTS
        // ============================================

        /// <summary>
        /// Sign up a new user
        /// POST /signup_action.php
        /// </summary>
        public async Task<ApiResponse<UserSignUpResponse>> SignUpAsync(string firstName, string lastName, string email, string password, string confirmPassword)
        {
            try
            {
                var requestBody = new
                {
                    first_name = firstName,
                    last_name = lastName,
                    email = email,
                    password = password,
                    confirm_password = confirmPassword
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/signup_action.php", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Log raw response for diagnostics
                    System.Diagnostics.Debug.WriteLine($"[ApiService] SignUp response: {responseContent}");

                    // Try to deserialize into expected shape, but be resilient to different JSON structures
                    var result = JsonSerializer.Deserialize<UserSignUpResponse>(responseContent, _jsonOptions);
                    if (result == null || result.Id <= 0)
                    {
                        // Try to extract from wrappers like { "data": { ... } } or arrays
                        try
                        {
                            using var doc = JsonDocument.Parse(responseContent);
                            var root = doc.RootElement;

                            JsonElement candidate = root;
                            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataProp))
                                candidate = dataProp;

                            if (candidate.ValueKind == JsonValueKind.Object && candidate.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out var parsedId))
                            {
                                result ??= new UserSignUpResponse();
                                result.Id = parsedId;
                                if (candidate.TryGetProperty("first_name", out var fn)) result.FirstName = fn.GetString();
                                if (candidate.TryGetProperty("last_name", out var ln)) result.LastName = ln.GetString();
                                if (candidate.TryGetProperty("email", out var em)) result.Email = em.GetString();
                            }
                        }
                        catch { /* ignore parse errors */ }
                    }

                    return new ApiResponse<UserSignUpResponse>
                    {
                        Success = result != null && result.Id > 0,
                        Data = result,
                        Message = result != null && result.Id > 0 ? "Account created successfully" : ExtractErrorMessage(responseContent) ?? "Sign up succeeded but server returned unexpected data"
                    };
                }
                else
                {
                    return new ApiResponse<UserSignUpResponse>
                    {
                        Success = false,
                        Message = ExtractErrorMessage(responseContent) ?? "Sign up failed"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserSignUpResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Sign in with email and password
        /// GET /signin_action.php?email={email}&password={password}
        /// </summary>
        public async Task<ApiResponse<UserSignInResponse>> SignInAsync(string email, string password)
        {
            try
            {
                var encodedEmail = Uri.EscapeDataString(email);
                var encodedPassword = Uri.EscapeDataString(password);
                var url = $"{_baseUrl}/signin_action.php?email={encodedEmail}&password={encodedPassword}";

                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log raw response for debugging
                System.Diagnostics.Debug.WriteLine($"[ApiService] SignIn response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    // Try to deserialize normally
                    var result = JsonSerializer.Deserialize<UserSignInResponse>(responseContent, _jsonOptions);

                    // If deserialization didn't yield an id, try common wrapper shapes
                    if ((result == null) || result.Id <= 0)
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(responseContent);
                            var root = doc.RootElement;

                            JsonElement candidate = root;
                            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataProp))
                                candidate = dataProp;

                            if (candidate.ValueKind == JsonValueKind.Array && candidate.GetArrayLength() > 0)
                                candidate = candidate[0];

                            if (candidate.ValueKind == JsonValueKind.Object && candidate.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out var parsedId))
                            {
                                result ??= new UserSignInResponse();
                                result.Id = parsedId;
                                if (candidate.TryGetProperty("first_name", out var fn)) result.FirstName = fn.GetString();
                                if (candidate.TryGetProperty("last_name", out var ln)) result.LastName = ln.GetString();
                                if (candidate.TryGetProperty("email", out var em)) result.Email = em.GetString();
                            }
                        }
                        catch { /* ignore parse errors */ }
                    }

                    return new ApiResponse<UserSignInResponse>
                    {
                        Success = result != null && result.Id > 0,
                        Data = result,
                        Message = result != null && result.Id > 0 ? "Sign in successful" : "Account does not exist"
                    };
                }
                else
                {
                    return new ApiResponse<UserSignInResponse>
                    {
                        Success = false,
                        Message = "Account does not exist"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserSignInResponse>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        // ============================================
        // TASK ENDPOINTS
        // ============================================

        /// <summary>
        /// Get tasks for a user with specific status
        /// GET /getItems_action.php?user_id={userId}&status={status}
        /// status: 'active' or 'inactive'
        /// </summary>
        public async Task<ApiResponse<List<TaskItem>>> GetTasksAsync(int userId, string status = "active")
        {
            try
            {
                var encodedStatus = Uri.EscapeDataString(status);
                var url = $"{_baseUrl}/getItems_action.php?user_id={userId}&status={encodedStatus}";

                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log raw response for diagnostics
                System.Diagnostics.Debug.WriteLine($"[ApiService] GetTasks response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    List<TaskItem>? result = null;
                    try
                    {
                        result = JsonSerializer.Deserialize<List<TaskItem>>(responseContent, _jsonOptions);
                    }
                    catch { result = null; }

                    if (result == null || result.Count == 0)
                    {
                        // Try to extract wrapped data: { "data": [...] } or similar
                        try
                        {
                            using var doc = JsonDocument.Parse(responseContent);
                            var root = doc.RootElement;

                            JsonElement candidate = root;
                            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataProp))
                                candidate = dataProp;

                            if (candidate.ValueKind == JsonValueKind.Array)
                            {
                                result = new List<TaskItem>();
                                foreach (var el in candidate.EnumerateArray())
                                {
                                    try
                                    {
                                        var itemJson = el.GetRawText();
                                        var item = JsonSerializer.Deserialize<TaskItem>(itemJson, _jsonOptions);
                                        if (item != null)
                                            result.Add(item);
                                    }
                                    catch { /* ignore individual parse errors */ }
                                }
                            }
                        }
                        catch { /* ignore parse errors */ }
                    }

                    return new ApiResponse<List<TaskItem>>
                    {
                        Success = true,
                        Data = result ?? new List<TaskItem>(),
                        Message = "Tasks retrieved successfully"
                    };
                }
                else
                {
                    return new ApiResponse<List<TaskItem>>
                    {
                        Success = false,
                        Data = new List<TaskItem>(),
                        Message = "Failed to retrieve tasks"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<TaskItem>>
                {
                    Success = false,
                    Data = new List<TaskItem>(),
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Add a new task for a user
        /// POST /addItem_action.php
        /// </summary>
        public async Task<ApiResponse<TaskItem>> AddTaskAsync(int userId, string itemName, string itemDescription)
        {
            try
            {
                var requestBody = new
                {
                    item_name = itemName,
                    item_description = itemDescription,
                    user_id = userId
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/addItem_action.php", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log raw response for diagnostics
                System.Diagnostics.Debug.WriteLine($"[ApiService] AddTask response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    // Try to deserialize directly
                    var result = JsonSerializer.Deserialize<TaskItem>(responseContent, _jsonOptions);

                    // If result is null or missing id, attempt to find a wrapped object
                    if (result == null || result.Id <= 0)
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(responseContent);
                            var root = doc.RootElement;

                            JsonElement candidate = root;
                            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataProp))
                                candidate = dataProp;

                            if (candidate.ValueKind == JsonValueKind.Array && candidate.GetArrayLength() > 0)
                                candidate = candidate[0];

                            if (candidate.ValueKind == JsonValueKind.Object)
                            {
                                // Map fields if present
                                result ??= new TaskItem();
                                if (candidate.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out var parsedId)) result.Id = parsedId;
                                if (candidate.TryGetProperty("item_name", out var nameProp)) result.ItemName = nameProp.GetString();
                                if (candidate.TryGetProperty("item_description", out var descProp)) result.ItemDescription = descProp.GetString();
                                if (candidate.TryGetProperty("status", out var statusProp)) result.Status = statusProp;
                                if (candidate.TryGetProperty("user_id", out var uidProp) && uidProp.TryGetInt32(out var parsedUid)) result.UserId = parsedUid;
                            }
                        }
                        catch { /* ignore parse errors */ }
                    }

                    return new ApiResponse<TaskItem>
                    {
                        Success = result != null && result.Id > 0,
                        Data = result,
                        Message = result != null && result.Id > 0 ? "Task added successfully" : ExtractErrorMessage(responseContent) ?? "Task added but server returned unexpected data"
                    };
                }
                else
                {
                    return new ApiResponse<TaskItem>
                    {
                        Success = false,
                        Message = ExtractErrorMessage(responseContent) ?? "Failed to add task"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<TaskItem>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Update task details (name and description)
        /// PUT /editItem_action.php
        /// </summary>
        public async Task<ApiResponse<TaskItem>> UpdateTaskAsync(int itemId, string itemName, string itemDescription)
        {
            try
            {
                var requestBody = new
                {
                    item_id = itemId,
                    item_name = itemName,
                    item_description = itemDescription
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/editItem_action.php")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TaskItem>(responseContent, _jsonOptions);
                    return new ApiResponse<TaskItem>
                    {
                        Success = true,
                        Data = result,
                        Message = "Task updated successfully"
                    };
                }
                else
                {
                    return new ApiResponse<TaskItem>
                    {
                        Success = false,
                        Message = ExtractErrorMessage(responseContent) ?? "Failed to update task"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<TaskItem>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Change task status between active and inactive
        /// PUT /statusItem_action.php
        /// status: 'active' or 'inactive'
        /// </summary>
        public async Task<ApiResponse<TaskItem>> ChangeTaskStatusAsync(int itemId, string status)
        {
            try
            {
                var requestBody = new
                {
                    item_id = itemId,
                    status = status
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/statusItem_action.php")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TaskItem>(responseContent, _jsonOptions);
                    return new ApiResponse<TaskItem>
                    {
                        Success = true,
                        Data = result,
                        Message = $"Task marked as {status}"
                    };
                }
                else
                {
                    return new ApiResponse<TaskItem>
                    {
                        Success = false,
                        Message = ExtractErrorMessage(responseContent) ?? "Failed to change task status"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<TaskItem>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Delete a task
        /// DELETE /deleteItem_action.php?item_id={itemId}
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteTaskAsync(int itemId)
        {
            try
            {
                var url = $"{_baseUrl}/deleteItem_action.php?item_id={itemId}";

                var request = new HttpRequestMessage(HttpMethod.Delete, url);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Task deleted successfully"
                    };
                }
                else
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to delete task"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        // ============================================
        // HELPER METHODS
        // ============================================

        /// <summary>
        /// Extract error message from JSON response
        /// </summary>
        private string? ExtractErrorMessage(string jsonContent)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                if (doc.RootElement.TryGetProperty("message", out var messageProp))
                {
                    return messageProp.GetString();
                }
                if (doc.RootElement.TryGetProperty("error", out var errorProp))
                {
                    return errorProp.GetString();
                }
            }
            catch { /* Ignore parsing errors */ }
            return null;
        }
    }

    // ============================================
    // RESPONSE & MODEL CLASSES
    // ============================================

    /// <summary>
    /// Generic API response wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sign up response from API
    /// </summary>
    public class UserSignUpResponse
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// Sign in response from API
    /// </summary>
    public class UserSignInResponse
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// Task item from API
    /// </summary>
    public class TaskItem
    {
        public int Id { get; set; }
        public string? ItemName { get; set; }
        public string? ItemDescription { get; set; }
        // Status can be returned by the API as a string, number or boolean depending on server implementation.
        // Use JsonElement to accept any JSON token and interpret it when mapping to UI models.
        public System.Text.Json.JsonElement Status { get; set; }
        public int UserId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
