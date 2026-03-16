namespace MauiApp2;

using System;

public partial class AuthPage : ContentPage
{
    private readonly UserDB db = new();

    public AuthPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        messageLabel.Text = string.Empty;
        var username = usernameEntry.Text?.Trim() ?? string.Empty;
        var password = passwordEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            messageLabel.Text = "Enter username and password.";
            return;
        }

        var ok = await db.ValidateUserAsync(username, password);
        if (ok)
        {
            messageLabel.TextColor = Colors.Green;
            messageLabel.Text = "Login successful.";
            // navigate to todo tab page
            await Shell.Current.GoToAsync("//Todo");
        }
        else
        {
            messageLabel.TextColor = Colors.Red;
            messageLabel.Text = "Invalid credentials.";
        }
    }

    private async void OnSignUpClicked(object sender, EventArgs e)
    {
        messageLabel.Text = string.Empty;
        var username = usernameEntry.Text?.Trim() ?? string.Empty;
        var password = passwordEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            messageLabel.Text = "Enter username and password.";
            return;
        }

        var res = await db.CreateUserAsync(username, password);
        if (res > 0)
        {
            messageLabel.TextColor = Colors.Green;
            messageLabel.Text = "User created. You can login now.";
        }
        else
        {
            messageLabel.TextColor = Colors.Red;
            messageLabel.Text = "User already exists.";
        }
    }
}
