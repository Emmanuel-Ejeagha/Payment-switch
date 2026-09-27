using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Tests;

public class UserLockoutTests
{
    private static readonly DateTime Now = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RegisterFailedLogin_BelowThreshold_DoesNotLock()
    {
        var user = CreateUser();

        for (var i = 0; i < User.MaxAccessFailedAttempts - 1; i++)
            user.RegisterFailedLogin(Now);

        Assert.False(user.IsLockedOut(Now));
        Assert.Equal(User.MaxAccessFailedAttempts - 1, user.AccessFailedCount);
        Assert.Null(user.LockoutEnd);
    }

    [Fact]
    public void RegisterFailedLogin_AtThreshold_LocksAccount()
    {
        var user = CreateUser();

        for (var i = 0; i < User.MaxAccessFailedAttempts; i++)
            user.RegisterFailedLogin(Now);

        Assert.True(user.IsLockedOut(Now));
        Assert.Equal(Now.AddMinutes(User.LockoutDurationMinutes), user.LockoutEnd);
        Assert.Equal(0, user.AccessFailedCount);
    }

    [Fact]
    public void IsLockedOut_WithinWindow_ReturnsTrue_AndFalseAfterExpiry()
    {
        var user = CreateUser();
        for (var i = 0; i < User.MaxAccessFailedAttempts; i++)
            user.RegisterFailedLogin(Now);

        Assert.True(user.IsLockedOut(Now.AddMinutes(User.LockoutDurationMinutes - 1)));
        Assert.False(user.IsLockedOut(Now.AddMinutes(User.LockoutDurationMinutes + 1)));
    }

    [Fact]
    public void IsLockedOut_NeverLocked_ReturnsFalse()
    {
        var user = CreateUser();
        Assert.False(user.IsLockedOut(Now));
    }

    [Fact]
    public void AfterLockoutExpiry_FailedAttemptsCountFresh()
    {
        var user = CreateUser();
        for (var i = 0; i < User.MaxAccessFailedAttempts; i++)
            user.RegisterFailedLogin(Now);

        user.RegisterFailedLogin(Now.AddMinutes(User.LockoutDurationMinutes + 1));

        Assert.Equal(1, user.AccessFailedCount);
        Assert.False(user.IsLockedOut(Now.AddMinutes(User.LockoutDurationMinutes + 2)));
    }

    [Fact]
    public void ResetAccessFailedCount_ClearsCounterAndLockout()
    {
        var user = CreateUser();
        for (var i = 0; i < User.MaxAccessFailedAttempts; i++)
            user.RegisterFailedLogin(Now);
        Assert.True(user.IsLockedOut(Now));

        user.ResetAccessFailedCount();

        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEnd);
        Assert.False(user.IsLockedOut(Now));
    }

    private static User CreateUser()
    {
        return new User(Guid.NewGuid(), new Email("user@domain.com"), new PasswordHash("hash"), new FullName("John Doe"));
    }
}
