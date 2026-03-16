namespace MauiApp2;

using System;
using System.Collections.ObjectModel;
using System.Linq;

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
        var list = await db.GetFinishedItemsAsync();
        items.Clear();
        foreach (var it in list)
            items.Add(it);
    }

    private async void UndoDoneItem(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var item = items.FirstOrDefault(x => x.ID == id);
            if (item != null)
            {
                item.Done = false;
                await db.SaveItemAsync(item);
                items.Remove(item);
            }
        }
    }
}
