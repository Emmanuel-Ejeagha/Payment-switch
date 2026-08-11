using BuildingBlocks.Shared.Events;
using FluentValidation;
using Identity.Application.Commands.ApiKey;
using Identity.Application.Commands.Auth.ChangePassword;
using Identity.Application.Commands.Auth.ForgotPassword;
using Identity.Application.Commands.Auth.Login;
using Identity.Application.Commands.Auth.Register;
using Identity.Application.Commands.Auth.ResetPassword;
using Identity.Application.Commands.Auth.ResendVerification;
using Identity.Application.Commands.Auth.Tokens;
using Identity.Application.Commands.Auth.VerifyEmail;
using Identity.Application.Commands.Role;
using Identity.Application.Queries.User;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<RevokeRefreshTokenHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ResendVerificationHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<GenerateApiKeyHandler>();
        services.AddScoped<RevokeApiKeyHandler>();
        services.AddScoped<AssignRoleHandler>();
        services.AddScoped<GetUserByIdHandler>();
        services.AddScoped<GetUserByEmailHandler>();
        services.AddScoped<GetApiKeysHandler>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();


        services.AddValidatorsFromAssemblyContaining<RegisterUserCommandValidator>();

        return services;
    }
}