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

        // Check if user is logged in
        var userId = await SessionStorage.GetUserIdAsync();
        if (!userId.HasValue || userId <= 0)
        {
            // User not logged in, go back to auth
            await Shell.Current.GoToAsync("//AuthPage");
            return;
        }

        _userId = userId.Value;

        // Load active tasks from API
        await LoadTasksAsync("active");
    }

    /// <summary>
    /// Load tasks from REST API
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
            }
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
                // Add to local list
                var item = new ToDoItem
                {
                    ID = response.Data.Id,
                    Name = response.Data.ItemName,
                    Notes = response.Data.ItemDescription,
                    Done = false
                };

                items.Add(item);
                ClearInputs();

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
}
