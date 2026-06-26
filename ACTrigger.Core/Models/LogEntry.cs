namespace ACTrigger.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; }

    public string Channel { get; set; } = "";

    public string Message { get; set; } = "";
}