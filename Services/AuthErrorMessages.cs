using System.Net;
using System.Text.Json;

namespace KiirLink.Services;

public enum AuthOperation
{
    Login,
    Register,
    ChangePassword
}

public static class AuthErrorMessages
{
    public static string FromResponse(HttpStatusCode statusCode, string? responseBody, AuthOperation operation)
    {
        if (statusCode == HttpStatusCode.TooManyRequests)
            return "Too many attempts. Wait a moment and try again.";

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return operation == AuthOperation.Login
                ? "Incorrect email or password."
                : "Your session has expired. Sign in again and retry.";

        var messages = ReadIdentityErrorCodes(responseBody)
            .Select(MapIdentityCode)
            .OfType<string>()
            .Distinct()
            .ToArray();

        return messages.Length > 0
            ? string.Join(Environment.NewLine, messages)
            : operation switch
        {
            AuthOperation.Login => "Incorrect email or password.",
            AuthOperation.Register => "Could not create the account. Check the email and password.",
            AuthOperation.ChangePassword => "Could not change the password. Check your current and new passwords.",
            _ => "Authentication failed. Please try again."
        };
    }

    private static IEnumerable<string> ReadIdentityErrorCodes(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return [];

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
                return [];

            return errors.EnumerateObject().Select(error => error.Name).ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? MapIdentityCode(string code) => code switch
    {
        "DuplicateEmail" or "DuplicateUserName" => "An account with this email already exists.",
        "InvalidEmail" or "InvalidUserName" => "Enter a valid email address.",
        "PasswordTooShort" => "The password is too short.",
        "PasswordRequiresDigit" => "The password must contain a number.",
        "PasswordRequiresLower" => "The password must contain a lowercase letter.",
        "PasswordRequiresUpper" => "The password must contain an uppercase letter.",
        "PasswordRequiresNonAlphanumeric" => "The password must contain a special character.",
        "PasswordRequiresUniqueChars" => "The password must contain more unique characters.",
        "PasswordMismatch" => "The current password is incorrect.",
        _ => null
    };
}
