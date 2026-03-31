using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Maui.Storage;

namespace MauiApp2
{
    /// <summary>
    /// Manages user session data using MAUI SecureStorage
    /// Stores: user_id, first_name, last_name, email
    /// </summary>
    public static class SessionStorage
    {
        private const string UserIdKey = "user_id";
        private const string FirstNameKey = "first_name";
        private const string LastNameKey = "last_name";
        private const string EmailKey = "email";

        /// <summary>
        /// Save user session after successful login
        /// </summary>
        public static async Task SaveUserSessionAsync(int userId, string firstName, string lastName, string email)
        {
            try
            {
                await SafeSetAsync(UserIdKey, userId.ToString());
                await SafeSetAsync(FirstNameKey, firstName);
                await SafeSetAsync(LastNameKey, lastName);
                await SafeSetAsync(EmailKey, email);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving session: {ex.Message}");
            }
        }

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

        /// <summary>
        /// Get stored user first name
        /// </summary>
        public static async Task<string?> GetFirstNameAsync()
        {
            try
            {
                return await SafeGetAsync(FirstNameKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving first name: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Get stored user last name
        /// </summary>
        public static async Task<string?> GetLastNameAsync()
        {
            try
            {
                return await SafeGetAsync(LastNameKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving last name: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Get stored user email
        /// </summary>
        public static async Task<string?> GetEmailAsync()
        {
            try
            {
                return await SafeGetAsync(EmailKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving email: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Check if user is logged in
        /// </summary>
        public static async Task<bool> IsUserLoggedInAsync()
        {
            var userId = await GetUserIdAsync();
            return userId.HasValue && userId > 0;
        }

        /// <summary>
        /// Get user display name (First Last)
        /// </summary>
        public static async Task<string> GetUserDisplayNameAsync()
        {
            var firstName = await GetFirstNameAsync() ?? "User";
            var lastName = await GetLastNameAsync() ?? "";
            return $"{firstName} {lastName}".Trim();
        }

        /// <summary>
        /// Clear user session (logout)
        /// </summary>
        public static async Task ClearSessionAsync()
        {
            try
            {
                await SafeRemoveAsync(UserIdKey);
                await SafeRemoveAsync(FirstNameKey);
                await SafeRemoveAsync(LastNameKey);
                await SafeRemoveAsync(EmailKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing session: {ex.Message}");
            }
        }

        // Safe helpers: attempt SecureStorage first, fall back to Preferences for platforms
        // where SecureStorage is not available (e.g., un-packaged Windows desktop during development).
        private static async Task SafeSetAsync(string key, string value)
        {
            try
            {
                await SecureStorage.SetAsync(key, value ?? string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SecureStorage.SetAsync failed for '{key}', falling back to Preferences: {ex.Message}");
                try
                {
                    Preferences.Set(key, value ?? string.Empty);
                }
                catch (Exception prefEx)
                {
                    Debug.WriteLine($"Preferences.Set failed for '{key}': {prefEx.Message}");
                }
            }
        }

        private static async Task<string?> SafeGetAsync(string key)
        {
            try
            {
                var val = await SecureStorage.GetAsync(key);
                if (!string.IsNullOrEmpty(val))
                    return val;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SecureStorage.GetAsync failed for '{key}', falling back to Preferences: {ex.Message}");
            }

            try
            {
                if (Preferences.ContainsKey(key))
                    return Preferences.Get(key, null);
            }
            catch (Exception prefEx)
            {
                Debug.WriteLine($"Preferences.Get failed for '{key}': {prefEx.Message}");
            }

            return null;
        }

        private static void SafeRemove(string key)
        {
            try
            {
                SecureStorage.Remove(key);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SecureStorage.Remove failed for '{key}', attempting Preferences.Remove: {ex.Message}");
                try
                {
                    if (Preferences.ContainsKey(key))
                        Preferences.Remove(key);
                }
                catch (Exception prefEx)
                {
                    Debug.WriteLine($"Preferences.Remove failed for '{key}': {prefEx.Message}");
                }
            }
        }

        private static Task SafeRemoveAsync(string key)
        {
            SafeRemove(key);
            return Task.CompletedTask;
        }
    }
}
