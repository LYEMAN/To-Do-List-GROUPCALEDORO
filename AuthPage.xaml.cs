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
    /// Show sign up form
    /// </summary>
    private void ShowSignUpForm(object sender, EventArgs e)
    {
        loginFrame.IsVisible = false;
        signupFrame.IsVisible = true;
        ClearSignupFields();
    }

    /// <summary>
    /// Show login form
    /// </summary>
    private void ShowLoginForm(object sender, EventArgs e)
    {
        loginFrame.IsVisible = true;
        signupFrame.IsVisible = false;
        ClearLoginFields();
    }

    /// <summary>
    /// Handle sign in with email and password via REST API
    /// </summary>
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        messageLabel.Text = string.Empty;
        var email = loginEmailEntry.Text?.Trim() ?? string.Empty;
        var password = loginPasswordEntry.Text ?? string.Empty;

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
                System.Diagnostics.Debug.WriteLine($"[AuthPage] SignIn response id={response.Data.Id}, email={response.Data.Email}");

                if (response.Data.Id <= 0)
                {
                    // Invalid user id returned by API — treat as failure
                    messageLabel.TextColor = Colors.Red;
                    messageLabel.Text = "Login failed: invalid user id returned by server.";
                    return;
                }

                await SessionStorage.SaveUserSessionAsync(
                    response.Data.Id,
                    response.Data.FirstName ?? "",
                    response.Data.LastName ?? "",
                    response.Data.Email ?? ""
                );

                // Debug: verify session saved
                var storedId = await SessionStorage.GetUserIdAsync();
                System.Diagnostics.Debug.WriteLine($"[AuthPage] Saved user id = {storedId}");

                // Navigate to main page
                System.Diagnostics.Debug.WriteLine("[AuthPage] Navigating to MainPage");
                await Shell.Current.GoToAsync("//MainPage");
                System.Diagnostics.Debug.WriteLine("[AuthPage] Navigation to MainPage completed");
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
        signupMessageLabel.Text = string.Empty;

        // Get form values
        var firstName = signupFirstNameEntry.Text?.Trim() ?? string.Empty;
        var lastName = signupLastNameEntry.Text?.Trim() ?? string.Empty;
        var email = signupEmailEntry.Text?.Trim() ?? string.Empty;
        var password = signupPasswordEntry.Text ?? string.Empty;
        var confirmPassword = confirmPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
            string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            signupMessageLabel.Text = "Please fill in all fields.";
            return;
        }

        if (password != confirmPassword)
        {
            signupMessageLabel.Text = "Passwords do not match.";
            return;
        }

        try
        {
            // Call API sign up
            var response = await _apiService.SignUpAsync(firstName, lastName, email, password, confirmPassword);

            if (response.Success)
            {
                signupMessageLabel.TextColor = Colors.Green;
                signupMessageLabel.Text = "Account created successfully. Signing you in...";

                // Auto-login after signup
                await Task.Delay(1500);
                var loginResponse = await _apiService.SignInAsync(email, password);

                if (loginResponse.Success && loginResponse.Data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AuthPage] Auto sign-in response id={loginResponse.Data.Id}, email={loginResponse.Data.Email}");

                    if (loginResponse.Data.Id <= 0)
                    {
                        signupMessageLabel.TextColor = Colors.Red;
                        signupMessageLabel.Text = "Sign up succeeded but invalid user id returned.";
                        return;
                    }
                    await SessionStorage.SaveUserSessionAsync(
                        loginResponse.Data.Id,
                        loginResponse.Data.FirstName ?? "",
                        loginResponse.Data.LastName ?? "",
                        loginResponse.Data.Email ?? ""
                    );

                    await Shell.Current.GoToAsync("//MainPage");
                }
            }
            else
            {
                signupMessageLabel.TextColor = Colors.Red;
                signupMessageLabel.Text = response.Message ?? "Sign up failed.";
            }
        }
        catch (Exception ex)
        {
            signupMessageLabel.TextColor = Colors.Red;
            signupMessageLabel.Text = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Clear login form fields
    /// </summary>
    private void ClearLoginFields()
    {
        loginEmailEntry.Text = string.Empty;
        loginPasswordEntry.Text = string.Empty;
        messageLabel.Text = string.Empty;
    }

    /// <summary>
    /// Clear signup form fields
    /// </summary>
    private void ClearSignupFields()
    {
        signupFirstNameEntry.Text = string.Empty;
        signupLastNameEntry.Text = string.Empty;
        signupEmailEntry.Text = string.Empty;
        signupPasswordEntry.Text = string.Empty;
        confirmPasswordEntry.Text = string.Empty;
        signupMessageLabel.Text = string.Empty;
    }
}
