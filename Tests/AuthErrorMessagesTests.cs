using System.Net;
using KiirLink.Services;

namespace KiirLink.Tests;

public sealed class AuthErrorMessagesTests
{
    [Fact]
    public void LoginUnauthorized_ReturnsCredentialMessage()
    {
        var message = AuthErrorMessages.FromResponse(
            HttpStatusCode.Unauthorized,
            null,
            AuthOperation.Login);

        Assert.Equal("Incorrect email or password.", message);
    }

    [Fact]
    public void RegisterDuplicateEmail_ReturnsFriendlyMessage()
    {
        const string body = """
            {
              "errors": {
                "DuplicateUserName": ["Username is already taken."]
              }
            }
            """;

        var message = AuthErrorMessages.FromResponse(
            HttpStatusCode.BadRequest,
            body,
            AuthOperation.Register);

        Assert.Equal("An account with this email already exists.", message);
    }

    [Fact]
    public void RegisterPasswordErrors_ReturnsReadableRequirements()
    {
        const string body = """
            {
              "errors": {
                "PasswordRequiresDigit": ["Technical message"],
                "PasswordRequiresUpper": ["Technical message"]
              }
            }
            """;

        var message = AuthErrorMessages.FromResponse(
            HttpStatusCode.BadRequest,
            body,
            AuthOperation.Register);

        Assert.Contains("number", message);
        Assert.Contains("uppercase", message);
        Assert.DoesNotContain("Technical", message);
    }

    [Fact]
    public void ChangePasswordMismatch_ReturnsCurrentPasswordMessage()
    {
        const string body = """
            {
              "errors": {
                "PasswordMismatch": ["Incorrect password."]
              }
            }
            """;

        var message = AuthErrorMessages.FromResponse(
            HttpStatusCode.BadRequest,
            body,
            AuthOperation.ChangePassword);

        Assert.Equal("The current password is incorrect.", message);
    }
}
