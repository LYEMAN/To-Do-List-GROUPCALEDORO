namespace MauiApp2;

public partial class AppShell : Shell
{
    public const string AuthRoute = "//AuthPage";
    public const string MainRoute = "//MainTabs/MainPage";

    private bool _hasCheckedAuthStatus;

    public AppShell()
    {
        InitializeComponent();
        Loaded += OnShellLoaded;
    }

    private async Task CheckAuthStatusAsync()
    {
        try
        {
            var isLoggedIn = await SessionStorage.IsUserLoggedInAsync();

            if (isLoggedIn)
                await NavigateToMainAsync();
            else
                await NavigateToAuthAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CheckAuthStatusAsync Error: {ex}");
            await NavigateToAuthAsync();
        }
    }

    private async void OnShellLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnShellLoaded;
        await InitializeNavigationAsync();
    }

    private async Task InitializeNavigationAsync()
    {
        if (_hasCheckedAuthStatus)
            return;

        _hasCheckedAuthStatus = true;
        await CheckAuthStatusAsync();
    }

    public async Task NavigateToMainAsync()
    {
        try
        {
            CurrentItem = MainTabsTabBar;
            await GoToAsync(MainRoute);
        }
        catch
        {
            // Fallback for route resolution edge cases.
            await GoToAsync("//MainPage");
        }
    }

    public async Task NavigateToAuthAsync()
    {
        await GoToAsync(AuthRoute);
    }
}