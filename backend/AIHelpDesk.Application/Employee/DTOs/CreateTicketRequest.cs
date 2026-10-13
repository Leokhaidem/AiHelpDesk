using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.Employee.DTOs;

public class CreateTicketRequest
{
    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Low|Medium|High|Urgent)$")]
    public string Priority { get; set; } = "Medium";
}