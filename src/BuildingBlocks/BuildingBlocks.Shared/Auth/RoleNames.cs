using System.Security.Claims;

namespace BuildingBlocks.Shared.Auth;

/// <summary>
/// Canonical role names and authorization policy strings (Step 7.5).
///
/// Roles are case-insensitive at every trust boundary: assignment normalizes
/// to canonical casing (<see cref="Normalize"/>), storage holds canonical
/// values, and issuance emits canonical claims. Never compare roles with
/// ordinal equality — use <see cref="Normalize"/>, <see cref="IsInRole"/>, or
/// the <c>Roles = RolePolicies.*</c> attribute constants.
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Merchant = "Merchant";
    public const string Support = "Support";

    public static readonly IReadOnlyList<string> All = [Admin, Merchant, Support];

    /// <summary>
    /// Canonicalizes a role (trims, case-insensitive match). Returns null for
    /// unknown roles.
    /// </summary>
    public static string? Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;

        var trimmed = role.Trim();
        foreach (var canonical in All)
        {
            if (string.Equals(canonical, trimmed, StringComparison.OrdinalIgnoreCase))
                return canonical;
        }
        return null;
    }

    public static bool IsKnown(string? role) => Normalize(role) is not null;

    /// <summary>
    /// Case-insensitive role check over the principal's role claims.
    /// Prefer this over <c>ClaimsPrincipal.IsInRole</c> (ordinal
    /// case-sensitive) so legacy non-canonical tokens fail safe to explicit
    /// policy rather than silent mismatch.
    /// </summary>
    public static bool IsInRole(ClaimsPrincipal? user, string role) =>
        user?.FindAll(ClaimTypes.Role).Any(c =>
            string.Equals(c.Value?.Trim(), role, StringComparison.OrdinalIgnoreCase)) == true;
}

/// <summary>
/// Authorization policy role strings for <c>[Authorize(Roles = ...)]</c>.
/// Must stay <c>const</c> for attribute use.
/// <list type="bullet">
/// <item><c>AdminOnly</c> — state transitions, money movement, role grants,
/// secrets/keys, job control.</item>
/// <item><c>ReadOnly</c> — read-only operational views granted to Support
/// (list/get merchants, settlements, notifications, reconciliation reports).
/// See <c>docs/roles.md</c> for the full matrix.</item>
/// </list>
/// </summary>
public static class RolePolicies
{
    public const string AdminOnly = RoleNames.Admin;
    public const string ReadOnly = RoleNames.Admin + "," + RoleNames.Support;
}
