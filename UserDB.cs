using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace MauiApp2
{
    class UserDB
    {
        SQLiteAsyncConnection database;

        async Task Init()
        {
            if (database is not null)
                return;

            database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
            await database.CreateTableAsync<User>();
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            await Init();
            return await database.Table<User>().Where(u => u.Username == username).FirstOrDefaultAsync();
        }

        public async Task<int> CreateUserAsync(string username, string password)
        {
            await Init();
            var existing = await GetUserByUsernameAsync(username);
            if (existing != null)
                return 0;

            var user = new User
            {
                Username = username,
                PasswordHash = ComputeHash(password)
            };

            return await database.InsertAsync(user);
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            await Init();
            var user = await GetUserByUsernameAsync(username);
            if (user == null)
                return false;
            var hash = ComputeHash(password);
            return string.Equals(user.PasswordHash, hash, StringComparison.Ordinal);
        }

        static string ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
