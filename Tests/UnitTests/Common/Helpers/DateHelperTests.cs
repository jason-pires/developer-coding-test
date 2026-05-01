using Common.Helpers;

namespace UnitTests.Common.Helpers;

public class DateHelperTests
{
    [Fact]
    public void ConvertUnixTimeToDateTime_ReturnsEpoch_ForZero()
    {
        var dt = DateHelper.ConvertUnixTimeToDateTime(0);

        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), dt);
    }

    [Fact]
    public void ConvertUnixTimeToDateTime_AddsSecondsCorrectly()
    {
        var dt = DateHelper.ConvertUnixTimeToDateTime(1_700_000_000);

        Assert.Equal(new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc), dt);
    }
}
