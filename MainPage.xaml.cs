namespace MauiApp2;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

public partial class MainPage : ContentPage
{
    private ObservableCollection<ToDoItem> items = new();
    private ToDoItem? selectedItem;

    // API Service for REST calls
    private readonly ApiService _apiService = new();

    // Current logged-in user
    private int _userId = 0;

    public MainPage()
    {
        InitializeComponent();
        todoLV.ItemsSource = items;
    }

    /// <summary>
    /// Load tasks when page appears
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            System.Diagnostics.Debug.WriteLine("MainPage.OnAppearing called");

            // Get current user ID (already validated by AppShell)
            var userId = await SessionStorage.GetUserIdAsync();
            if (!userId.HasValue || userId <= 0)
            {
                System.Diagnostics.Debug.WriteLine("MainPage: No valid user ID found");
                return;
            }

            _userId = userId.Value;

            System.Diagnostics.Debug.WriteLine($"MainPage: UserId = {_userId}, loading tasks...");

            // Load user display name
            var displayName = await SessionStorage.GetUserDisplayNameAsync();
            userGreeting.Text = $"Welcome, {displayName ?? "User"}!";

            // Load active tasks from API
            await LoadTasksAsync("active");

            // Show/hide empty state
            emptyLabel.IsVisible = items.Count == 0;
            System.Diagnostics.Debug.WriteLine($"MainPage: Loaded {items.Count} tasks");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainPage.OnAppearing Error: {ex}");
            await DisplayAlert("Error", $"Failed to load page: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Load tasks from REST API
    /// </summary>
    private async Task LoadTasksAsync(string status)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"LoadTasksAsync START - Loading {status} tasks for userId {_userId}");

            var response = await _apiService.GetTasksAsync(_userId, status);

            System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - API Response: Success={response?.Success}, Data Count={response?.Data?.Count}, Message={response?.Message}");

            items.Clear();
            System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - Cleared items collection");

            if (response != null && response.Success && response.Data != null)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - Processing {response.Data.Count} tasks from API");

                foreach (var apiTask in response.Data)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - Adding task: Id={apiTask.Id}, Name={apiTask.ItemName}, Status={apiTask.Status}");

                    // Convert API TaskItem to ToDoItem
                    var toDoItem = new ToDoItem
                    {
                        ID = apiTask.Id,
                        Name = apiTask.ItemName,
                        Notes = apiTask.ItemDescription,
                        Done = apiTask.Status == "inactive"
                    };
                    items.Add(toDoItem);
                }

                System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - Successfully added {items.Count} items to collection");
            }
            else if (response != null)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - API Response Error: {response.Message}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"LoadTasksAsync - Response is null!");
            }

            emptyLabel.IsVisible = items.Count == 0;
            System.Diagnostics.Debug.WriteLine($"LoadTasksAsync END - Total items: {items.Count}, Empty label visible: {emptyLabel.IsVisible}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTasksAsync ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"LoadTasksAsync ERROR STACKTRACE: {ex.StackTrace}");
            items.Clear();
            emptyLabel.IsVisible = true;
            await DisplayAlert("Error", $"Failed to load tasks: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Add new task to API
    /// </summary>
    private async void AddToDoItem(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(titleEntry.Text))
        {
            await DisplayAlert("Error", "Please enter a task title", "OK");
            return;
        }

        try
        {
            var taskName = titleEntry.Text.Trim();
            var taskDescription = detailsEditor.Text?.Trim() ?? "";

            var response = await _apiService.AddTaskAsync(
                _userId,
                taskName,
                taskDescription
            );

            if (response != null && response.Success && response.Data != null)
            {
                // Add to local list
                var item = new ToDoItem
                {
                    ID = response.Data.Id,
                    Name = response.Data.ItemName ?? taskName,
                    Notes = response.Data.ItemDescription ?? taskDescription,
                    Done = false
                };

                items.Add(item);
                emptyLabel.IsVisible = items.Count == 0;
                ClearInputs();

                await DisplayAlert("Success", "Task added successfully", "OK");
            }
            else
            {
                await DisplayAlert("Error", response?.Message ?? "Failed to add task", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AddToDoItem Error: {ex}");
            await DisplayAlert("Error", $"Error adding task: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Edit selected task
    /// </summary>
    private async void EditToDoItem(object sender, EventArgs e)
    {
        if (selectedItem == null)
            return;

        if (string.IsNullOrWhiteSpace(titleEntry.Text))
        {
            await DisplayAlert("Error", "Please enter a task title", "OK");
            return;
        }

        try
        {
            var taskName = titleEntry.Text.Trim();
            var taskDescription = detailsEditor.Text?.Trim() ?? string.Empty;

            var response = await _apiService.UpdateTaskAsync(
                selectedItem.ID,
                taskName,
                taskDescription
            );

            if (response.Success)
            {
                // Update local item
                selectedItem.Name = taskName;
                selectedItem.Notes = taskDescription;

                await DisplayAlert("Success", "Task updated successfully", "OK");
                ResetEditMode();
            }
            else
            {
                await DisplayAlert("Error", response.Message ?? "Failed to update task", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error updating task: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Cancel editing
    /// </summary>
    private void CancelEdit(object sender, EventArgs e)
    {
        ResetEditMode();
    }

    /// <summary>
    /// Delete task from API
    /// </summary>
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

    /// <summary>
    /// Mark task as done (inactive)
    /// </summary>
    private async void MarkDoneToDoItem(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var item = items.FirstOrDefault(x => x.ID == id);
            if (item != null)
            {
                try
                {
                    // Change status to inactive via API
                    var response = await _apiService.ChangeTaskStatusAsync(id, "inactive");

                    if (response.Success)
                    {
                        item.Done = true;
                        items.Remove(item);
                        await DisplayAlert("Success", "Task marked as completed", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", response.Message ?? "Failed to mark task as done", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error updating task: {ex.Message}", "OK");
                }
            }
        }
    }

    /// <summary>
    /// Handle item selection for editing
    /// </summary>
    private void TodoLV_OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        selectedItem = e.CurrentSelection.FirstOrDefault() as ToDoItem;

        if (selectedItem == null)
            return;

        titleEntry.Text = selectedItem.Name;
        detailsEditor.Text = selectedItem.Notes;

        addBtn.IsVisible = false;
        editBtn.IsVisible = true;
        cancelBtn.IsVisible = true;
    }

    /// <summary>
    /// Reset edit mode
    /// </summary>
    private void ResetEditMode()
    {
        selectedItem = null;
        todoLV.SelectedItem = null;

        addBtn.IsVisible = true;
        editBtn.IsVisible = false;
        cancelBtn.IsVisible = false;

        ClearInputs();
    }

    /// <summary>
    /// Clear input fields
    /// </summary>
    private void ClearInputs()
    {
        titleEntry.Text = string.Empty;
        detailsEditor.Text = string.Empty;
    }

    /// <summary>
    /// Handle logout
    /// </summary>
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirmed)
        {
            await SessionStorage.ClearSessionAsync();
            if (Shell.Current is AppShell appShell)
                await appShell.NavigateToAuthAsync();
            else
                await Shell.Current.GoToAsync(AppShell.AuthRoute);
        }
    }

    /// <summary>
    /// Manual refresh button
    /// </summary>
    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Refresh button clicked");
        await LoadTasksAsync("active");
    }
}
