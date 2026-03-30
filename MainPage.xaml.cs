namespace MauiApp2;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Maui.ApplicationModel;

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

    private static bool ConvertStatusToDone(System.Text.Json.JsonElement statusElement)
    {
        try
        {
            if (statusElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var s = statusElement.GetString();
                return string.Equals(s, "inactive", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "false", StringComparison.OrdinalIgnoreCase);
            }
            if (statusElement.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                if (statusElement.TryGetInt32(out var n))
                    return n != 1; // assume 1 == active, 0 == inactive
            }
            if (statusElement.ValueKind == System.Text.Json.JsonValueKind.True)
                return false; // true => active
            if (statusElement.ValueKind == System.Text.Json.JsonValueKind.False)
                return true; // false => inactive
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Load tasks when page appears
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Check if user is logged in
        var userId = await SessionStorage.GetUserIdAsync();
        System.Diagnostics.Debug.WriteLine($"[MainPage] OnAppearing retrieved user id = {userId}");
        if (!userId.HasValue || userId <= 0)
        {
            // User not logged in, go back to auth
            System.Diagnostics.Debug.WriteLine("[MainPage] No valid session, navigating to AuthPage");
            await Shell.Current.GoToAsync("//AuthPage");
            return;
        }

        _userId = userId.Value;

        // Load user display name
        var displayName = await SessionStorage.GetUserDisplayNameAsync();
        userGreeting.Text = $"Welcome, {displayName}!";

        // Load active tasks from API
        await LoadTasksAsync("active");

        // Show/hide empty state
        emptyLabel.IsVisible = items.Count == 0;
    }

    /// <summary>
    /// Load tasks from REST API
    /// </summary>
    private async Task LoadTasksAsync(string status)
    {
        try
        {
            var response = await _apiService.GetTasksAsync(_userId, status);

            System.Diagnostics.Debug.WriteLine($"[MainPage] GetTasks response: Success={response.Success} Message='{response.Message}' DataCount={(response.Data?.Count ?? 0)}");

            // If the request failed (network error / server unreachable), inform the user and do not clear existing items
            if (!response.Success)
            {
                // Show a non-blocking alert so the user knows why the list is empty
                await DisplayAlert("Network error", response.Message ?? "Failed to retrieve tasks", "OK");
                System.Diagnostics.Debug.WriteLine("[MainPage] GetTasks failed - keeping existing items");
                return;
            }

            var newItems = new List<ToDoItem>();
            if (response.Data != null)
            {
                foreach (var apiTask in response.Data)
                {
                    // Log API task for diagnostics
                    try
                    {
                        var statusText = apiTask.Status.ValueKind != System.Text.Json.JsonValueKind.Undefined ? apiTask.Status.GetRawText() : "<null>";
                        System.Diagnostics.Debug.WriteLine($"[MainPage] API Task id={apiTask.Id} name='{apiTask.ItemName}' desc='{apiTask.ItemDescription}' status={statusText}");
                    }
                    catch { }

                    var toDoItem = new ToDoItem
                    {
                        ID = apiTask.Id,
                        Name = apiTask.ItemName,
                        Notes = apiTask.ItemDescription,
                        Done = ConvertStatusToDone(apiTask.Status)
                    };
                    newItems.Add(toDoItem);
                }
            }

            // Update UI on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                items.Clear();
                foreach (var it in newItems)
                    items.Add(it);

                // Defensive: ensure ItemsSource is set
                if (todoLV.ItemsSource == null)
                    todoLV.ItemsSource = items;

                emptyLabel.IsVisible = items.Count == 0;
                System.Diagnostics.Debug.WriteLine($"[MainPage] UI items count = {items.Count}");
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load tasks: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Add new task to API
    /// </summary>
    private async void AddToDoItem(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(titleEntry.Text))
            return;

        try
        {
            var response = await _apiService.AddTaskAsync(
                _userId,
                titleEntry.Text,
                detailsEditor.Text ?? ""
            );

            if (response.Success && response.Data != null)
            {
                // Refresh tasks from server to ensure list matches backend state
                ClearInputs();
                await LoadTasksAsync("active");
                await DisplayAlert("Success", "Task added successfully", "OK");
            }
            else if (!string.IsNullOrEmpty(response.Message) && response.Message.IndexOf("add", StringComparison.OrdinalIgnoreCase) >= 0 && response.Message.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Server indicates success via message but did not return a usable data object.
                // Treat as success and refresh the list.
                ClearInputs();
                await LoadTasksAsync("active");
                await DisplayAlert("Success", "Task added successfully", "OK");
            }
            else
            {
                await DisplayAlert("Error", response.Message ?? "Failed to add task", "OK");
            }
        }
        catch (Exception ex)
        {
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

        try
        {
            var response = await _apiService.UpdateTaskAsync(
                selectedItem.ID,
                titleEntry.Text,
                detailsEditor.Text ?? ""
            );

            if (response.Success)
            {
                // Update local item
                selectedItem.Name = titleEntry.Text;
                selectedItem.Notes = detailsEditor.Text;

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
    private void TodoLV_OnItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        selectedItem = e.SelectedItem as ToDoItem;

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
            await Shell.Current.GoToAsync("//AuthPage");
        }
    }
}
