using FluentValidation;
using Hoarding.Application.Common.Interfaces;
using Hoarding.Application.Pricing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Hoarding.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddAutoMapper(assembly);
        services.AddValidatorsFromAssembly(assembly);

        // Pricing strategies (one per channel)
        services.AddScoped<IPricingStrategy, HoardingPricingStrategy>();
        services.AddScoped<IPricingStrategy, InfluencerPricingStrategy>();
        services.AddScoped<PricingStrategyResolver>();

        return services;
    }
}
