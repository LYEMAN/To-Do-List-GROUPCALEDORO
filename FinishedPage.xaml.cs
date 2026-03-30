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

        // Check if user is logged in
        var userId = await SessionStorage.GetUserIdAsync();
        if (!userId.HasValue || userId <= 0)
        {
            await Shell.Current.GoToAsync("AuthPage");
            return;
        }

        _userId = userId.Value;

        // Load completed tasks from API
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

            items.Clear();

            if (response.Success && response.Data != null)
            {
                foreach (var apiTask in response.Data)
                {
                    var toDoItem = new ToDoItem
                    {
                        ID = apiTask.Id,
                        Name = apiTask.ItemName,
                        Notes = apiTask.ItemDescription,
                        Done = true
                    };
                    items.Add(toDoItem);
                }
                emptyLabel.IsVisible = items.Count == 0;
            }
        }
        catch (Exception ex)
        {
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
}
