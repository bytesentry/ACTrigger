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

        FileStream stream = new(
            logFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        StreamReader reader = new(stream);

        stream.Seek(
            position,
            SeekOrigin.Begin);

        while (true)
        {
            try
            {
                if (position > stream.Length)
                {
                    Console.WriteLine(
                        "Log was recreated. Restarting at beginning.");

                    reader.Dispose();
                    stream.Dispose();

                    stream = new FileStream(
                        logFile,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite);

                    reader = new StreamReader(stream);

                    position = 0;
                }

                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    var entry =
                        LogParser.Parse(line);

                    if (entry != null)
                    {
                        try
                        {
                            LogEntryReceived?.Invoke(entry);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                $"LOG ENTRY ERROR: {ex}");
                        }
                    }
                }

                position = stream.Position;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"LOGWATCHER ERROR: {ex.Message}");

                reader.Dispose();
                stream.Dispose();

                stream = new FileStream(
                    logFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                reader = new StreamReader(stream);

                stream.Seek(
                    position,
                    SeekOrigin.Begin);
            }

            await Task.Delay(50);
        }
    }
}