using System.Text.Json;
using System.IO;

namespace Romer.UI.Services;

public sealed class FileLogger
{
    private readonly string _path;
    private readonly object _gate = new();

    public FileLogger(string appName)
    {
        var directory = Path.Combine(Path.GetTempPath(), appName);
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "romer.log");
    }

    public void Info(string message, object? details = null)
    {
        Write("info", message, details);
    }

    public void Error(string message, object? details = null)
    {
        Write("error", message, details);
    }

    private void Write(string level, string message, object? details)
    {
        var payload = new
        {
            ts = DateTimeOffset.UtcNow,
            level,
            message,
            details
        };

        var line = JsonSerializer.Serialize(payload);
        lock (_gate)
        {
            File.AppendAllText(_path, line + Environment.NewLine);
        }
    }
}
