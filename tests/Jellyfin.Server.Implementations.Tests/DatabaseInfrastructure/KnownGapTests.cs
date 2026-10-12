using System;
using System.Threading.Tasks;
using Jellyfin.Database.Testing;
using Xunit;
using Xunit.Sdk;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public class KnownGapTests
{
    private static readonly TestDbProvider _selected = TestDatabaseEnvironment.Provider;

    private static readonly TestDbProvider _other = _selected == TestDbProvider.Sqlite ? TestDbProvider.Postgres : TestDbProvider.Sqlite;

    private static readonly KnownGapDefinition _selectedGap = new(_selected, "test-gap", ex => ex is InvalidOperationException);

    private static readonly KnownGapDefinition _otherGap = new(_other, "test-gap", ex => ex is InvalidOperationException);

    private static readonly KnownGapDefinition _assertionGap = new(_selected, "test-gap", ex => ex is IAssertionException);

    [Fact]
    public void On_GapOnOtherProvider_RunsBody()
    {
        var ran = false;

        KnownGap.On(_otherGap, () => ran = true);

        Assert.True(ran);
    }

    [Fact]
    public async Task OnAsync_GapOnOtherProvider_RunsBody()
    {
        var ran = false;

        await KnownGap.OnAsync(_otherGap, () =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        Assert.True(ran);
    }

    [Fact]
    public void On_GapOnOtherProvider_DoesNotHideFailures()
    {
        Assert.Throws<InvalidOperationException>(
            () => KnownGap.On(_otherGap, () => Throw(new InvalidOperationException("boom"))));
    }

    [Fact]
    public void On_ThrowExpressionBody_PropagatesTheException()
    {
        Assert.Throws<InvalidOperationException>(
            () => KnownGap.On(_otherGap, () => throw new InvalidOperationException("boom")));
    }

    [Fact]
    public async Task OnAsync_GapOnOtherProvider_DoesNotHideFailures()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => KnownGap.OnAsync(_otherGap, () => Task.FromException(new InvalidOperationException("boom"))));
    }

    [Fact]
    public void On_GapOnSelectedProviderAndExceptionMatches_Skips()
    {
        var ex = CaptureSkip(() => KnownGap.On(_selectedGap, () => Throw(new InvalidOperationException("boom"))));

        Assert.Contains("test-gap", ex.Message, StringComparison.Ordinal);
        Assert.Contains("boom", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnAsync_GapOnSelectedProviderAndExceptionMatches_Skips()
    {
        var ex = await CaptureSkipAsync(() => KnownGap.OnAsync(_selectedGap, () => Task.FromException(new InvalidOperationException("boom"))));

        Assert.Contains("test-gap", ex.Message, StringComparison.Ordinal);
        Assert.Contains("boom", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void On_GapOnSelectedProviderAndExceptionDoesNotMatch_Fails()
    {
        Assert.Throws<ArgumentException>(
            () => FailOnSkip(() => KnownGap.On(_selectedGap, () => Throw(new ArgumentException("boom")))));
    }

    [Fact]
    public async Task OnAsync_GapOnSelectedProviderAndExceptionDoesNotMatch_Fails()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => FailOnSkipAsync(() => KnownGap.OnAsync(_selectedGap, () => Task.FromException(new ArgumentException("boom")))));
    }

    [Fact]
    public void On_GapOnSelectedProviderAndBodyPasses_Fails()
    {
        var ex = Assert.Throws<FailException>(() => KnownGap.On(_selectedGap, () => { }));

        Assert.Contains("remove the KnownGap marker", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnAsync_GapOnSelectedProviderAndBodyPasses_Fails()
    {
        var ex = await Assert.ThrowsAsync<FailException>(
            () => KnownGap.OnAsync(_selectedGap, () => Task.CompletedTask));

        Assert.Contains("remove the KnownGap marker", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void On_AssertionGapAndAssertionFails_Skips()
    {
        CaptureSkip(() => KnownGap.On(_assertionGap, () => Assert.Equal(1, 2)));
    }

    [Fact]
    public async Task OnAsync_AssertionGapAndAssertionFails_Skips()
    {
        await CaptureSkipAsync(() => KnownGap.OnAsync(_assertionGap, () =>
        {
            Assert.Equal(1, 2);
            return Task.CompletedTask;
        }));
    }

    [Fact]
    public void On_AssertionGapAndOtherExceptionThrown_Fails()
    {
        Assert.Throws<InvalidOperationException>(
            () => FailOnSkip(() => KnownGap.On(_assertionGap, () => Throw(new InvalidOperationException("boom")))));
    }

    [Fact]
    public async Task OnAsync_AssertionGapAndOtherExceptionThrown_Fails()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => FailOnSkipAsync(() => KnownGap.OnAsync(_assertionGap, () => Task.FromException(new InvalidOperationException("boom")))));
    }

    // Assert.Throws rethrows a SkipException so the test itself is reported as skipped.
    private static SkipException CaptureSkip(Action action)
    {
        try
        {
            action();
        }
        catch (SkipException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("The action did not skip.");
    }

    private static async Task<SkipException> CaptureSkipAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (SkipException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("The action did not skip.");
    }

    // Assert.Throws rethrows a SkipException too, which would report a swallowed failure as a skipped test.
    private static void FailOnSkip(Action action)
    {
        try
        {
            action();
        }
        catch (SkipException ex)
        {
            Assert.Fail($"The failure was skipped: {ex.Message}");
        }
    }

    private static async Task FailOnSkipAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (SkipException ex)
        {
            Assert.Fail($"The failure was skipped: {ex.Message}");
        }
    }

    private static void Throw(Exception exception) => throw exception;
}
