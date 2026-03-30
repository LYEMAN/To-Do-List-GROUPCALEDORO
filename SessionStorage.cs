using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                await SecureStorage.SetAsync(UserIdKey, userId.ToString());
                await SecureStorage.SetAsync(FirstNameKey, firstName);
                await SecureStorage.SetAsync(LastNameKey, lastName);
                await SecureStorage.SetAsync(EmailKey, email);
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
                var value = await SecureStorage.GetAsync(UserIdKey);
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
                return await SecureStorage.GetAsync(FirstNameKey);
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
                return await SecureStorage.GetAsync(LastNameKey);
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
                return await SecureStorage.GetAsync(EmailKey);
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
                SecureStorage.Remove(UserIdKey);
                SecureStorage.Remove(FirstNameKey);
                SecureStorage.Remove(LastNameKey);
                SecureStorage.Remove(EmailKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing session: {ex.Message}");
            }
        }
    }
}
