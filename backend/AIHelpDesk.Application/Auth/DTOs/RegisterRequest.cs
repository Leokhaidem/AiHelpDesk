
using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.Auth.DTOs;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Agent|Employee)$",
        ErrorMessage = "Role must be either Agent or Employee.")]
    public string Role { get; set; } = string.Empty;
}
