using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

static class ch08r01
{
  public static Task<string> DownloadStringTaskAsync(this WebClient client,
      Uri address)
  {
    var tcs = new TaskCompletionSource<string>();

    // The event handler will complete the task and unregister itself.
    DownloadStringCompletedEventHandler handler = null;
    handler = (_, e) =>
    {
      client.DownloadStringCompleted -= handler;
      if (e.Cancelled)
        tcs.TrySetCanceled();
      else if (e.Error != null)
        tcs.TrySetException(e.Error);
      else
        tcs.TrySetResult(e.Result);
    };

    // Register for the event and *then* start the operation.
    client.DownloadStringCompleted += handler;
    client.DownloadStringAsync(address);

    return tcs.Task;
  }
}

static class ch08r02
{
  public static Task<WebResponse> GetResponseAsync(this WebRequest client)
  {
    return Task<WebResponse>.Factory.FromAsync(client.BeginGetResponse,
        client.EndGetResponse, null);
  }
}

static class ch08r03
{
  public interface IMyAsyncHttpService
  {
    void DownloadString(Uri address, Action<string, Exception> callback);
  }

  public static Task<string> DownloadStringAsync(
      this IMyAsyncHttpService httpService, Uri address)
  {
    var tcs = new TaskCompletionSource<string>();
    httpService.DownloadString(address, (result, exception) =>
    {
      if (exception != null)
        tcs.TrySetException(exception);
      else
        tcs.TrySetResult(result);
    });
    return tcs.Task;
  }
}

class ch08r04
{
  async Task Test()
  {
    var source = Enumerable.Range(0, 10);
    Action<int> body = x => { };

    await Task.Run(() => Parallel.ForEach(source, body));
  }
}

class ch08r05
{
  async Task Test()
  {
    IObservable<int> observable = Observable.Range(0, 10); // ...;
    int lastElement = await observable.LastAsync();
    int lastElement2 = await observable;
    int nextElement = await observable.FirstAsync();
    IList<int> allElements = await observable.ToList();
  }
}

class ch08r06A
{
  IObservable<HttpResponseMessage> GetPage(HttpClient client)
  {
    Task<HttpResponseMessage> task =
        client.GetAsync("http://www.example.com/");
    return task.ToObservable();
  }
}

class ch08r06B
{
  IObservable<HttpResponseMessage> GetPage(HttpClient client)
  {
    return Observable.StartAsync(
        token => client.GetAsync("http://www.example.com/", token));
  }
}

class ch08r06C
{
  IObservable<HttpResponseMessage> GetPage(HttpClient client)
  {
    return Observable.FromAsync(
        token => client.GetAsync("http://www.example.com/", token));
  }
}

class ch08r06D
{
  IObservable<HttpResponseMessage> GetPages(
      IObservable<string> urls, HttpClient client)
  {
    return urls.SelectMany(
        (url, token) => client.GetAsync(url, token));
  }
}

class ch08r07A
{
  async Task Test()
  {
    var multiplyBlock = new TransformBlock<int, int>(value => value * 2);

    multiplyBlock.Post(5);
    multiplyBlock.Post(2);
    multiplyBlock.Complete();

    await foreach (int item in multiplyBlock.ReceiveAllAsync())
    {
      Console.WriteLine(item);
    }
  }
}

static class ch08r07B
{
  public static async Task WriteToBlockAsync<T>(this IAsyncEnumerable<T> enumerable,
      ITargetBlock<T> block, CancellationToken token = default)
  {
    try
    {
      await foreach (var item in enumerable.WithCancellation(token).ConfigureAwait(false))
        await block.SendAsync(item, token).ConfigureAwait(false);
      block.Complete();
    }
    catch (Exception ex)
    {
      block.Fault(ex);
    }
  }
}

class ch08r08A
{
  void Test()
  {
    var buffer = new BufferBlock<int>();
    IObservable<int> integers = buffer.AsObservable();
    integers.Subscribe(data => Trace.WriteLine(data),
        ex => Trace.WriteLine(ex),
        () => Trace.WriteLine("Done"));

    buffer.Post(13);
  }
}

class ch08r08B
{
  void Test()
  {
    IObservable<DateTimeOffset> ticks =
        Observable.Interval(TimeSpan.FromSeconds(1))
            .Timestamp()
            .Select(x => x.Timestamp)
            .Take(5);

    var display = new ActionBlock<DateTimeOffset>(x => Trace.WriteLine(x));
    ticks.Subscribe(display.AsObserver());

    try
    {
      display.Completion.Wait();
      Trace.WriteLine("Done.");
    }
    catch (Exception ex)
    {
      Trace.WriteLine(ex);
    }
  }
}

class ch08r09
{
  void Test()
  {
    IObservable<long> observable =
        Observable.Interval(TimeSpan.FromSeconds(1));

    // WARNING: May consume unbounded memory; see discussion!
    IAsyncEnumerable<long> enumerable =
        observable.ToAsyncEnumerable();
  }
}

static class ch08r09B
{
  // WARNING: May consume unbounded memory; see discussion!
  public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
      this IObservable<T> observable)
  {
    Channel<T> buffer = Channel.CreateUnbounded<T>();
    using (observable.Subscribe(
        value => buffer.Writer.TryWrite(value),
        error => buffer.Writer.Complete(error),
        () => buffer.Writer.Complete()))
    {
      await foreach (T item in buffer.Reader.ReadAllAsync())
        yield return item;
    }
  }
}

static class ch08r09C
{
  // WARNING: May discard items; see discussion!
  public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
      this IObservable<T> observable, int bufferSize)
  {
    var bufferOptions = new BoundedChannelOptions(bufferSize)
    {
      FullMode = BoundedChannelFullMode.DropOldest,
    };
    Channel<T> buffer = Channel.CreateBounded<T>(bufferOptions);
    using (observable.Subscribe(
        value => buffer.Writer.TryWrite(value),
        error => buffer.Writer.Complete(error),
        () => buffer.Writer.Complete()))
    {
      await foreach (T item in buffer.Reader.ReadAllAsync())
        yield return item;
    }
  }
}

