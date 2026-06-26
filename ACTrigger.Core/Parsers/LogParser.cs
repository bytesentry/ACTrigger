using ACTrigger.Models;

namespace ACTrigger.Parsers;

public static class LogParser
{
    public static LogEntry? Parse(string line)
    {
        var parts = line.Split('|', 3);

        if (parts.Length != 3)
            return null;

        if (!DateTime.TryParse(parts[0], out var timestamp))
            return null;

        return new LogEntry
        {
            Timestamp = timestamp,
            Channel = parts[1],
            Message = parts[2]
        };
    }
}