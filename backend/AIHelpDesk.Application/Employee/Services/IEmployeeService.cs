using AIHelpDesk.Application.Employee.DTOs;

namespace AIHelpDesk.Application.Employee.Services;

public interface IEmployeeService
{
    Task<EmployeeTicketResponse> CreateTicketAsync(
        string employeeId,
        CreateTicketRequest request);

    Task<IReadOnlyList<EmployeeTicketResponse>> GetMyTicketsAsync(
        string employeeId);

    Task<EmployeeTicketResponse?> GetMyTicketByIdAsync(
        int ticketId,
        string employeeId);
}