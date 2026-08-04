using BuildingBlocks.Shared.Events;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Features.Command.AuthorizePayment;
using Payment.Application.Features.Command.CapturePayment;
using Payment.Application.Features.Command.CheckoutPayment;
using Payment.Application.Features.Command.CheckoutTokenize;
using Payment.Application.Features.Command.ConfirmPaymentIntent;
using Payment.Application.Features.Command.CreateCardToken;
using Payment.Application.Features.Command.CreatePaymentIntent;
using Payment.Application.Features.Command.CreatePaymentLink;
using Payment.Application.Features.Command.RefundPayment;
using Payment.Application.Features.Command.ReplayWebhookEvent;
using Payment.Application.Features.Command.SendTestWebhookEvent;
using Payment.Application.Features.Command.VoidPayment;
using Payment.Application.Features.Queries.GetPaymentIntentById;
using Payment.Application.Features.Queries.GetPaymentLinkByCode;
using Payment.Application.Features.Queries.ListPaymentIntentsByMerchant;
using Payment.Application.Features.Queries.ListPaymentLinksByMerchant;
using Payment.Application.Features.Queries.ListWebhookEvents;

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
        services.AddScoped<ConfirmPaymentIntentHandler>();
        services.AddScoped<CreateCardTokenHandler>();
        services.AddScoped<CreatePaymentLinkHandler>();
        services.AddScoped<CheckoutTokenizeHandler>();
        services.AddScoped<CheckoutPaymentHandler>();
        services.AddScoped<GetPaymentIntentByIdHandler>();
        services.AddScoped<ListPaymentIntentsByMerchantHandler>();
        services.AddScoped<GetPaymentLinkByCodeHandler>();
        services.AddScoped<ListPaymentLinksByMerchantHandler>();
        services.AddScoped<ListWebhookEventsHandler>();
        services.AddScoped<ReplayWebhookEventHandler>();
        services.AddScoped<SendTestWebhookEventHandler>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddValidatorsFromAssemblyContaining<CreatePaymentIntentCommandValidator>();

        return services;
    }
}