namespace MauiApp2;

using System;

public partial class AuthPage : ContentPage
{
    // API Service for REST calls
    private readonly ApiService _apiService = new();

    public AuthPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handle sign in with email and password via REST API
    /// </summary>
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        messageLabel.Text = string.Empty;
        var email = usernameEntry.Text?.Trim() ?? string.Empty;
        var password = passwordEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            messageLabel.Text = "Enter email and password.";
            return;
        }

        try
        {
            // Call API sign in
            var response = await _apiService.SignInAsync(email, password);

            if (response.Success && response.Data != null)
            {
                messageLabel.TextColor = Colors.Green;
                messageLabel.Text = "Login successful.";

                // Save user session for later use
                await SessionStorage.SaveUserSessionAsync(
                    response.Data.Id,
                    response.Data.FirstName ?? "",
                    response.Data.LastName ?? "",
                    response.Data.Email ?? ""
                );

                // Navigate to main page
                await Shell.Current.GoToAsync("MainPage");
            }
            else
            {
                messageLabel.TextColor = Colors.Red;
                messageLabel.Text = response.Message ?? "Invalid credentials.";
            }
        }
        catch (Exception ex)
        {
            messageLabel.TextColor = Colors.Red;
            messageLabel.Text = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Handle sign up via REST API
    /// </summary>
    private async void OnSignUpClicked(object sender, EventArgs e)
    {
        messageLabel.Text = string.Empty;

        // Get form values
        var firstName = usernameEntry.Text?.Trim() ?? string.Empty;
        var lastName = passwordEntry.Text?.Trim() ?? string.Empty;
        var email = usernameEntry.Text?.Trim() ?? string.Empty;
        var password = passwordEntry.Text ?? string.Empty;
        var confirmPassword = passwordEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
            string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            messageLabel.Text = "Please fill in all fields.";
            return;
        }

        if (password != confirmPassword)
        {
            messageLabel.Text = "Passwords do not match.";
            return;
        }

        try
        {
            // Call API sign up
            var response = await _apiService.SignUpAsync(firstName, lastName, email, password, confirmPassword);

            if (response.Success)
            {
                messageLabel.TextColor = Colors.Green;
                messageLabel.Text = "Account created successfully. You can now sign in.";
            }
            else
            {
                messageLabel.TextColor = Colors.Red;
                messageLabel.Text = response.Message ?? "Sign up failed.";
            }
        }
        catch (Exception ex)
        {
            messageLabel.TextColor = Colors.Red;
            messageLabel.Text = $"Error: {ex.Message}";
        }
    }
}
