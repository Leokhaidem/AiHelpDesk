
using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.Admin.DTOs;

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}
