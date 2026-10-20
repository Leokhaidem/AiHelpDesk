using AIHelpDesk.Application.Tickets.DTOs;
using FluentValidation;

namespace AIHelpDesk.Application.Tickets.Validators;

public sealed class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(20).WithMessage("Description must be at least 20 characters so the AI can analyze it properly.")
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters.");
    }
}

public sealed class ResolveTicketRequestValidator : AbstractValidator<ResolveTicketRequest>
{
    public ResolveTicketRequestValidator()
    {
        RuleFor(x => x.Resolution)
            .NotEmpty().WithMessage("Resolution text is required.")
            .MinimumLength(10).WithMessage("Resolution must be at least 10 characters.")
            .MaximumLength(5000).WithMessage("Resolution must not exceed 5000 characters.");
    }
}

public sealed class AdminOverrideAnalysisRequestValidator : AbstractValidator<AdminOverrideAnalysisRequest>
{
    public AdminOverrideAnalysisRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("A valid CategoryId is required.");
        RuleFor(x => x.PriorityId).GreaterThan(0).WithMessage("A valid PriorityId is required.");
    }
}
