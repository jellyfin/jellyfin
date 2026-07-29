using System;
using System.Diagnostics;
using System.Linq;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding.FFProcessing;

/// <summary>
/// Pins <see cref="FFActionPolicy.For"/>.
/// <para>
/// Every arm of that table states only what makes its action unusual and inherits the rest, which
/// makes it cheap to write and easy to break: deleting a line does not fail to compile, it silently
/// swaps in a background default. Several of those defaults are wrong in ways nothing observes at
/// run time — an interrogation that quietly loses its deadline still works until the day it hangs.
/// These tests are what turns that into a build failure.
/// </para>
/// </summary>
public static class FFActionPolicyTests
{
    /// <summary>
    /// Gets the distinctive values per action: what each arm exists to say. Anything not listed is
    /// inherited and is covered by the cross-cutting invariants below instead.
    /// </summary>
    public static TheoryData<FFAction, bool, bool, FFStdinMode, bool, bool> Distinctive => new()
    {
        // action, probeOnly, overwrite, stdin, requiresProgressStats, stderrIsPayload
        { FFAction.Probe, true, false, FFStdinMode.FireAndForget, false, false },
        { FFAction.ScanKeyframes, true, false, FFStdinMode.FireAndForget, false, false },
        { FFAction.ValidateBinary, false, false, FFStdinMode.FireAndForget, false, false },
        { FFAction.Capabilities, false, false, FFStdinMode.FireAndForget, false, true },
        { FFAction.ProbeRuntimeKeys, false, false, FFStdinMode.ControlChannel, false, true },
        { FFAction.MeasureLoudness, false, false, FFStdinMode.FireAndForget, false, true },
        { FFAction.ExtractAttachment, false, true, FFStdinMode.FireAndForget, false, false },
        { FFAction.ExtractSubtitle, false, true, FFStdinMode.FireAndForget, false, false },
        { FFAction.ExtractImage, false, true, FFStdinMode.FireAndForget, false, false },
        { FFAction.GenerateTrickplay, false, true, FFStdinMode.FireAndForget, false, false },
        { FFAction.Record, false, true, FFStdinMode.ControlChannel, false, false },
        { FFAction.Stream, false, true, FFStdinMode.ControlChannel, true, false },
    };

    /// <summary>Gets the deadline and priority each action is expected to carry.</summary>
    public static TheoryData<FFAction, TimeSpan, TimeSpan, ProcessPriorityClass> Bounds => new()
    {
        // action, timeout, idleTimeout, priority
        { FFAction.Probe, FFActionPolicy.MetadataRead, FFDefaults.Unbounded, ProcessPriorityClass.BelowNormal },
        { FFAction.ScanKeyframes, FFActionPolicy.FullFileScan, FFDefaults.Unbounded, ProcessPriorityClass.BelowNormal },
        { FFAction.ValidateBinary, FFActionPolicy.StartupInterrogation, FFDefaults.Unbounded, FFDefaults.InheritPriority },
        { FFAction.Capabilities, FFActionPolicy.StartupInterrogation, FFDefaults.Unbounded, FFDefaults.InheritPriority },
        { FFAction.ProbeRuntimeKeys, FFActionPolicy.StartupInterrogation, FFDefaults.Unbounded, FFDefaults.InheritPriority },
        { FFAction.MeasureLoudness, FFActionPolicy.FullAudioScan, FFDefaults.Unbounded, ProcessPriorityClass.BelowNormal },
        { FFAction.ExtractAttachment, FFActionPolicy.MetadataRead, FFDefaults.Unbounded, ProcessPriorityClass.BelowNormal },
        { FFAction.ExtractSubtitle, FFActionPolicy.DefaultSubtitleExtraction, FFDefaults.Unbounded, ProcessPriorityClass.BelowNormal },
        { FFAction.ExtractImage, FFActionPolicy.DefaultImageExtraction, FFDefaults.Unbounded, ProcessPriorityClass.BelowNormal },
        { FFAction.GenerateTrickplay, FFDefaults.Unbounded, FFActionPolicy.DefaultImageExtraction, ProcessPriorityClass.BelowNormal },
        { FFAction.Record, FFDefaults.Unbounded, FFDefaults.Unbounded, ProcessPriorityClass.Normal },
        { FFAction.Stream, FFDefaults.Unbounded, FFDefaults.Unbounded, ProcessPriorityClass.Normal },
    };

    public static TheoryData<FFAction> AllActions => new(Enum.GetValues<FFAction>());

    [Theory]
    [MemberData(nameof(AllActions))]
    public static void For_DefinesAPolicyForEveryAction(FFAction action)
    {
        // The table's fallback arm throws, so a newly added action with no arm of its own fails here
        // rather than at the moment something first tries to run it.
        var exception = Record.Exception(() => FFActionPolicy.For(action));

        Assert.Null(exception);
    }

