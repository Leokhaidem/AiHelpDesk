using AIHelpDesk.Application.Categories.DTOs;
using AIHelpDesk.Application.Priorities.DTOs;
using AIHelpDesk.Application.Comments.DTOs;
using FluentValidation;

namespace AIHelpDesk.Application.Validators;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public sealed class CreatePriorityRequestValidator : AbstractValidator<CreatePriorityRequest>
{
    public CreatePriorityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Priority name is required.")
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters.");
        RuleFor(x => x.Level)
            .GreaterThan(0).WithMessage("Level must be a positive integer.");
    }
}

public sealed class UpdatePriorityRequestValidator : AbstractValidator<UpdatePriorityRequest>
{
    public UpdatePriorityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Priority name is required.")
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters.");
        RuleFor(x => x.Level)
            .GreaterThan(0).WithMessage("Level must be a positive integer.");
    }
}

public sealed class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required.")
            .MaximumLength(2000).WithMessage("Comment must not exceed 2000 characters.");
    }
}
