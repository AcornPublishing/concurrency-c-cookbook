using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class ch03r01
{
  async IAsyncEnumerable<int> GetValuesAsync()
  {
    await Task.Delay(1000); // some asynchronous work
    yield return 10;
    await Task.Delay(1000); // more asynchronous work
    yield return 13;
  }

  async IAsyncEnumerable<string> GetValuesAsync(HttpClient client)
  {
    int offset = 0;
    const int limit = 10;
    while (true)
    {
      // Get the current page of results and parse them
      string result = await client.GetStringAsync(
          $"https://example.com/api/values?offset={offset}&limit={limit}");
      string[] valuesOnThisPage = result.Split('\n');

      // Produce the results for this page
      foreach (string value in valuesOnThisPage)
        yield return value;

      // If this is the last page, we're done
      if (valuesOnThisPage.Length != limit)
        break;

      // Otherwise, proceed to the next page
      offset += limit;
    }
  }
}

abstract class ch03r02
{
  public abstract

  IAsyncEnumerable<string> GetValuesAsync(HttpClient client);

  public async Task ProcessValueAsync(HttpClient client)
  {
    await foreach (string value in GetValuesAsync(client))
    {
      Console.WriteLine(value);
    }
  }
}

abstract class ch03r02B
{
  public abstract

  IAsyncEnumerable<string> GetValuesAsync(HttpClient client);

  public async Task ProcessValueAsync(HttpClient client)
  {
    await foreach (string value in GetValuesAsync(client))
    {
      await Task.Delay(100); // asynchronous work
      Console.WriteLine(value);
    }
  }
}

abstract class ch03r02C
{
  public abstract


  IAsyncEnumerable<string> GetValuesAsync(HttpClient client);

  public async Task ProcessValueAsync(HttpClient client)
  {
    await foreach (string value in GetValuesAsync(client).ConfigureAwait(false))
    {
      await Task.Delay(100).ConfigureAwait(false); // asynchronous work
      Console.WriteLine(value);
    }
  }
}

class ch03r03
{
  async Task Test()
  {
    IAsyncEnumerable<int> values = SlowRange().WhereAwait(
        async value =>
        {
          // do some asynchronous work to determine
          //  if this element should be included
          await Task.Delay(10);
          return value % 2 == 0;
        });

    await foreach (int result in values)
    {
      Console.WriteLine(result);
    }

    // Produce sequence that slows down as it progresses
    async IAsyncEnumerable<int> SlowRange()
    {
      for (int i = 0; i != 10; ++i)
      {
        await Task.Delay(i * 100);
        yield return i;
      }
    }
  }

  async Task Test2()
  {
    // Produce sequence that slows down as it progresses
    async IAsyncEnumerable<int> SlowRange()
    {
      for (int i = 0; i != 10; ++i)
      {
        await Task.Delay(i * 100);
        yield return i;
      }
    }


    IAsyncEnumerable<int> values = SlowRange().Where(
    value => value % 2 == 0);

    await foreach (int result in values)
    {
      Console.WriteLine(result);
    }
  }

  async Task Test3()
  {
    // Produce sequence that slows down as it progresses
    async IAsyncEnumerable<int> SlowRange()
    {
      for (int i = 0; i != 10; ++i)
      {
        await Task.Delay(i * 100);
        yield return i;
      }
    }


    int count = await SlowRange().CountAsync(
    value => value % 2 == 0);
  }

  async Task Test4()
  {
    // Produce sequence that slows down as it progresses
    async IAsyncEnumerable<int> SlowRange()
    {
      for (int i = 0; i != 10; ++i)
      {
        await Task.Delay(i * 100);
        yield return i;
      }
    }


    int count = await SlowRange().CountAwaitAsync(
        async value =>
        {
          await Task.Delay(10);
          return value % 2 == 0;
        });
  }
}


class ch03r04
{
  async Task Test()
  {
    await foreach (int result in SlowRange())
    {
      Console.WriteLine(result);
      if (result >= 8)
        break;
    }

    // Produce sequence that slows down as it progresses
    async IAsyncEnumerable<int> SlowRange()
    {
      for (int i = 0; i != 10; ++i)
      {
        await Task.Delay(i * 100);
        yield return i;
      }
    }
  }
}

class ch03r04B
{ 
  async Task Test2()
  {
    using var cts = new CancellationTokenSource(500);
    CancellationToken token = cts.Token;
    await foreach (int result in SlowRange(token))
    {
      Console.WriteLine(result);
    }
  }

  // Produce sequence that slows down as it progresses
  async IAsyncEnumerable<int> SlowRange(
      [EnumeratorCancellation] CancellationToken token = default)
  {
    for (int i = 0; i != 10; ++i)
    {
      await Task.Delay(i * 100, token);
      yield return i;
    }
  }
}

class ch03r04C
{
  async Task ConsumeSequence(IAsyncEnumerable<int> items)
  {
    using var cts = new CancellationTokenSource(500);
    CancellationToken token = cts.Token;
    await foreach (int result in items.WithCancellation(token))
    {
      Console.WriteLine(result);
    }
  }

  // Produce sequence that slows down as it progresses
  async IAsyncEnumerable<int> SlowRange(
      [EnumeratorCancellation] CancellationToken token = default)
  {
    for (int i = 0; i != 10; ++i)
    {
      await Task.Delay(i * 100, token);
      yield return i;
    }
  }

  async Task Test() =>
  await ConsumeSequence(SlowRange());
}

class ch03r04D
{
  async Task ConsumeSequence(IAsyncEnumerable<int> items)
  {
    using var cts = new CancellationTokenSource(500);
    CancellationToken token = cts.Token;
    await foreach (int result in items
        .WithCancellation(token).ConfigureAwait(false))
    {
      Console.WriteLine(result);
    }
  }
}
