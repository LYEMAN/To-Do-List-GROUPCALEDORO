using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                    var result = JsonSerializer.Deserialize<UserSignUpResponse>(responseContent, _jsonOptions);

                    if (result == null || result.Id <= 0)
                    {
                        return new ApiResponse<UserSignUpResponse>
                        {
                            Success = false,
                            Message = ExtractErrorMessage(responseContent) ?? "Sign up failed"
                        };
                    }

                    return new ApiResponse<UserSignUpResponse>
                    {
                        Success = true,
                        Data = result,
                        Message = "Account created successfully"
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

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<UserSignInResponse>(responseContent, _jsonOptions);

                    if (result == null || result.Id <= 0)
                    {
                        return new ApiResponse<UserSignInResponse>
                        {
                            Success = false,
                            Message = ExtractErrorMessage(responseContent) ?? "Account does not exist"
                        };
                    }

                    return new ApiResponse<UserSignInResponse>
                    {
                        Success = true,
                        Data = result,
                        Message = "Sign in successful"
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

            System.Diagnostics.Debug.WriteLine($"GetTasksAsync Response: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                // Parse the response which has data as an object with numbered keys
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                var tasks = new List<TaskItem>();

                if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
                {
                    // Convert object with numbered keys to list
                    foreach (var prop in dataElement.EnumerateObject())
                    {
                        var task = JsonSerializer.Deserialize<TaskItem>(prop.Value.GetRawText(), _jsonOptions);
                        if (task != null && task.UserId == userId)
                            tasks.Add(task);
                    }
                }

                return new ApiResponse<List<TaskItem>>
                {
                    Success = true,
                    Data = tasks,
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
            System.Diagnostics.Debug.WriteLine($"GetTasksAsync Error: {ex}");
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

                System.Diagnostics.Debug.WriteLine($"AddTaskAsync Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"AddTaskAsync Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    // Parse response which contains data inside a "data" property
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;

                    TaskItem? result = null;
                    if (root.TryGetProperty("data", out var dataElement))
                    {
                        result = JsonSerializer.Deserialize<TaskItem>(dataElement.GetRawText(), _jsonOptions);
                    }

                    System.Diagnostics.Debug.WriteLine($"Deserialized TaskItem: Id={result?.Id}, ItemName={result?.ItemName}, Status={result?.Status}");

                    return new ApiResponse<TaskItem>
                    {
                        Success = true,
                        Data = result,
                        Message = "Task added successfully"
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
                System.Diagnostics.Debug.WriteLine($"AddTaskAsync Exception: {ex}");
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
        public async Task<ApiResponse<bool>> UpdateTaskAsync(int itemId, string itemName, string itemDescription)
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

                System.Diagnostics.Debug.WriteLine($"UpdateTaskAsync Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Task updated successfully"
                    };
                }
                else
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = ExtractErrorMessage(responseContent) ?? "Failed to update task"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateTaskAsync Exception: {ex}");
                return new ApiResponse<bool>
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
        public async Task<ApiResponse<bool>> ChangeTaskStatusAsync(int itemId, string status)
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

                System.Diagnostics.Debug.WriteLine($"ChangeTaskStatusAsync Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = $"Task marked as {status}"
                    };
                }
                else
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = ExtractErrorMessage(responseContent) ?? "Failed to change task status"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangeTaskStatusAsync Exception: {ex}");
                return new ApiResponse<bool>
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
                var responseContent = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"DeleteTaskAsync Response: {responseContent}");

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
                        Message = ExtractErrorMessage(responseContent) ?? "Failed to delete task"
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
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    /// <summary>
    /// Sign in response from API
    /// </summary>
    public class UserSignInResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("fname")]
        public string? FirstName { get; set; }
        [JsonPropertyName("lname")]
        public string? LastName { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    /// <summary>
    /// Custom JSON converter to handle string values that might be numbers or other types
    /// </summary>
    public class FlexibleStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out var intValue))
                        return intValue.ToString();
                    if (reader.TryGetInt64(out var longValue))
                        return longValue.ToString();
                    return reader.GetDouble().ToString();
                case JsonTokenType.True:
                    return "true";
                case JsonTokenType.False:
                    return "false";
                case JsonTokenType.Null:
                    return null;
                default:
                    return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value);
        }
    }

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
        public int UserId { get; set; }
        [JsonPropertyName("timemodified")]
        public DateTime? UpdatedAt { get; set; }
    }
}
