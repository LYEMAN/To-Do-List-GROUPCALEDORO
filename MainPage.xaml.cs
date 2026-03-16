namespace MauiApp2;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Linq;

public partial class MainPage : ContentPage
{
    private ObservableCollection<ToDoItem> items = new();
    private ToDoItem? selectedItem;
    private readonly TodoItemDB db = new();

    public MainPage()
    {
        InitializeComponent();
        todoLV.ItemsSource = items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var list = await db.GetItemsNotDoneAsync();
        items.Clear();
        foreach (var it in list)
            items.Add(it);
    }

    private async void AddToDoItem(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(titleEntry.Text))
            return;

        var item = new ToDoItem
        {
            ID = 0,
            Name = titleEntry.Text,
            Notes = detailsEditor.Text,
            Done = false
        };

        await db.SaveItemAsync(item);
        items.Add(item);

        ClearInputs();
    }

    private async void EditToDoItem(object sender, EventArgs e)
    {
        if (selectedItem == null)
            return;

        selectedItem.Name = titleEntry.Text;
        selectedItem.Notes = detailsEditor.Text;

        await db.SaveItemAsync(selectedItem);

        ResetEditMode();
    }

    private void CancelEdit(object sender, EventArgs e)
    {
        ResetEditMode();
    }

    private async void DeleteToDoItem(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var item = items.FirstOrDefault(x => x.ID == id);
            if (item != null)
            {
                await db.DeleteItemAsync(item);
                items.Remove(item);
            }
        }
    }

    private async void MarkDoneToDoItem(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var item = items.FirstOrDefault(x => x.ID == id);
            if (item != null)
            {
                item.Done = true;
                await db.SaveItemAsync(item);
                items.Remove(item);
            }
        }
    }

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

    private void ResetEditMode()
    {
        selectedItem = null;
        todoLV.SelectedItem = null;

        addBtn.IsVisible = true;
        editBtn.IsVisible = false;
        cancelBtn.IsVisible = false;

        ClearInputs();
    }

    private void ClearInputs()
    {
        titleEntry.Text = string.Empty;
        detailsEditor.Text = string.Empty;
    }
}
