
using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.Admin.DTOs;

public class UpdateUserRoleRequest
{
    [Required]
    [RegularExpression(
        "^(Employee|Agent)$",
        ErrorMessage = "Role must be Employee or Agent.")]
    public string Role { get; set; } = "Employee";

    /// <summary>
    /// Categories to assign when promoting to Agent. Optional — an agent with zero
    /// categories simply receives no tickets until they are set later via
    /// PUT /admin/users/{id}/categories. Ignored for Employee.
    /// </summary>
    public List<int>? CategoryIds { get; set; }
}
