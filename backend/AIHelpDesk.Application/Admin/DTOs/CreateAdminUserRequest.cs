
using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.Admin.DTOs;

public class CreateAdminUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Employee|Agent)$",
        ErrorMessage = "Role must be Employee or Agent.")]
    public string Role { get; set; } = "Employee";
}
