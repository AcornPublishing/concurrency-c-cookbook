using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Nito.AsyncEx;

class ch02r01
{
  async Task<T> DelayResult<T>(T result, TimeSpan delay)
  {
    await Task.Delay(delay);
    return result;
  }

  async Task<string> DownloadStringWithRetries(HttpClient client, string uri)
  {
    // Retry after 1 second, then after 2 seconds, then 4.
    TimeSpan nextDelay = TimeSpan.FromSeconds(1);
    for (int i = 0; i != 3; ++i)
    {
      try
      {
        return await client.GetStringAsync(uri);
      }
      catch
      {
      }

      await Task.Delay(nextDelay);
      nextDelay = nextDelay + nextDelay;
    }

    // Try one last time, allowing the error to propagate.
    return await client.GetStringAsync(uri);
  }

  async Task<string> DownloadStringWithTimeout(HttpClient client, string uri)
  {
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    Task<string> downloadTask = client.GetStringAsync(uri);
    Task timeoutTask = Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);

    Task completedTask = await Task.WhenAny(downloadTask, timeoutTask);
    if (completedTask == timeoutTask)
      return null;
    return await downloadTask;
  }
}

class ch02r02A
{
  interface IMyAsyncInterface
  {
    Task<int> GetValueAsync();
  }

  class MySynchronousImplementation : IMyAsyncInterface
  {
    public Task<int> GetValueAsync()
    {
      return Task.FromResult(13);
    }
  }
}

class ch02r02B
{
  interface IMyAsyncInterface
  {
    Task DoSomethingAsync();
  }

  class MySynchronousImplementation : IMyAsyncInterface
  {
    public Task DoSomethingAsync()
    {
      return Task.CompletedTask;
    }
  }
}

class ch02r02C
{
  Task<T> NotImplementedAsync<T>()
  {
    return Task.FromException<T>(new NotImplementedException());
  }

  Task<int> GetValueAsync(CancellationToken cancellationToken)
  {
    if (cancellationToken.IsCancellationRequested)
      return Task.FromCanceled<int>(cancellationToken);
    return Task.FromResult(13);
  }

  static void DoSomethingSynchronously() { }

  interface IMyAsyncInterface
  {
    Task DoSomethingAsync();
  }

  class MySynchronousImplementation : IMyAsyncInterface
  {
    public Task DoSomethingAsync()
    {
      try
      {
        DoSomethingSynchronously();
        return Task.CompletedTask;
      }
      catch (Exception ex)
      {
        return Task.FromException(ex);
      }
    }
  }

  private static readonly Task<int> zeroTask = Task.FromResult(0);
  Task<int> GetValueAsync()
  {
    return zeroTask;
  }
}

class ch02r03
{
  async Task MyMethodAsync(IProgress<double> progress = null)
  {
    bool done = false;
    double percentComplete = 0;
    while (!done)
    {
      // ...
      progress?.Report(percentComplete);
    }
  }

  async Task CallMyMethodAsync()
  {
    var progress = new Progress<double>();
    progress.ProgressChanged += (sender, args) =>
    {
      // ...
    };
    await MyMethodAsync(progress);
  }
}

class ch02r04
{
  async Task Test()
  {
    Task task1 = Task.Delay(TimeSpan.FromSeconds(1));
    Task task2 = Task.Delay(TimeSpan.FromSeconds(2));
    Task task3 = Task.Delay(TimeSpan.FromSeconds(1));

    await Task.WhenAll(task1, task2, task3);
  }

  async Task Test2()
  {
    Task<int> task1 = Task.FromResult(3);
    Task<int> task2 = Task.FromResult(5);
    Task<int> task3 = Task.FromResult(7);

    int[] results = await Task.WhenAll(task1, task2, task3);

    // "results" contains { 3, 5, 7 }
  }

  async Task<string> DownloadAllAsync(HttpClient client, IEnumerable<string> urls)
  {
    // Define the action to do for each URL.
    var downloads = urls.Select(url => client.GetStringAsync(url));
    // Note that no tasks have actually started yet
    //  because the sequence is not evaluated.

    // Start all URLs downloading simultaneously.
    Task<string>[] downloadTasks = downloads.ToArray();
    // Now the tasks have all started.

    // Asynchronously wait for all downloads to complete.
    string[] htmlPages = await Task.WhenAll(downloadTasks);

    return string.Concat(htmlPages);
  }


  async Task ThrowNotImplementedExceptionAsync()
  {
    throw new NotImplementedException();
  }

