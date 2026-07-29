using CodexUsageCompanion.Lifecycle;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class InstanceCoordinatorTests
{
    [Fact]
    public void TryAcquireResidentAllowsOnlyOneOwner()
    {
        var name = $"CodexUsageCompanion.Tests.{Guid.NewGuid():N}";
        var first = new InstanceCoordinator(name);
        var second = new InstanceCoordinator(name);

        using var firstLease = first.TryAcquireResident();
        using var secondLease = second.TryAcquireResident();

        Assert.NotNull(firstLease);
        Assert.Null(secondLease);
    }

    [Fact]
    public void SignalRefreshWakesResidentOwner()
    {
        var name = $"CodexUsageCompanion.Tests.{Guid.NewGuid():N}";
        var owner = new InstanceCoordinator(name);
        var sender = new InstanceCoordinator(name);
        using var lease = owner.TryAcquireResident();

        Assert.NotNull(lease);
        Assert.False(lease.RefreshSignal.WaitOne(0));
        Assert.True(sender.SignalRefresh());
        Assert.True(lease.RefreshSignal.WaitOne(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void SignalRefreshReturnsFalseWithoutResidentOwner()
    {
        var name = $"CodexUsageCompanion.Tests.{Guid.NewGuid():N}";
        var sender = new InstanceCoordinator(name);

        Assert.False(sender.SignalRefresh());
    }
}
