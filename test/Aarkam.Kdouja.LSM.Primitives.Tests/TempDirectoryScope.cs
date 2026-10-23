namespace Aarkam.Kdouja.LSM.Primitives.Tests;

/// <summary>
/// Provides an isolated, unique temporary directory on disk for integration and E2E tests.
/// Automatically cleans up all files and subdirectories on disposal with retry backoff for Windows file locks.
/// </summary>
public sealed class TempDirectoryScope : IDisposable, IAsyncDisposable
{
    private bool _disposed;

    public string DirectoryPath { get; }

    public TempDirectoryScope(string prefix = "kdouja_test")
    {
        DirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "Aarkam_Kdouja_Tests",
            $"{prefix}_{Guid.NewGuid():N}");

        Directory.CreateDirectory(DirectoryPath);
    }

    public string GetFilePath(string fileName) => Path.Combine(DirectoryPath, fileName);

    public string CreateSubdirectory(string relativePath)
    {
        string subPath = Path.Combine(DirectoryPath, relativePath);
        Directory.CreateDirectory(subPath);
        return subPath;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DeleteDirectoryWithRetries(DirectoryPath);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await Task.Run(() => DeleteDirectoryWithRetries(DirectoryPath)).ConfigureAwait(false);
    }

    private static void DeleteDirectoryWithRetries(string path)
    {
        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(attempt * 25);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(attempt * 25);
            }
            catch
            {
                // Best-effort cleanup on final attempt
                break;
            }
        }
    }
}
