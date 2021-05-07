using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

class ch11r01
{
  interface IMyAsyncInterface
  {
    Task<int> CountBytesAsync(HttpClient client, string url);
  }

  class MyAsyncClass : IMyAsyncInterface
  {
    public async Task<int> CountBytesAsync(HttpClient client, string url)
    {
      var bytes = await client.GetByteArrayAsync(url);
      return bytes.Length;
    }
  }

  async Task UseMyInterfaceAsync(HttpClient client, IMyAsyncInterface service)
  {
    var result = await service.CountBytesAsync(client, "http://www.example.com");
    Trace.WriteLine(result);
  }

  class MyAsyncClassStub : IMyAsyncInterface
  {
    public Task<int> CountBytesAsync(HttpClient client, string url)
    {
      return Task.FromResult(13);
    }
  }
}

class ch11r02A
{
  class MyAsyncClass
  {
    public Task InitializeAsync() => Task.CompletedTask;
  }


  async Task Test()
  {
    var instance = new MyAsyncClass();
    await instance.InitializeAsync();
  }
}

class ch11r02B
{
  class MyAsyncClass
  {
    private MyAsyncClass()
    {
    }

    private async Task<MyAsyncClass> InitializeAsync()
    {
      await Task.Delay(TimeSpan.FromSeconds(1));
      return this;
    }

    public static Task<MyAsyncClass> CreateAsync()
    {
      var result = new MyAsyncClass();
      return result.InitializeAsync();
    }
  }

  async Task Test()
  {
    MyAsyncClass instance = await MyAsyncClass.CreateAsync();
  }
}

class ch11r02C
{
  class MyAsyncClass
  {
    public MyAsyncClass()
    {
      InitializeAsync();
    }

    // BAD CODE!!
    private async void InitializeAsync()
    {
      await Task.Delay(TimeSpan.FromSeconds(1));
    }
  }
}

class ch11r03A
{
  interface IMyFundamentalType { }
  interface IMyComposedType { }


  /// <summary>
  /// Marks a type as requiring asynchronous initialization 
  /// and provides the result of that initialization.
  /// </summary>
  public interface IAsyncInitialization
  {
    /// <summary>
    /// The result of the asynchronous initialization of this instance.
    /// </summary>
    Task Initialization { get; }
  }

  class MyFundamentalType : IMyFundamentalType, IAsyncInitialization
  {
    public MyFundamentalType()
    {
      Initialization = InitializeAsync();
    }

    public Task Initialization { get; private set; }

    private async Task InitializeAsync()
    {
      // Asynchronously initialize this instance.
      await Task.Delay(TimeSpan.FromSeconds(1));
    }
  }


  async Task Test1()
  {
    IMyFundamentalType instance = new MyFundamentalType(); // UltimateDIFactory.Create<IMyFundamentalType>();
    var instanceAsyncInit = instance as IAsyncInitialization;
    if (instanceAsyncInit != null)
      await instanceAsyncInit.Initialization;
  }


  class MyComposedType : IMyComposedType, IAsyncInitialization
  {
    private readonly IMyFundamentalType _fundamental;

    public MyComposedType(IMyFundamentalType fundamental)
    {
      _fundamental = fundamental;
      Initialization = InitializeAsync();
    }

    public Task Initialization { get; private set; }

    private async Task InitializeAsync()
    {
      // Asynchronously wait for the fundamental instance to initialize,
      //  if necessary.
      var fundamentalAsyncInit = _fundamental as IAsyncInitialization;
      if (fundamentalAsyncInit != null)
        await fundamentalAsyncInit.Initialization;

      // Do our own initialization (synchronous or asynchronous).
      // ...
    }
  }


  public static class AsyncInitialization
  {
    public static Task WhenAllInitializedAsync(params object[] instances)
    {
      return Task.WhenAll(instances
          .OfType<IAsyncInitialization>()
          .Select(x => x.Initialization));
    }
  }



  class MyComposedType2 : IMyComposedType, IAsyncInitialization
  {
    private readonly IMyFundamentalType _fundamental, _anotherType, _yetAnother;

    public MyComposedType2(IMyFundamentalType fundamental)
    {
      _fundamental = fundamental;
      Initialization = InitializeAsync();
    }

