using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using Nito;
using Nito.AsyncEx;
using Nito.Mvvm;

class ch14r01A
{
  static int _simpleValue;
  static readonly Lazy<int> MySharedInteger = new Lazy<int>(() => _simpleValue++);

  void UseSharedInteger()
  {
    int sharedValue = MySharedInteger.Value;
  }
}

class ch14r01B
{
  static int _simpleValue;
  static readonly Lazy<Task<int>> MySharedAsyncInteger =
      new Lazy<Task<int>>(async () =>
      {
        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        return _simpleValue++;
      });

  async Task GetSharedIntegerAsync()
  {
    int sharedValue = await MySharedAsyncInteger.Value;
  }
}

class ch14r01C
{
  static int _simpleValue;
  static readonly Lazy<Task<int>> MySharedAsyncInteger =
    new Lazy<Task<int>>(() => Task.Run(async () =>
    {
      await Task.Delay(TimeSpan.FromSeconds(2));
      return _simpleValue++;
    }));

  async Task GetSharedIntegerAsync()
  {
    int sharedValue = await MySharedAsyncInteger.Value;
  }
}

class ch14r01D
{
  public sealed class AsyncLazy<T>
  {
    private readonly object _mutex;
    private readonly Func<Task<T>> _factory;
    private Lazy<Task<T>> _instance;

    public AsyncLazy(Func<Task<T>> factory)
    {
      _mutex = new object();
      _factory = RetryOnFailure(factory);
      _instance = new Lazy<Task<T>>(_factory);
    }

    private Func<Task<T>> RetryOnFailure(Func<Task<T>> factory)
    {
      return async () =>
      {
        try
        {
          return await factory().ConfigureAwait(false);
        }
        catch
        {
          lock (_mutex)
          {
            _instance = new Lazy<Task<T>>(_factory);
          }
          throw;
        }
      };
    }

    public Task<T> Task
    {
      get
      {
        lock (_mutex)
          return _instance.Value;
      }
    }
  }

  static int _simpleValue;
  static readonly AsyncLazy<int> MySharedAsyncInteger =
    new AsyncLazy<int>(() => Task.Run(async () =>
    {
      await Task.Delay(TimeSpan.FromSeconds(2));
      return _simpleValue++;
    }));

  async Task GetSharedIntegerAsync()
  {
    int sharedValue = await MySharedAsyncInteger.Task;
  }
}

class ch14r01E
{
  static int _simpleValue;
  private static readonly AsyncLazy<int> MySharedAsyncInteger =
    new AsyncLazy<int>(async () =>
    {
      await Task.Delay(TimeSpan.FromSeconds(2));
      return _simpleValue++;
    },
    AsyncLazyFlags.RetryOnFailure);

  public async Task UseSharedIntegerAsync()
  {
    int sharedValue = await MySharedAsyncInteger;
  }
}

class ch14r02A
{
  void SubscribeWithDefer()
  {
    var invokeServerObservable = Observable.Defer(
        () => GetValueAsync().ToObservable());
    invokeServerObservable.Subscribe(_ => { });
    invokeServerObservable.Subscribe(_ => { });

    Console.ReadKey();
  }

  async Task<int> GetValueAsync()
  {
    Console.WriteLine("Calling server...");
    await Task.Delay(TimeSpan.FromSeconds(2));
    Console.WriteLine("Returning result...");
    return 13;
  }
}

class ch14r03A
{
  class MyViewModel
  {
    public MyViewModel()
    {
      MyValue = NotifyTask.Create(CalculateMyValueAsync());
    }

    public NotifyTask<int> MyValue { get; private set; }

    private async Task<int> CalculateMyValueAsync()
    {
      await Task.Delay(TimeSpan.FromSeconds(10));
      return 13;
    }
  }
}

class ch14r03B
{
  class BindableTask<T> : INotifyPropertyChanged
  {
    private readonly Task<T> _task;

    public BindableTask(Task<T> task)
    {
      _task = task;
      var _ = WatchTaskAsync();
    }

    private async Task WatchTaskAsync()
    {
      try
      {
        await _task;
      }
      catch
      {
      }

      OnPropertyChanged("IsNotCompleted");
      OnPropertyChanged("IsSuccessfullyCompleted");
      OnPropertyChanged("IsFaulted");
      OnPropertyChanged("Result");
    }

    public bool IsNotCompleted { get { return !_task.IsCompleted; } }
    public bool IsSuccessfullyCompleted
    {
      get { return _task.Status == TaskStatus.RanToCompletion; }
    }
    public bool IsFaulted { get { return _task.IsFaulted; } }
    public T Result
    {
      get { return IsSuccessfullyCompleted ? _task.Result : default; }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}

class ch14r04
{
  private static AsyncLocal<Guid> _operationId = new AsyncLocal<Guid>();

  async Task DoLongOperationAsync()
  {
    _operationId.Value = Guid.NewGuid();

    await DoSomeStepOfOperationAsync();
  }

  async Task DoSomeStepOfOperationAsync()
  {
    await Task.Delay(100); // some async work

    // Do some logging here.
    Trace.WriteLine("In operation: " + _operationId.Value);
  }
}

class ch14r04B
{
  internal sealed class AsyncLocalGuidStack
  {
    private readonly AsyncLocal<ImmutableStack<Guid>> _operationIds =
        new AsyncLocal<ImmutableStack<Guid>>();

    private ImmutableStack<Guid> Current =>
        _operationIds.Value ?? ImmutableStack<Guid>.Empty;

    public IDisposable Push(Guid value)
    {
      _operationIds.Value = Current.Push(value);
      return new PopWhenDisposed(this);
    }

