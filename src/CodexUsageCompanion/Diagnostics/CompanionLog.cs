using System.IO;
using System.Text;

namespace CodexUsageCompanion.Diagnostics;

public sealed class CompanionLog
{
    private const int DefaultMaxBytes = 262144;
    private readonly object _sync = new();
    private readonly string _currentPath;
    private readonly string _previousPath;
    private readonly int _maxBytes;

    public CompanionLog(string? directory = null, int maxBytes = DefaultMaxBytes)
    {
        var root = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodexUsageCompanion");
        _currentPath = Path.Combine(root, "companion.log");
        _previousPath = Path.Combine(root, "companion.previous.log");
        _maxBytes = Math.Max(1, maxBytes);
    }

    public static CompanionLog Shared { get; } = new();

    public void Write(string area, Exception exception)
    {
        var entry = $"{DateTimeOffset.Now:O} [{area}] {exception.GetType().Name}: {exception.Message}{Environment.NewLine}{exception.StackTrace}{Environment.NewLine}";
        lock (_sync)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_currentPath)!);
                RotateIfNeeded(Encoding.UTF8.GetByteCount(entry));
                File.AppendAllText(_currentPath, entry, Encoding.UTF8);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private void RotateIfNeeded(int incomingBytes)
    {
        if (!File.Exists(_currentPath) || new FileInfo(_currentPath).Length + incomingBytes <= _maxBytes)
        {
            return;
        }

        File.Move(_currentPath, _previousPath, true);
    }
}
