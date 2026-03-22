using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper: scan all profiles in this assembly
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // FluentValidation: register all validators in this assembly
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}