    private void Pop()
    {
      ImmutableStack<Guid> newValue = Current.Pop();
      if (newValue.IsEmpty)
        newValue = null;
      _operationIds.Value = newValue;
    }

    public IEnumerable<Guid> Values => Current;

    private sealed class PopWhenDisposed : IDisposable
    {
      private AsyncLocalGuidStack _stack;

      public PopWhenDisposed(AsyncLocalGuidStack stack) =>
          _stack = stack;

      public void Dispose()
      {
        _stack?.Pop();
        _stack = null;
      }
    }
  }

  private static AsyncLocalGuidStack _operationIds = new AsyncLocalGuidStack();

  async Task DoLongOperationAsync()
  {
    using (_operationIds.Push(Guid.NewGuid()))
      await DoSomeStepOfOperationAsync();
  }

  async Task DoSomeStepOfOperationAsync()
  {
    await Task.Delay(100); // some async work

    // Do some logging here.
    Trace.WriteLine("In operation: " +
        string.Join(":", _operationIds.Values));
  }
}

class ch14r05
{
  private async Task<int> DelayAndReturnCore(bool sync)
  {
    int value = 100;

    // Do some work
    if (sync)
      Thread.Sleep(value); // call synchronous API
    else
      await Task.Delay(value); // call asynchronous API

    return value;
  }

  // Asynchronous API
  public Task<int> DelayAndReturnAsync() =>
      DelayAndReturnCore(sync: false);

  // Synchronous API
  public int DelayAndReturn() =>
      DelayAndReturnCore(sync: true).GetAwaiter().GetResult();
}

class ch14r06
{
  private static TransformBlock<Try<TInput>, Try<TOutput>>
      RailwayTransform<TInput, TOutput>(Func<TInput, TOutput> func)
  {
    return new TransformBlock<Try<TInput>, Try<TOutput>>(t => t.Map(func));
  }

  async Task Test()
  {
    var subtractBlock = RailwayTransform<int, int>(value => value - 2);
    var divideBlock = RailwayTransform<int, int>(value => 60 / value);
    var multiplyBlock = RailwayTransform<int, int>(value => value * 2);

    var options = new DataflowLinkOptions { PropagateCompletion = true };
    subtractBlock.LinkTo(divideBlock, options);
    divideBlock.LinkTo(multiplyBlock, options);

    // Insert data items into the first block
    subtractBlock.Post(Try.FromValue(5));
    subtractBlock.Post(Try.FromValue(2));
    subtractBlock.Post(Try.FromValue(4));
    subtractBlock.Complete();

    // Receive data/exception items from the last block
    while (await multiplyBlock.OutputAvailableAsync())
    {
      Try<int> item = await multiplyBlock.ReceiveAsync();
      if (item.IsValue)
        Console.WriteLine(item.Value);
      else
        Console.WriteLine(item.Exception.Message);
    }
  }
}

class ch14r07: Window
{
  private Label MyLabel;

  private string Solve(IProgress<int> progress)
  {
    // Count as quickly as possible for 3 seconds.
    var endTime = DateTime.UtcNow.AddSeconds(3);
    int value = 0;
    while (DateTime.UtcNow < endTime)
    {
      value++;
      progress?.Report(value);
    }
    return value.ToString();
  }

  // For simplicity, this code updates a label directly.
  // In a real-world MVVM application, those assignments
  //  would instead be updating a ViewModel property
  //  which is data-bound to the actual UI.
  private async void StartButton_Click(object sender, RoutedEventArgs e)
  {
    MyLabel.Content = "Starting...";
    var progress = new Progress<int>(value => MyLabel.Content = value);
    var result = await Task.Run(() => Solve(progress));
    MyLabel.Content = $"Done! Result: {result}";
  }
}

class ch14r07B: Window
{
  private Label MyLabel;
  private string Solve(IProgress<int> progress)
  {
    // Count as quickly as possible for 3 seconds.
    var endTime = DateTime.UtcNow.AddSeconds(3);
    int value = 0;
    while (DateTime.UtcNow < endTime)
    {
      value++;
      progress?.Report(value);
    }
    return value.ToString();
  }



  public static class ObservableProgress
  {
    private sealed class EventProgress<T> : IProgress<T>
    {
      void IProgress<T>.Report(T value) => OnReport?.Invoke(value);
      public event Action<T> OnReport;
    }

    public static (IObservable<T>, IProgress<T>) Create<T>()
    {
      var progress = new EventProgress<T>();
      var observable = Observable.FromEvent<T>(
          handler => progress.OnReport += handler,
          handler => progress.OnReport -= handler);
      return (observable, progress);
    }

    // Note: this must be called from the UI thread.
    public static (IObservable<T>, IProgress<T>) CreateForUi<T>(TimeSpan? sampleInterval = null)
    {
      var (observable, progress) = Create<T>();
      observable = observable.Sample(sampleInterval ?? TimeSpan.FromMilliseconds(100))
          .ObserveOn(SynchronizationContext.Current);
      return (observable, progress);
    }
  }

  // For simplicity, this code updates a label directly.
  // In a real-world MVVM application, those assignments
  //  would instead be updating a ViewModel property
  //  which is data-bound to the actual UI.
  private async void StartButton_Click(object sender, RoutedEventArgs e)
  {
    MyLabel.Content = "Starting...";
    var (observable, progress) = ObservableProgress.CreateForUi<int>();
    string result;
    using (observable.Subscribe(value => MyLabel.Content = value))
      result = await Task.Run(() => Solve(progress));
    MyLabel.Content = $"Done! Result: {result}";
  }
}
