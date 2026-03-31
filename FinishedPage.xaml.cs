namespace MauiApp2;

using System;
using System.Collections.ObjectModel;
using System.Linq;

public partial class FinishedPage : ContentPage
{
    private ObservableCollection<ToDoItem> items = new();
    private readonly ApiService _apiService = new();
    private int _userId = 0;

    public FinishedPage()
    {
        InitializeComponent();
        finishedLV.ItemsSource = items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            System.Diagnostics.Debug.WriteLine("FinishedPage.OnAppearing called");

            // Get current user ID (already validated by AppShell)
            var userId = await SessionStorage.GetUserIdAsync();
            if (!userId.HasValue || userId <= 0)
            {
                System.Diagnostics.Debug.WriteLine("FinishedPage: No valid user ID found");
                return;
            }

            _userId = userId.Value;

            System.Diagnostics.Debug.WriteLine($"FinishedPage: UserId = {_userId}, loading incomplete tasks...");

            // Load completed tasks from API
            await LoadTasksAsync("inactive");

            System.Diagnostics.Debug.WriteLine($"FinishedPage: Loaded {items.Count} completed tasks");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FinishedPage.OnAppearing Error: {ex}");
            await DisplayAlert("Error", $"Failed to load page: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Manual refresh button
    /// </summary>
    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Refresh button clicked");
        await LoadTasksAsync("inactive");
    }

    /// <summary>
    /// Load completed tasks from REST API
    /// </summary>
    private async Task LoadTasksAsync(string status)
    {
        try
        {
            var response = await _apiService.GetTasksAsync(_userId, status);

            System.Diagnostics.Debug.WriteLine($"FinishedPage.LoadTasksAsync - Status: {status}, Response Success: {response?.Success}, Data Count: {response?.Data?.Count}");

            items.Clear();

            if (response != null && response.Success && response.Data != null)
            {
                System.Diagnostics.Debug.WriteLine($"Loading {response.Data.Count} completed tasks");

                foreach (var apiTask in response.Data)
                {
                    System.Diagnostics.Debug.WriteLine($"Task: Id={apiTask.Id}, Name={apiTask.ItemName}, Status={apiTask.Status}");

                    var toDoItem = new ToDoItem
                    {
                        ID = apiTask.Id,
                        Name = apiTask.ItemName,
                        Notes = apiTask.ItemDescription,
                        Done = true
                    };
                    items.Add(toDoItem);
                }
            }
            else if (response != null)
            {
                System.Diagnostics.Debug.WriteLine($"API Response Error: {response.Message}");
            }

            System.Diagnostics.Debug.WriteLine($"Final items count: {items.Count}");
            emptyLabel.IsVisible = items.Count == 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTasksAsync Error: {ex}");
            items.Clear();
            emptyLabel.IsVisible = true;
            await DisplayAlert("Error", $"Failed to load completed tasks: {ex.Message}", "OK");
        }
    }

    private async void DeleteToDoItem(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var item = items.FirstOrDefault(x => x.ID == id);
            if (item != null)
            {
                try
                {
                    var response = await _apiService.DeleteTaskAsync(id);

                    if (response.Success)
                    {
                        items.Remove(item);
                        emptyLabel.IsVisible = items.Count == 0;
                        await DisplayAlert("Success", "Task deleted successfully", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", response.Message ?? "Failed to delete task", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error deleting task: {ex.Message}", "OK");
                }
            }
        }
    }

    /// <summary>
    /// Undo completed task - mark it as active again
    /// </summary>
    private async void UndoToDoItem(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var item = items.FirstOrDefault(x => x.ID == id);
            if (item != null)
            {
                try
                {
                    var response = await _apiService.ChangeTaskStatusAsync(id, "active");

                    if (response.Success)
                    {
                        items.Remove(item);
                        emptyLabel.IsVisible = items.Count == 0;
                        await DisplayAlert("Success", "Task restored to active", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", response.Message ?? "Failed to restore task", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error restoring task: {ex.Message}", "OK");
                }
            }
        }
    }
}
