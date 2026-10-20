using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace AIHelpDesk.Infrastructure.Identity;

public static class IdentitySeeder
{
    private static readonly string[] Roles =
    {
        "Admin",
        "Agent",
        "Employee"
    };

    public static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role '{role}'.");
                }
            }
        }
    }

    public static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Admin seed credentials are not configured.");
        }

        var existingUser = await userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return;
        }

        var fullName = configuration["AdminSeed:FullName"] ?? "System Administrator";

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };

        var result = await userManager.CreateAsync(
            admin,
            password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"Failed to create admin user: {errors}");
        }

        await userManager.AddToRoleAsync(
            admin,
            "Admin");
    }
}