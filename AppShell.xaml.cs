namespace MauiApp2;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CheckAuthStatusAsync();
    }

    private async Task CheckAuthStatusAsync()
    {
        try
        {
            var userId = await SessionStorage.GetUserIdAsync();
            bool isLoggedIn = userId.HasValue && userId > 0;

            if (isLoggedIn)
            {
                // User is logged in, show TabBar
                await GoToAsync("//MainPage");
            }
            else
            {
                // User is not logged in, show Auth page
                await GoToAsync("//AuthPage");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CheckAuthStatusAsync Error: {ex}");
            await GoToAsync("//AuthPage");
        }
    }
}