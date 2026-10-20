using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.Admin.DTOs;

public class UpdatePriorityRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Level { get; set; }
}