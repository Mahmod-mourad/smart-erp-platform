using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace NexaFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddAutoMapper(cfg => { }, assembly);
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
