using GorgeStudio.Models.Timeline;
using Xunit;

namespace GorgeStudio.Tests.Models.Timeline;

public class TimelineSnapperTests
{
    [Theory]
    [InlineData(120, 2.0)]
    [InlineData(60, 4.0)]
    [InlineData(240, 1.0)]
    public void Snap_BarMode_ReturnsSecondsFromBpm(int bpm, double expectedSeconds)
    {
        var snapped = TimelineSnapper.Snap(
            expectedSeconds * 0.98,
            true,
            TimelineSnapMode.Bar,
            bpm,
            0,
            4,
            4);

        Assert.Equal(expectedSeconds, snapped, precision: 6);
    }

    [Fact]
    public void Snap_BeatMode_AppliesOffsetToAbsoluteTimelinePositions()
    {
        var snapped = TimelineSnapper.Snap(
            0.92,
            true,
            TimelineSnapMode.Beat,
            120,
            500,
            4,
            4);

        Assert.Equal(1.0, snapped, precision: 6);
    }

    [Fact]
    public void Snap_SubdivisionMode_ReturnsSubdivisionSeconds()
    {
        var snapped = TimelineSnapper.Snap(
            0.13,
            true,
            TimelineSnapMode.Subdivision,
            120,
            0,
            4,
            4);

        Assert.Equal(0.125, snapped, precision: 6);
    }

    [Fact]
    public void SnapDuration_DoesNotApplyTimelineOffset()
    {
        var snapped = TimelineSnapper.SnapDuration(
            1.9,
            true,
            TimelineSnapMode.Bar,
            120,
            4,
            4);

        Assert.Equal(2.0, snapped, precision: 6);
    }

    [Fact]
    public void DurationGridIndexToSeconds_ReturnsSecondsFromBpm()
    {
        var seconds = TimelineTimeConverter.DurationGridIndexToSeconds(
            1,
            TimelineSnapMode.Bar,
            60,
            4,
            4);

        Assert.Equal(4.0, seconds, precision: 6);
    }
}