    public Task Initialization { get; private set; }

    private async Task InitializeAsync()
    {
      // Asynchronously wait for all 3 instances to initialize, if necessary.
      await AsyncInitialization.WhenAllInitializedAsync(_fundamental,
          _anotherType, _yetAnother);

      // Do our own initialization (synchronous or asynchronous).
      // ...
    }
  }
}

class ch11r04A
{
  // As an asynchronous method.
  public async Task<int> GetDataAsync()
  {
    await Task.Delay(TimeSpan.FromSeconds(1));
    return 13;
  }
}

class ch11r04B
{
  // As a Task-returning property.
  // This API design is questionable.
  public Task<int> Data
  {
    get { return GetDataAsync(); }
  }

  private async Task<int> GetDataAsync()
  {
    await Task.Delay(TimeSpan.FromSeconds(1));
    return 13;
  }
}

class ch11r04C
{
  // As a cached value.
  public AsyncLazy<int> Data
  {
    get { return _data; }
  }

  private readonly AsyncLazy<int> _data =
      new AsyncLazy<int>(async () =>
      {
        await Task.Delay(TimeSpan.FromSeconds(1));
        return 13;
      });


  async Task Test()
  {
    var instance = new ch11r04C();


    int value = await instance.Data;
  }
}


class ch11r04D
{
  private async Task<int> GetDataAsync()
  {
    await Task.Delay(TimeSpan.FromSeconds(1));
    return 13;
  }

  public int Data
  {
    // BAD CODE!!
    get { return GetDataAsync().Result; }
  }
}

class ch11r05
{
  public class MyEventArgs : EventArgs, IDeferralSource
  {
    private readonly DeferralManager _deferrals = new DeferralManager();

    // ... // Your own constructors and properties.

    public IDisposable GetDeferral()
    {
      return _deferrals.DeferralSource.GetDeferral();
    }

    internal Task WaitForDeferralsAsync()
    {
      return _deferrals.WaitForDeferralsAsync();
    }
  }



  public event EventHandler<MyEventArgs> MyEvent;

  private async Task RaiseMyEventAsync()
  {
    EventHandler<MyEventArgs> handler = MyEvent;
    if (handler == null)
      return;

    var args = new MyEventArgs();
    handler(this, args);
    await args.WaitForDeferralsAsync();
  }



  async void AsyncHandler(object sender, MyEventArgs args)
  {
    using IDisposable deferral = args.GetDeferral();
    await Task.Delay(TimeSpan.FromSeconds(2));
  }
}

class ch11r06A
{
  class MyClass : IDisposable
  {
    private readonly CancellationTokenSource _disposeCts =
        new CancellationTokenSource();

    public async Task<int> CalculateValueAsync()
    {
      await Task.Delay(TimeSpan.FromSeconds(2), _disposeCts.Token);
      return 13;
    }

    public void Dispose()
    {
      _disposeCts.Cancel();
    }
  }
}

class ch11r06B
{
  class MyClass : IDisposable
  {
    private readonly CancellationTokenSource _disposeCts =
        new CancellationTokenSource();

    public async Task<int> CalculateValueAsync(CancellationToken cancellationToken)
    {
      using CancellationTokenSource combinedCts = CancellationTokenSource
          .CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
      await Task.Delay(TimeSpan.FromSeconds(2), combinedCts.Token);
      return 13;
    }

    public void Dispose()
    {
      _disposeCts.Cancel();
    }
  }

  async Task UseMyClassAsync()
  {
    Task<int> task;
    using (var resource = new MyClass())
    {
      task = resource.CalculateValueAsync(default);
    }

    // Throws OperationCanceledException.
    var result = await task;
  }
}

class ch11r06C
{
  class MyClass : IAsyncDisposable
  {
    public async ValueTask DisposeAsync()
    {
      await Task.Delay(TimeSpan.FromSeconds(2));
    }
  }

  async Task Test()
  {
    await using (var myClass = new MyClass())
    {
      // ...
    } // DisposeAsync is invoked (and awaited) here
  }

  async Task Test2()
  {
    var myClass = new MyClass();
    await using (myClass.ConfigureAwait(false))
    {
      // ...
    } // DisposeAsync is invoked (and awaited) here with ConfigureAwait(false)
  }
}