  async Task ThrowInvalidOperationExceptionAsync()
  {
    throw new InvalidOperationException();
  }

  async Task ObserveOneExceptionAsync()
  {
    var task1 = ThrowNotImplementedExceptionAsync();
    var task2 = ThrowInvalidOperationExceptionAsync();

    try
    {
      await Task.WhenAll(task1, task2);
    }
    catch (Exception ex)
    {
      // "ex" is either NotImplementedException or InvalidOperationException.
      // ...
    }
  }

  async Task ObserveAllExceptionsAsync()
  {
    var task1 = ThrowNotImplementedExceptionAsync();
    var task2 = ThrowInvalidOperationExceptionAsync();

    Task allTasks = Task.WhenAll(task1, task2);
    try
    {
      await allTasks;
    }
    catch
    {
      AggregateException allExceptions = allTasks.Exception;
      // ...
    }
  }
}

class ch02r05
{
  // Returns the length of data at the first URL to respond.
  async Task<int> FirstRespondingUrlAsync(HttpClient client,
      string urlA, string urlB)
  {
    // Start both downloads concurrently.
    Task<byte[]> downloadTaskA = client.GetByteArrayAsync(urlA);
    Task<byte[]> downloadTaskB = client.GetByteArrayAsync(urlB);

    // Wait for either of the tasks to complete.
    Task<byte[]> completedTask =
        await Task.WhenAny(downloadTaskA, downloadTaskB);

    // Return the length of the data retrieved from that URL.
    byte[] data = await completedTask;
    return data.Length;
  }
}

class ch02r06A
{
  async Task<int> DelayAndReturnAsync(int value)
  {
    await Task.Delay(TimeSpan.FromSeconds(value));
    return value;
  }

  // Currently, this method prints "2", "3", and "1".
  // The desired behavior is for this method to print "1", "2", and "3".
  async Task ProcessTasksAsync()
  {
    // Create a sequence of tasks.
    Task<int> taskA = DelayAndReturnAsync(2);
    Task<int> taskB = DelayAndReturnAsync(3);
    Task<int> taskC = DelayAndReturnAsync(1);
    Task<int>[] tasks = new[] { taskA, taskB, taskC };

    // Await each task in order.
    foreach (Task<int> task in tasks)
    {
      var result = await task;
      Trace.WriteLine(result);
    }
  }
}

class ch02r06B
{
  async Task<int> DelayAndReturnAsync(int value)
  {
    await Task.Delay(TimeSpan.FromSeconds(value));
    return value;
  }

  async Task AwaitAndProcessAsync(Task<int> task)
  {
    int result = await task;
    Trace.WriteLine(result);
  }

  // This method now prints "1", "2", and "3".
  async Task ProcessTasksAsync()
  {
    // Create a sequence of tasks.
    Task<int> taskA = DelayAndReturnAsync(2);
    Task<int> taskB = DelayAndReturnAsync(3);
    Task<int> taskC = DelayAndReturnAsync(1);
    Task<int>[] tasks = new[] { taskA, taskB, taskC };

    IEnumerable<Task> taskQuery =
        from t in tasks select AwaitAndProcessAsync(t);
    Task[] processingTasks = taskQuery.ToArray();

    // Await all processing to complete
    await Task.WhenAll(processingTasks);
  }
}

class ch02r06C
{
  async Task<int> DelayAndReturnAsync(int value)
  {
    await Task.Delay(TimeSpan.FromSeconds(value));
    return value;
  }

  // This method now prints "1", "2", and "3".
  async Task ProcessTasksAsync()
  {
    // Create a sequence of tasks.
    Task<int> taskA = DelayAndReturnAsync(2);
    Task<int> taskB = DelayAndReturnAsync(3);
    Task<int> taskC = DelayAndReturnAsync(1);
    Task<int>[] tasks = new[] { taskA, taskB, taskC };

    Task[] processingTasks = tasks.Select(async t =>
    {
      var result = await t;
      Trace.WriteLine(result);
    }).ToArray();

    // Await all processing to complete
    await Task.WhenAll(processingTasks);
  }
}

class ch02r06D
{
  async Task<int> DelayAndReturnAsync(int value)
  {
    await Task.Delay(TimeSpan.FromSeconds(value));
    return value;
  }

