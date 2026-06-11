using System.Text.Json.Serialization;

namespace KiirLink.Models;

public sealed class CredentialsRequest
{
    [JsonPropertyName( "email" )] public string Email { get; set; } = string.Empty;

    [JsonPropertyName( "password" )] public string Password { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [JsonPropertyName( "oldPassword" )] public string OldPassword { get; set; } = string.Empty;

    [JsonPropertyName( "newPassword" )] public string NewPassword { get; set; } = string.Empty;
}

public class AccessTokenResponse
{
    [JsonPropertyName( "tokenType" )] public string? TokenType { get; set; }

    [JsonPropertyName( "accessToken" )] public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName( "expiresIn" )] public long ExpiresIn { get; set; }

    [JsonPropertyName( "refreshToken" )] public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshRequest
{
    [JsonPropertyName( "refreshToken" )] public string RefreshToken { get; set; } = string.Empty;
}

public class InfoResponse
{
    [JsonPropertyName( "email" )] public string Email { get; set; } = string.Empty;

    [JsonPropertyName( "isEmailConfirmed" )]
    public bool IsEmailConfirmed { get; set; }
}
