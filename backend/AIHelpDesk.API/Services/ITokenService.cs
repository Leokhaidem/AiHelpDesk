using AIHelpDesk.Application.Auth.DTOs;

namespace AIHelpDesk.API.Services;

public interface ITokenService
{
    /// <summary>Builds a signed JWT (30 minutes) carrying the user's id, email and roles.</summary>
    LoginResponse CreateToken(string userId, string email, IEnumerable<string> roles);
}