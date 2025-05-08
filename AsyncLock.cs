internal class AsyncLock : IDisposable
{
  private readonly SemaphoreSlim _semaphore = new(1, 1);

  public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
  {
    await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
    return new LockReleaser(_semaphore);
  }

  public void Dispose()
  {
    _semaphore.Dispose();
  }
}

internal class LockReleaser : IDisposable
{
  private readonly SemaphoreSlim _semaphore;
  private bool _disposed;

  public LockReleaser(SemaphoreSlim semaphore)
  {
    _semaphore = semaphore;
  }

  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    _semaphore.Release();
    _disposed = true;
  }
}