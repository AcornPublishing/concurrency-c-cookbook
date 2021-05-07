using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Nito.AsyncEx;

class ch12r00
{
  async Task MyMethodAsync()
  {
    int value = 10;
    await Task.Delay(TimeSpan.FromSeconds(1));
    value = value + 1;
    await Task.Delay(TimeSpan.FromSeconds(1));
    value = value - 1;
    await Task.Delay(TimeSpan.FromSeconds(1));
    Trace.WriteLine(value);
  }
}

class ch12r00B
{
  class SharedData
  {
    public int Value { get; set; }
  }

  async Task ModifyValueAsync(SharedData data)
  {
    await Task.Delay(TimeSpan.FromSeconds(1));
    data.Value = data.Value + 1;
  }

  // WARNING: may require synchronization; see discussion below.
  async Task<int> ModifyValueConcurrentlyAsync()
  {
    var data = new SharedData();

    // Start three concurrent modifications.
    Task task1 = ModifyValueAsync(data);
    Task task2 = ModifyValueAsync(data);
    Task task3 = ModifyValueAsync(data);

    await Task.WhenAll(task1, task2, task3);
    return data.Value;
  }
}

class ch12r00C
{
  private int value;

  async Task ModifyValueAsync()
  {
    await Task.Delay(TimeSpan.FromSeconds(1));
    value = value + 1;
  }

  // WARNING: may require synchronization; see discussion below.
  async Task<int> ModifyValueConcurrentlyAsync()
  {
    // Start three concurrent modifications.
    Task task1 = ModifyValueAsync();
    Task task2 = ModifyValueAsync();
    Task task3 = ModifyValueAsync();

    await Task.WhenAll(task1, task2, task3);

    return value;
  }
}

class ch12r00D
{
  // BAD CODE!!
  async Task<int> SimpleParallelismAsync()
  {
    int value = 0;
    Task task1 = Task.Run(() => { value = value + 1; });
    Task task2 = Task.Run(() => { value = value + 1; });
    Task task3 = Task.Run(() => { value = value + 1; });
    await Task.WhenAll(task1, task2, task3);
    return value;
  }
}

class ch12r00E
{
  void IndependentParallelism(IEnumerable<int> values)
  {
    Parallel.ForEach(values, item => Trace.WriteLine(item));
  }
}

class ch12r00F
{
  // BAD CODE!!
  int ParallelSum(IEnumerable<int> values)
  {
    int result = 0;
    Parallel.ForEach(source: values,
        localInit: () => 0,
        body: (item, state, localValue) => localValue + item,
        localFinally: localValue => { result += localValue; });
    return result;
  }
}

class ch12r00G
{
  async Task<bool> PlayWithStackAsync()
  {
    ImmutableStack<int> stack = ImmutableStack<int>.Empty;

    Task task1 = Task.Run(() => Trace.WriteLine(stack.Push(3).Peek()));
    Task task2 = Task.Run(() => Trace.WriteLine(stack.Push(5).Peek()));
    Task task3 = Task.Run(() => Trace.WriteLine(stack.Push(7).Peek()));
    await Task.WhenAll(task1, task2, task3);

    return stack.IsEmpty; // Always returns true.
  }
}

class ch12r00H
{
  // BAD CODE!!
  async Task<bool> PlayWithStackAsync()
  {
    ImmutableStack<int> stack = ImmutableStack<int>.Empty;

    Task task1 = Task.Run(() => { stack = stack.Push(3); });
    Task task2 = Task.Run(() => { stack = stack.Push(5); });
    Task task3 = Task.Run(() => { stack = stack.Push(7); });
    await Task.WhenAll(task1, task2, task3);

    return stack.IsEmpty;
  }
}

class ch12r00I
{
  async Task<int> ThreadsafeCollectionsAsync()
  {
    var dictionary = new ConcurrentDictionary<int, int>();

    Task task1 = Task.Run(() => { dictionary.TryAdd(2, 3); });
    Task task2 = Task.Run(() => { dictionary.TryAdd(3, 5); });
    Task task3 = Task.Run(() => { dictionary.TryAdd(5, 7); });
    await Task.WhenAll(task1, task2, task3);

    return dictionary.Count; // Always returns 3.
  }
}

