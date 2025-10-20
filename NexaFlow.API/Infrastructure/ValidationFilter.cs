using FluentValidation;
using AppValidationException = NexaFlow.Application.Common.Exceptions.ValidationException;

namespace NexaFlow.API.Infrastructure;

/// <summary>
/// Minimal-API endpoint filter that runs the FluentValidation validator for the request
/// body type <typeparamref name="T"/> before the handler executes. (T-016)
/// </summary>
public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var arg = context.Arguments.OfType<T>().FirstOrDefault();
        if (arg is not null)
        {
            var result = await validator.ValidateAsync(arg);
            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                throw new AppValidationException(errors);
            }
        }

        return await next(context);
    }
}
