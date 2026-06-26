using ACTrigger.Models;
using ACTrigger.Parsers;

namespace ACTrigger.Core.Services;

public class LogWatcher
{
    public event Action<LogEntry>? LogEntryReceived;

    public async Task StartAsync(string logFile)
    {
        long position;

        using (var initStream = new FileStream(
            logFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite))
        {
            position = initStream.Length;
        }

        while (true)
        {
            try
            {
                using var stream = new FileStream(
                    logFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                stream.Seek(
                    position,
                    SeekOrigin.Begin);

                using var reader =
                    new StreamReader(stream);

                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    var entry =
                        LogParser.Parse(line);

                    if (entry != null)
                    {
                        LogEntryReceived?.Invoke(entry);
                    }
                }

                position =
                    stream.Position;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"LOGWATCHER ERROR: {ex.Message}");
            }

            await Task.Delay(10);
        }
    }
}