    [Fact]
    public static void For_RejectsAnActionOutsideTheEnum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FFActionPolicy.For((FFAction)(-1)));
    }

    [Fact]
    public static void EveryActionIsPinnedByTheseTests()
    {
        // Guards the tables above, which are hand-maintained: adding an action without adding rows
        // would otherwise leave it silently unpinned while everything still passes.
        var actions = Enum.GetValues<FFAction>();

        Assert.Equal(actions.Length, Distinctive.Count);
        Assert.Equal(actions.Length, Bounds.Count);
    }

    [Theory]
    [MemberData(nameof(Distinctive))]
    public static void For_CarriesTheDistinctiveValues(
        FFAction action,
        bool probeOnly,
        bool overwrite,
        FFStdinMode stdin,
        bool requiresProgressStats,
        bool stderrIsPayload)
    {
        var policy = FFActionPolicy.For(action);

        Assert.Equal(probeOnly, policy.ProbeOnly);
        Assert.Equal(overwrite, policy.Overwrite);
        Assert.Equal(stdin, policy.Stdin);
        Assert.Equal(requiresProgressStats, policy.RequiresProgressStats);
        Assert.Equal(stderrIsPayload, policy.StderrIsPayload);
    }

    [Theory]
    [MemberData(nameof(Bounds))]
    public static void For_CarriesTheExpectedBounds(
        FFAction action,
        TimeSpan timeout,
        TimeSpan idleTimeout,
        ProcessPriorityClass priority)
    {
        var policy = FFActionPolicy.For(action);

        Assert.Equal(timeout, policy.Timeout);
        Assert.Equal(idleTimeout, policy.IdleTimeout);
        Assert.Equal(priority, policy.Priority);
    }

    [Theory]
    [MemberData(nameof(AllActions))]
    public static void NoActionIsUnboundedInEveryDimension(FFAction action)
    {
        var policy = FFActionPolicy.For(action);

        if (policy.Timeout != FFDefaults.Unbounded)
        {
            return;
        }

        // An action may drop the wall clock only because something else bounds it: an idle watchdog,
        // or a control channel that lets the caller stop it when the viewer or the recording is done.
        // Losing both is how a process becomes unkillable in practice, and nothing at run time would
        // report it — the run simply never ends.
        Assert.True(
            policy.IdleTimeout != FFDefaults.Unbounded || policy.Stdin == FFStdinMode.ControlChannel,
            $"{action} has no wall clock, no idle watchdog and no way to be asked to stop.");
    }

    [Theory]
    [MemberData(nameof(AllActions))]
    public static void OnlyASteerableActionCanBeAskedToStop(FFAction action)
    {
        var policy = FFActionPolicy.For(action);

        if (policy.GracefulStopTimeout == TimeSpan.Zero)
        {
            return;
        }

        // The grace period is the wait after writing "q" to stdin. An action that closes stdin has
        // nowhere to write it, so the grace would be spent waiting for a message never delivered
        // before killing the process anyway.
        Assert.Equal(FFStdinMode.ControlChannel, policy.Stdin);
    }

    [Theory]
    [MemberData(nameof(AllActions))]
    public static void AProbeNeverClaimsToOverwrite(FFAction action)
    {
        var policy = FFActionPolicy.For(action);

        if (!policy.ProbeOnly)
        {
            return;
        }

        // ffprobe registers neither -y nor -n, so the runner omits them for a probe. Setting the flag
        // would therefore be inert, and an inert setting that reads as meaningful is worse than none.
        Assert.False(policy.Overwrite);
    }

    [Fact]
    public static void OnlyTrickplayArmsTheIdleWatchdog()
    {
        // Stated as a rule in the table's own documentation. The watchdog only means anything where
        // the request supplies a progress signal, and trickplay's tile count is the only one.
        var armed = Enum.GetValues<FFAction>()
            .Where(a => FFActionPolicy.For(a).IdleTimeout != FFDefaults.Unbounded)
            .ToArray();

        Assert.Equal([FFAction.GenerateTrickplay], armed);
    }

    [Fact]
    public static void OnlyStderrScrapersFloorTheLogLevel()
    {
        // StderrIsPayload both floors -loglevel at info and retains all of stderr rather than a
        // trailing window. Every action that parses its own stderr for the answer must be here; the
        // failure mode for a missing one is silent, so the set is pinned rather than spot-checked.
        var payload = Enum.GetValues<FFAction>()
            .Where(a => FFActionPolicy.For(a).StderrIsPayload)
            .ToArray();

        Assert.Equal(
            [FFAction.Capabilities, FFAction.ProbeRuntimeKeys, FFAction.MeasureLoudness],
            payload);
    }
}
