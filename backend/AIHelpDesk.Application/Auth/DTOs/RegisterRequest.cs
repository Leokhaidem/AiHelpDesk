using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.Auth.DTOs;

public class RegisterRequest
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// When true, an agent application is created alongside the Employee account.
    /// The user stays an Employee until an Admin approves the application.
    /// </summary>
    public bool ApplyAsAgent { get; set; }

    /// <summary>Categories the applicant wants to work on (required when ApplyAsAgent is true).</summary>
    public List<int> CategoryIds { get; set; } = new();

    [MaxLength(1000)]
    public string? AgentMessage { get; set; }
}