class ch12r01
{
  class MyClass
  {
    // This lock protects the _value field.
    private readonly object _mutex = new object();

    private int _value;

    public void Increment()
    {
      lock (_mutex)
      {
        _value = _value + 1;
      }
    }
  }
}

class ch12r02A
{
  class MyClass
  {
    // This lock protects the _value field.
    private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);

    private int _value;

    public async Task DelayAndIncrementAsync()
    {
      await _mutex.WaitAsync();
      try
      {
        int oldValue = _value;
        await Task.Delay(TimeSpan.FromSeconds(oldValue));
        _value = oldValue + 1;
      }
      finally
      {
        _mutex.Release();
      }
    }
  }
}

class ch12r02B
{
  class MyClass
  {
    // This lock protects the _value field.
    private readonly AsyncLock _mutex = new AsyncLock();

    private int _value;

    public async Task DelayAndIncrementAsync()
    {
      using (await _mutex.LockAsync())
      {
        int oldValue = _value;
        await Task.Delay(TimeSpan.FromSeconds(oldValue));
        _value = oldValue + 1;
      }
    }
  }
}

class ch12r03A
{
  class MyClass
  {
    private readonly ManualResetEventSlim _initialized =
        new ManualResetEventSlim();

    private int _value;

    public int WaitForInitialization()
    {
      _initialized.Wait();
      return _value;
    }

    public void InitializeFromAnotherThread()
    {
      _value = 13;
      _initialized.Set();
    }
  }
}

class ch12r04A
{
  class MyClass
  {
    private readonly TaskCompletionSource<object> _initialized =
        new TaskCompletionSource<object>();

    private int _value1;
    private int _value2;

    public async Task<int> WaitForInitializationAsync()
    {
      await _initialized.Task;
      return _value1 + _value2;
    }

    public void Initialize()
    {
      _value1 = 13;
      _value2 = 17;
      _initialized.TrySetResult(null);
    }
  }
}

class ch12r04B
{
  class MyClass
  {
    private readonly AsyncManualResetEvent _connected =
        new AsyncManualResetEvent();

    public async Task WaitForConnectedAsync()
    {
      await _connected.WaitAsync();
    }

    public void ConnectedChanged(bool connected)
    {
      if (connected)
        _connected.Set();
      else
        _connected.Reset();
    }
  }
}

class ch12r05
{
  abstract class Matrix
  {
    public abstract void Rotate(float degrees);
  }



  IPropagatorBlock<int, int> DataflowMultiplyBy2()
  {
    var options = new ExecutionDataflowBlockOptions
    {
      MaxDegreeOfParallelism = 10
    };

    return new TransformBlock<int, int>(data => data * 2, options);
  }

  // Using Parallel LINQ (PLINQ)
  IEnumerable<int> ParallelMultiplyBy2(IEnumerable<int> values)
  {
    return values.AsParallel()
        .WithDegreeOfParallelism(10)
        .Select(item => item * 2);
  }

  // Using the Parallel class
  void ParallelRotateMatrices(IEnumerable<Matrix> matrices, float degrees)
  {
    var options = new ParallelOptions
    {
      MaxDegreeOfParallelism = 10
    };
    Parallel.ForEach(matrices, options, matrix => matrix.Rotate(degrees));
  }


  async Task<string[]> DownloadUrlsAsync(HttpClient client, IEnumerable<string> urls)
  {
    using var semaphore = new SemaphoreSlim(10);
    Task<string>[] tasks = urls.Select(async url =>
    {
      await semaphore.WaitAsync();
      try
      {
        return await client.GetStringAsync(url);
      }
      finally
      {
        semaphore.Release();
      }
    }).ToArray();
    return await Task.WhenAll(tasks);
  }
}
