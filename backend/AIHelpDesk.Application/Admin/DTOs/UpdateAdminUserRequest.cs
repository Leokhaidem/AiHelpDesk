
using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.Admin.DTOs;

public class UpdateAdminUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
