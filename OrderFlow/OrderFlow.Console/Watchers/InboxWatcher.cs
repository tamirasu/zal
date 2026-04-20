using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;
using OrderFlow.Console.Services;

namespace OrderFlow.Console.Watchers;

public class InboxWatcher : IDisposable
{
    private readonly OrderPipeline   _pipeline;
    private readonly OrderRepository _repo;
    private readonly FileSystemWatcher _watcher;
    private readonly SemaphoreSlim  _semaphore = new(2);          // max 2 pliki równocześnie
    private readonly HashSet<string> _inProgress = new();
    private readonly object          _lockSet    = new();

    public InboxWatcher(string inboxPath, OrderPipeline pipeline, OrderRepository repo)
    {
        _pipeline = pipeline;
        _repo     = repo;

        Directory.CreateDirectory(inboxPath);
        Directory.CreateDirectory(Path.Combine(inboxPath, "processed"));
        Directory.CreateDirectory(Path.Combine(inboxPath, "failed"));

        _watcher = new FileSystemWatcher(Path.GetFullPath(inboxPath), "*.json")
        {
            NotifyFilter        = NotifyFilters.FileName | NotifyFilters.CreationTime,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnCreated;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        lock (_lockSet)
        {
            if (!_inProgress.Add(e.FullPath))
                return;    // już przetwarzamy ten plik
        }

        _ = Task.Run(() => ProcessAsync(e.FullPath));
    }

    private async Task ProcessAsync(string filePath)
    {
        await _semaphore.WaitAsync();
        try
        {
            System.Console.WriteLine($"  [INBOX] Wykryto: {Path.GetFileName(filePath)}");

            // Retry — plik może być jeszcze zajęty przez twórcę
            List<Order>? orders = null;
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    orders = await _repo.LoadFromJsonAsync(filePath);
                    break;
                }
                catch (IOException)
                {
                    System.Console.WriteLine($"  [INBOX] Plik zajęty, próba {attempt}/5...");
                    await Task.Delay(Random.Shared.Next(200, 501));
                }
            }

            if (orders == null || orders.Count == 0)
            {
                System.Console.WriteLine($"  [INBOX] Plik pusty lub błąd odczytu: {Path.GetFileName(filePath)}");
                return;
            }

            System.Console.WriteLine($"  [INBOX] Załadowano {orders.Count} zamówień");

            // Przepuszczamy przez pipeline — odpalają się zdarzenia StatusChanged z Lab 2
            foreach (var order in orders)
            {
                order.Status = OrderStatus.New;
                _pipeline.ProcessOrder(order);
            }

            // Przenosimy do processed/
            var dest = Path.Combine(
                Path.GetDirectoryName(filePath)!, "processed", Path.GetFileName(filePath));
            File.Move(filePath, dest, overwrite: true);
            System.Console.WriteLine($"  [INBOX] → processed/{Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  [INBOX] BŁĄD: {ex.Message}");
            try
            {
                var failDir  = Path.Combine(Path.GetDirectoryName(filePath)!, "failed");
                var failDest = Path.Combine(failDir, Path.GetFileName(filePath));
                File.Move(filePath, failDest, overwrite: true);
                await File.WriteAllTextAsync(failDest + ".error.txt",
                    $"Błąd: {ex.Message}\n{ex.StackTrace}");
                System.Console.WriteLine($"  [INBOX] → failed/{Path.GetFileName(filePath)}");
            }
            catch { /* nie możemy nic więcej zrobić */ }
        }
        finally
        {
            lock (_lockSet)
                _inProgress.Remove(filePath);
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _semaphore.Dispose();
    }
}
