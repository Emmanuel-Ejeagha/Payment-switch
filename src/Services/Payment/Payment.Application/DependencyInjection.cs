using BuildingBlocks.Shared.Events;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Features.Command.AuthorizePayment;
using Payment.Application.Features.Command.CapturePayment;
using Payment.Application.Features.Command.CreatePaymentIntent;
using Payment.Application.Features.Command.RefundPayment;
using Payment.Application.Features.Command.VoidPayment;
using Payment.Application.Features.Queries.GetPaymentIntentById;
using Payment.Application.Features.Queries.ListPaymentIntentsByMerchant;

namespace Payment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentApplication(this IServiceCollection services)
    {
        services.AddScoped<CreatePaymentIntentHandler>();
        services.AddScoped<AuthorizePaymentHandler>();
        services.AddScoped<CapturePaymentHandler>();
        services.AddScoped<VoidPaymentHandler>();
        services.AddScoped<RefundPaymentHandler>();
        services.AddScoped<GetPaymentIntentByIdHandler>();
        services.AddScoped<ListPaymentIntentsByMerchantHandler>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddValidatorsFromAssemblyContaining<CreatePaymentIntentCommandValidator>();

        return services;
    }
}