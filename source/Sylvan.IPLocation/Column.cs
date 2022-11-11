namespace Sylvan.IPLocation;

static class ColumnMethods
{
    public static bool IsNumeric(this Column c)
    {
        switch (c)
        {
            case Column.Elevation:
            case Column.Latitude:
            case Column.Longitude:
                return true;
        }
        return false;
    }
    public static bool IsString(this Column c)
    {
        return !c.IsNumeric();
    }
}

public enum Column
{
    Country = 1,
    Region,
    City,
    Isp,
    Domain,
    ZipCode,
    Latitude,
    Longitude,
    TimeZone,
    NetSpeed,
    IddCode,
    AreaCode,
    WeatherStationCode,
    WeatherStationName,
    Mcc,
    Mnc,
    MobileBrand,
    Elevation,
    UsageType,
}
