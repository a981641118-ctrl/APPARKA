namespace ApparkaTrainingFlowOnline.Services;

public class PeruClock
{
    private readonly TimeZoneInfo _timeZone;

    public PeruClock()
    {
        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTimeOffset Now => ToLocal(UtcNow);
    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    public DateTimeOffset At(DateOnly date, TimeOnly time)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        var localOffset = new DateTimeOffset(local, _timeZone.GetUtcOffset(local));
        return localOffset.ToUniversalTime();
    }

    public DateTimeOffset ToLocal(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, _timeZone);

    public string Format(DateTimeOffset value, string format = "dd/MM/yyyy HH:mm") =>
        ToLocal(value).ToString(format);
}
