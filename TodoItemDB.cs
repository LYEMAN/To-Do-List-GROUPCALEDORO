using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace MauiApp2
{
    class TodoItemDB
    {
		SQLiteAsyncConnection database;

		async Task Init()
		{
			if (database is not null)
				return;

			database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
			var result = await database.CreateTableAsync<ToDoItem>();
		}

		public async Task<List<ToDoItem>> GetItemsAsync()
		{
			await Init();
			return await database.Table<ToDoItem>().ToListAsync();
		}

		public async Task<List<ToDoItem>> GetItemsNotDoneAsync()
		{
			await Init();
			return await database.Table<ToDoItem>().Where(t => !t.Done).ToListAsync();
		}

		public async Task<List<ToDoItem>> GetItemsDoneAsync()
		{
			await Init();
			return await database.Table<ToDoItem>().Where(t => t.Done).ToListAsync();
		}

		public async Task<ToDoItem> GetItemAsync(int id)
		{
			await Init();
			return await database.Table<ToDoItem>().Where(i => i.ID == id).FirstOrDefaultAsync();
		}

		public async Task<int> SaveItemAsync(ToDoItem item)
		{
			await Init();
			if (item.ID != 0)
				return await database.UpdateAsync(item);
			else
				return await database.InsertAsync(item);
		}

		public async Task<int> DeleteItemAsync(ToDoItem item)
		{
			await Init();
			return await database.DeleteAsync(item);
		}
    
    }
}
