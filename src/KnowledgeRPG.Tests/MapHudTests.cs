using KnowledgeRPG.UI;
using System;
using Xunit;

namespace KnowledgeRPG.Tests;

public class MapHudTests
{
    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(59, "00:59")]
    [InlineData(61, "01:01")]
    [InlineData(65, "01:05")]
    [InlineData(3600, "1:00:00")]
    [InlineData(3661, "1:01:01")]
    public void FormatDurationHandlesMinutesAndHours(int totalSeconds, string expected)
    {
        Assert.Equal(expected, MapHud.FormatDuration(TimeSpan.FromSeconds(totalSeconds)));
    }
}
