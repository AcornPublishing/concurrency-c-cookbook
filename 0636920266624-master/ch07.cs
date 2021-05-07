using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Reactive.Testing;
using Nito.AsyncEx;
using static AssertEx;

class TestMethodAttribute: Attribute { }

class Sut
{
  public Task<bool> MyMethodAsync() => Task.FromResult(true);
  public async void MyVoidMethodAsync() { }
}

class ch07r01A
{
  [TestMethod]
  public async Task MyMethodAsync_ReturnsFalse()
  {
    var objectUnderTest = new Sut(); // ...;
    bool result = await objectUnderTest.MyMethodAsync();
    Assert.IsFalse(result);
  }
}

class ch07r01B
{
  [TestMethod]
  public void MyMethodAsync_ReturnsFalse()
  {
    AsyncContext.Run(async () =>
    {
      var objectUnderTest = new Sut(); // ...;
      bool result = await objectUnderTest.MyMethodAsync();
      Assert.IsFalse(result);
    });
  }
}

class ch07r01C
{
  interface IMyInterface
  {
    Task<int> SomethingAsync();
  }

  class SynchronousSuccess : IMyInterface
  {
    public Task<int> SomethingAsync()
    {
      return Task.FromResult(13);
    }
  }

  class SynchronousError : IMyInterface
  {
    public Task<int> SomethingAsync()
    {
      return Task.FromException<int>(new InvalidOperationException());
    }
  }

  class AsynchronousSuccess : IMyInterface
  {
    public async Task<int> SomethingAsync()
    {
      await Task.Yield(); // force asynchronous behavior
      return 13;
    }
  }
}

class ch07r03
{
  // Not a recommended solution; see the rest of this section.
  [TestMethod]
  public void MyMethodAsync_DoesNotThrow()
  {
    AsyncContext.Run(() =>
    {
      var objectUnderTest = new Sut(); // ...;
      objectUnderTest.MyVoidMethodAsync();
    });
  }
}

class ch07r04A
{
  static TransformBlock<int, int> CreateMyCustomBlock() => new TransformBlock<int, int>(x => x);

  [TestMethod]
  public async Task MyCustomBlock_AddsOneToDataItems()
  {
    var myCustomBlock = CreateMyCustomBlock();

    myCustomBlock.Post(3);
    myCustomBlock.Post(13);
    myCustomBlock.Complete();

    Assert.AreEqual(4, myCustomBlock.Receive());
    Assert.AreEqual(14, myCustomBlock.Receive());
    await myCustomBlock.Completion;
  }
}

class ch07r04B
{
  static TransformBlock<int, int> CreateMyCustomBlock() => new TransformBlock<int, int>(x => x);

  [TestMethod]
  public async Task MyCustomBlock_Fault_DiscardsDataAndFaults()
  {
    var myCustomBlock = CreateMyCustomBlock();

    myCustomBlock.Post(3);
    myCustomBlock.Post(13);
    (myCustomBlock as IDataflowBlock).Fault(new InvalidOperationException());

    try
    {
      await myCustomBlock.Completion;
    }
    catch (AggregateException ex)
    {
      AssertExceptionIs<InvalidOperationException>(
          ex.Flatten().InnerException, false);
    }
  }

  public static void AssertExceptionIs<TException>(Exception ex,
      bool allowDerivedTypes = true)
  {
    if (allowDerivedTypes && !(ex is TException))
      Assert.Fail($"Exception is of type {ex.GetType().Name}, but " +
          $"{typeof(TException).Name} or a derived type was expected.");
    if (!allowDerivedTypes && ex.GetType() != typeof(TException))
      Assert.Fail($"Exception is of type {ex.GetType().Name}, but " +
          $"{typeof(TException).Name} was expected.");
  }
}

class ch07r05A
{
  public interface IHttpService
  {
    IObservable<string> GetString(string url);
  }

  public class MyTimeoutClass
  {
    private readonly IHttpService _httpService;

    public MyTimeoutClass(IHttpService httpService)
    {
      _httpService = httpService;
    }

    public IObservable<string> GetStringWithTimeout(string url)
    {
      return _httpService.GetString(url)
          .Timeout(TimeSpan.FromSeconds(1));
    }
  }

  class SuccessHttpServiceStub : IHttpService
  {
    public IObservable<string> GetString(string url)
    {
      return Observable.Return("stub");
    }
  }

  [TestMethod]
  public async Task MyTimeoutClass_SuccessfulGet_ReturnsResult()
  {
    var stub = new SuccessHttpServiceStub();
    var my = new MyTimeoutClass(stub);

    var result = await my.GetStringWithTimeout("http://www.example.com/")
        .SingleAsync();

    Assert.AreEqual("stub", result);
  }

  private class FailureHttpServiceStub : IHttpService
  {
    public IObservable<string> GetString(string url)
    {
      return Observable.Throw<string>(new HttpRequestException());
    }
  }

  [TestMethod]
  public async Task MyTimeoutClass_FailedGet_PropagatesFailure()
  {
    var stub = new FailureHttpServiceStub();
    var my = new MyTimeoutClass(stub);

    await ThrowsAsync<HttpRequestException>(async () =>
    {
      await my.GetStringWithTimeout("http://www.example.com/")
          .SingleAsync();
    });
  }
}

class ch07r06A
{
  public interface IHttpService
  {
    IObservable<string> GetString(string url);
  }

  public class MyTimeoutClass
  {
    private readonly IHttpService _httpService;

    public MyTimeoutClass(IHttpService httpService)
    {
      _httpService = httpService;
    }

    public IObservable<string> GetStringWithTimeout(string url,
        IScheduler scheduler = null)
    {
      return _httpService.GetString(url)
          .Timeout(TimeSpan.FromSeconds(1), scheduler ?? Scheduler.Default);
    }
  }

  private class SuccessHttpServiceStub : IHttpService
  {
    public IScheduler Scheduler { get; set; }
    public TimeSpan Delay { get; set; }

    public IObservable<string> GetString(string url)
    {
      return Observable.Return("stub")
          .Delay(Delay, Scheduler);
    }
  }

  [TestMethod]
  public void MyTimeoutClass_SuccessfulGetShortDelay_ReturnsResult()
  {
    var scheduler = new TestScheduler();
    var stub = new SuccessHttpServiceStub
    {
      Scheduler = scheduler,
      Delay = TimeSpan.FromSeconds(0.5),
    };
    var my = new MyTimeoutClass(stub);
    string result = null;

    my.GetStringWithTimeout("http://www.example.com/", scheduler)
        .Subscribe(r => { result = r; });

    scheduler.Start();

    Assert.AreEqual("stub", result);
  }

  [TestMethod]
  public void MyTimeoutClass_SuccessfulGetLongDelay_ThrowsTimeoutException()
  {
    var scheduler = new TestScheduler();
    var stub = new SuccessHttpServiceStub
    {
      Scheduler = scheduler,
      Delay = TimeSpan.FromSeconds(1.5),
    };
    var my = new MyTimeoutClass(stub);
    Exception result = null;

    my.GetStringWithTimeout("http://www.example.com/", scheduler)
        .Subscribe(_ => Assert.Fail("Received value"), ex => { result = ex; });

    scheduler.Start();

    Assert.IsInstanceOfType(result, typeof(TimeoutException));
  }
}
