
using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.Admin.DTOs;

public class UpdateUserRoleRequest
{
    [Required]
    [RegularExpression(
        "^(Employee|Agent)$",
        ErrorMessage = "Role must be Employee or Agent.")]
    public string Role { get; set; } = "Employee";
}
