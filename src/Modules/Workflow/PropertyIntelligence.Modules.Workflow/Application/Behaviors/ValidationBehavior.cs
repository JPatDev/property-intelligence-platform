using FluentValidation;
using MediatR;
using PropertyIntelligence.Modules.Workflow.Application.Errors;

namespace PropertyIntelligence.Modules.Workflow.Application.Behaviors;

internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validatorArray = validators as IValidator<TRequest>[] ?? validators.ToArray();
        if (validatorArray.Length == 0)
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validatorArray.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = results
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        if (errors.Count > 0)
        {
            throw new WorkflowValidationException(errors);
        }

        return await next();
    }
}
