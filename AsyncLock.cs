internal class AsyncLock : IDisposable
{
  private readonly SemaphoreSlim _semaphore = new(1, 1);

  public async Task LockAsync(CancellationToken cancellationToken = default)
  {
    await _semaphore.WaitAsync(cancellationToken);
  }

  public void Dispose()
  {
    _semaphore.Dispose();
  }
}