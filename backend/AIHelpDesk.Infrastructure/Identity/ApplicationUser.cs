using Microsoft.AspNetCore.Identity;

namespace AIHelpDesk.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}