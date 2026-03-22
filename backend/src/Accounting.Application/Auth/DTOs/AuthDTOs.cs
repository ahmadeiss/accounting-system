namespace Accounting.Application.Auth.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserProfileDto User);

public record RefreshTokenRequest(string RefreshToken);

public record UserProfileDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string RoleName,
    Guid? BranchId,
    IReadOnlyList<string> Permissions);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);

