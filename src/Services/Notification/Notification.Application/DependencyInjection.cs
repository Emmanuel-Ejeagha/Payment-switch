using BuildingBlocks.Shared.Events;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Features.Commands.CreateNotification;
using Notification.Application.Features.Commands.SendPendingNotification;
using Notification.Application.Features.Queries.GetNotificationById;
using Notification.Application.Features.Queries.ListNotifications;

namespace Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateNotificationHandler>();
        services.AddScoped<SendPendingNotificationHandler>();
        services.AddScoped<GetNotificationByIdHandler>();
        services.AddScoped<ListNotificationsHandler>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddValidatorsFromAssemblyContaining<CreateNotificationCommandValidator>();

        return services;
    }
}