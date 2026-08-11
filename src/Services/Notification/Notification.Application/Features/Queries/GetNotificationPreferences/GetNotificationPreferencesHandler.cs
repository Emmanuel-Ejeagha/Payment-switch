using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;

namespace Notification.Application.Features.Queries.GetNotificationPreferences;

public record GetNotificationPreferencesQuery(string Recipient);

public class GetNotificationPreferencesHandler
{
    private readonly INotificationPreferenceRepository _repository;
    private readonly ILogger<GetNotificationPreferencesHandler> _logger;

    public GetNotificationPreferencesHandler(
        INotificationPreferenceRepository repository,
        ILogger<GetNotificationPreferencesHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<NotificationPreferenceDto>>> Handle(
        GetNotificationPreferencesQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName} for {Recipient}", nameof(GetNotificationPreferencesQuery), query.Recipient);

        var preferences = await _repository.ListByRecipientAsync(query.Recipient.Trim().ToLowerInvariant(), cancellationToken);

        return preferences
            .Select(p => new NotificationPreferenceDto(p.Id, p.Recipient, p.Channel.Value, p.EventType, p.Enabled))
            .ToList();
    }
}
