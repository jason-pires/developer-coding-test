namespace Common.Helpers
{
    public class DateHelper
    {
        public static DateTime ConvertUnixTimeToDateTime(long unixTime)
        {
            // Unix time is seconds past epoch
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }
    }
}