  // This method now prints "1", "2", and "3".
  async Task UseOrderByCompletionAsync()
  {
    // Create a sequence of tasks.
    Task<int> taskA = DelayAndReturnAsync(2);
    Task<int> taskB = DelayAndReturnAsync(3);
    Task<int> taskC = DelayAndReturnAsync(1);
    Task<int>[] tasks = new[] { taskA, taskB, taskC };

    // Await each one as they complete.
    foreach (Task<int> task in tasks.OrderByCompletion())
    {
      int result = await task;
      Trace.WriteLine(result);
    }
  }
}

class ch02r07
{
  async Task ResumeOnContextAsync()
  {
    await Task.Delay(TimeSpan.FromSeconds(1));

    // This method resumes within the same context.
  }

  async Task ResumeWithoutContextAsync()
  {
    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

    // This method discards its context when it resumes.
  }
}

class ch02r08A
{
  async Task ThrowExceptionAsync()
  {
    await Task.Delay(TimeSpan.FromSeconds(1));
    throw new InvalidOperationException("Test");
  }

  async Task TestAsync()
  {
    try
    {
      await ThrowExceptionAsync();
    }
    catch (InvalidOperationException)
    {
    }
  }
}

class ch02r08B
{
  async Task ThrowExceptionAsync()
  {
    await Task.Delay(TimeSpan.FromSeconds(1));
    throw new InvalidOperationException("Test");
  }

  async Task TestAsync()
  {
    // The exception is thrown by the method and placed on the task.
    Task task = ThrowExceptionAsync();
    try
    {
      // The exception is re-raised here, where the task is awaited.
      await task;
    }
    catch (InvalidOperationException)
    {
      // The exception is correctly caught here.
    }
  }
}

class ch02r09A
{
  sealed class MyAsyncCommand : ICommand
  {
    async void ICommand.Execute(object parameter)
    {
      await Execute(parameter);
    }

    public async Task Execute(object parameter)
    {
      // ... // Asynchronous command implementation goes here.
    }

    // ... // Other members (CanExecute, etc)
    public bool CanExecute(object parameter)
    {
      CanExecuteChanged?.Invoke(null, null);
      throw new NotImplementedException();
    }
    public event EventHandler CanExecuteChanged;
  }
}

class ch02r09B
{
  static class Program
  {
    static void Main(string[] args)
    {
      try
      {
        AsyncContext.Run(() => MainAsync(args));
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex);
      }
    }

    // BAD CODE!!!
    // In the real world, do not use async void unless you have to.
    static async void MainAsync(string[] args)
    {
      // ...
    }
  }
}

class ch02r10A
{
  public async ValueTask<int> MethodAsync()
  {
    await Task.Delay(100); // asynchronous work.
    return 13;
  }
}

abstract class ch02r10B
{
  bool CanBehaveSynchronously;

  public ValueTask<int> MethodAsync()
  {
    if (CanBehaveSynchronously)
      return new ValueTask<int>(13);
    return new ValueTask<int>(SlowMethodAsync());
  }

  private Task<int> SlowMethodAsync() => Task.FromResult(13);
}

class ch02r10C
{
  private Func<Task> _disposeLogic;

  public ValueTask DisposeAsync()
  {
    if (_disposeLogic == null)
      return default;

    // Note: this simple example is not threadsafe;
    //  if multiple threads call DisposeAsync,
    //  the logic could run more than once.
    Func<Task> logic = _disposeLogic;
    _disposeLogic = null;
    return new ValueTask(logic());
  }
}

class ch02r11A
{
  ValueTask<int> MethodAsync() => new ValueTask<int>(13);

  async Task ConsumingMethodAsync()
  {
    int value = await MethodAsync();
  }
}

class ch02r11B
{
  ValueTask<int> MethodAsync() => new ValueTask<int>(13);

  async Task ConsumingMethodAsync()
  {
    ValueTask<int> valueTask = MethodAsync();
    // ... // other concurrent work
    int value = await valueTask;
  }
}

class ch02r11C
{
  ValueTask<int> MethodAsync() => new ValueTask<int>(13);

  async Task ConsumingMethodAsync()
  {
    Task<int> task = MethodAsync().AsTask();
    // ... // other concurrent work
    int value = await task;
    int anotherValue = await task;
  }
}

class ch02r11D
{
  ValueTask<int> MethodAsync() => new ValueTask<int>(13);

  async Task ConsumingMethodAsync()
  {
    Task<int> task1 = MethodAsync().AsTask();
    Task<int> task2 = MethodAsync().AsTask();
    int[] results = await Task.WhenAll(task1, task2);
  }
}
