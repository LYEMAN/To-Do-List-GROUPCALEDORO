namespace MauiApp2;

using System;
using System.Collections.ObjectModel;

public partial class FinishedPage : ContentPage
{
    private ObservableCollection<ToDoItem> items = new();
    private readonly TodoItemDB db = new();

    public FinishedPage()
    {
        InitializeComponent();
        finishedLV.ItemsSource = items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var list = await db.GetItemsDoneAsync();
        items.Clear();
        foreach (var item in list)
            items.Add(item);
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
}
