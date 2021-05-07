using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Nito.AsyncEx;

class ch09r01A
{
  void Test()
  {
    ImmutableStack<int> stack = ImmutableStack<int>.Empty;
    stack = stack.Push(13);
    stack = stack.Push(7);

    // Displays "7" followed by "13".
    foreach (int item in stack)
      Trace.WriteLine(item);

    int lastItem;
    stack = stack.Pop(out lastItem);
    // lastItem == 7
  }
}

class ch09r01B
{
  void Test()
  {
    ImmutableStack<int> stack = ImmutableStack<int>.Empty;
    stack = stack.Push(13);
    ImmutableStack<int> biggerStack = stack.Push(7);

    // Displays "7" followed by "13".
    foreach (int item in biggerStack)
      Trace.WriteLine(item);

    // Only displays "13".
    foreach (int item in stack)
      Trace.WriteLine(item);
  }
}

class ch09r01C
{
  void Test()
  {
    ImmutableQueue<int> queue = ImmutableQueue<int>.Empty;
    queue = queue.Enqueue(13);
    queue = queue.Enqueue(7);

    // Displays "13" followed by "7".
    foreach (int item in queue)
      Trace.WriteLine(item);

    int nextItem;
    queue = queue.Dequeue(out nextItem);
    // Displays "13"
    Trace.WriteLine(nextItem);
  }
}

class ch09r02A
{
  void Test()
  {
    ImmutableList<int> list = ImmutableList<int>.Empty;
    list = list.Insert(0, 13);
    list = list.Insert(0, 7);

    // Displays "7" followed by "13".
    foreach (int item in list)
      Trace.WriteLine(item);

    list = list.RemoveAt(1);
  }
}

class ch09r02V
{
  void Test(ImmutableList<int> list)
  {
    // The best way to iterate over an ImmutableList<T>
    foreach (var item in list)
      Trace.WriteLine(item);

    // This will also work, but it will be much slower.
    for (int i = 0; i != list.Count; ++i)
      Trace.WriteLine(list[i]);
  }
}

class ch09r03A
{
  void Test()
  {
    ImmutableHashSet<int> hashSet = ImmutableHashSet<int>.Empty;
    hashSet = hashSet.Add(13);
    hashSet = hashSet.Add(7);

    // Displays "7" and "13" in an unpredictable order.
    foreach (int item in hashSet)
      Trace.WriteLine(item);

    hashSet = hashSet.Remove(7);
  }
}

class ch09r03B
{
  void Test()
  {
    ImmutableSortedSet<int> sortedSet = ImmutableSortedSet<int>.Empty;
    sortedSet = sortedSet.Add(13);
    sortedSet = sortedSet.Add(7);

    // Displays "7" followed by "13".
    foreach (int item in sortedSet)
      Trace.WriteLine(item);
    int smallestItem = sortedSet[0];
    // smallestItem == 7

    sortedSet = sortedSet.Remove(7);
  }
}

class ch09r04A
{
  void Test()
  {
    ImmutableDictionary<int, string> dictionary = ImmutableDictionary<int, string>.Empty;
    dictionary = dictionary.Add(10, "Ten");
    dictionary = dictionary.Add(21, "Twenty-One");
    dictionary = dictionary.SetItem(10, "Diez");

    // Displays "10Diez" and "21Twenty-One" in an unpredictable order.
    foreach (KeyValuePair<int, string> item in dictionary)
      Trace.WriteLine(item.Key + item.Value);

    string ten = dictionary[10];
    // ten == "Diez"

    dictionary = dictionary.Remove(21);
  }
}

class ch09r04B
{
  void Test()
  {
    ImmutableSortedDictionary<int, string> sortedDictionary = ImmutableSortedDictionary<int, string>.Empty;
    sortedDictionary = sortedDictionary.Add(10, "Ten");
    sortedDictionary = sortedDictionary.Add(21, "Twenty-One");
    sortedDictionary = sortedDictionary.SetItem(10, "Diez");

    // Displays "10Diez" followed by "21Twenty-One".
    foreach (KeyValuePair<int, string> item in sortedDictionary)
      Trace.WriteLine(item.Key + item.Value);

    string ten = sortedDictionary[10];
    // ten == "Diez"

    sortedDictionary = sortedDictionary.Remove(21);
  }
}

class ch09r05A
{
  void Test()
  {
    var dictionary = new ConcurrentDictionary<int, string>();
    string newValue = dictionary.AddOrUpdate(0,
        key => "Zero",
        (key, oldValue) => "Zero");


    // Using the same "dictionary" as above.
    // Adds (or updates) key 0 to have the value "Zero".
    dictionary[0] = "Zero";


    // Using the same "dictionary" as above.
    bool keyExists = dictionary.TryGetValue(0, out string currentValue);


    // Using the same "dictionary" as above.
    bool keyExisted = dictionary.TryRemove(0, out string removedValue);
  }
}

class ch09r06A
{
  private readonly BlockingCollection<int> _blockingQueue =
      new BlockingCollection<int>();

  void Test1()
  {
    _blockingQueue.Add(7);
    _blockingQueue.Add(13);
    _blockingQueue.CompleteAdding();
  }

  void Test2()
  {
    // Displays "7" followed by "13".
    foreach (int item in _blockingQueue.GetConsumingEnumerable())
      Trace.WriteLine(item);
  }
}

class ch09r07A
{
  void Test()
  {
    BlockingCollection<int> _blockingStack = new BlockingCollection<int>(
        new ConcurrentStack<int>());
    BlockingCollection<int> _blockingBag = new BlockingCollection<int>(
        new ConcurrentBag<int>());



    // Producer code
    _blockingStack.Add(7);
    _blockingStack.Add(13);
    _blockingStack.CompleteAdding();

    // Consumer code
    // Displays "13" followed by "7".
    foreach (int item in _blockingStack.GetConsumingEnumerable())
      Trace.WriteLine(item);
  }
}

class ch09r08A
{
  async Task Test()
  {
    Channel<int> queue = Channel.CreateUnbounded<int>();

    // Producer code
    ChannelWriter<int> writer = queue.Writer;
    await writer.WriteAsync(7);
    await writer.WriteAsync(13);
    writer.Complete();

    // Consumer code
    // Displays "7" followed by "13".
    ChannelReader<int> reader = queue.Reader;
    await foreach (int value in reader.ReadAllAsync())
      Trace.WriteLine(value);
  }

  async Task Test2()
  {
    Channel<int> queue = Channel.CreateUnbounded<int>();



    // Consumer code (older platforms)
    // Displays "7" followed by "13".
    ChannelReader<int> reader = queue.Reader;
    while (await reader.WaitToReadAsync())
      while (reader.TryRead(out int value))
        Trace.WriteLine(value);
  }
}

class ch09r08B
{
  async Task Test()
  {
    var _asyncQueue = new BufferBlock<int>();

    // Producer code
    await _asyncQueue.SendAsync(7);
    await _asyncQueue.SendAsync(13);
    _asyncQueue.Complete();

    // Consumer code
    // Displays "7" followed by "13".
    while (await _asyncQueue.OutputAvailableAsync())
      Trace.WriteLine(await _asyncQueue.ReceiveAsync());



    while (true)
    {
      int item;
      try
      {
        item = await _asyncQueue.ReceiveAsync();
      }
      catch (InvalidOperationException)
      {
        break;
      }
      Trace.WriteLine(item);
    }
  }
}

class ch09r08C
{
  async Task Test()
  {
    var _asyncQueue = new AsyncProducerConsumerQueue<int>();

    // Producer code
    await _asyncQueue.EnqueueAsync(7);
    await _asyncQueue.EnqueueAsync(13);
    _asyncQueue.CompleteAdding();

    // Consumer code
    // Displays "7" followed by "13".
    while (await _asyncQueue.OutputAvailableAsync())
      Trace.WriteLine(await _asyncQueue.DequeueAsync());



    while (true)
    {
      int item;
      try
      {
        item = await _asyncQueue.DequeueAsync();
      }
      catch (InvalidOperationException)
      {
        break;
      }
      Trace.WriteLine(item);
    }
  }
}

class ch09r09A
{
  async Task Test()
  {
    Channel<int> queue = Channel.CreateBounded<int>(1);
    ChannelWriter<int> writer = queue.Writer;

    // This Write completes immediately.
    await writer.WriteAsync(7);

    // This Write (asynchronously) waits for the 7 to be removed
    // before it enqueues the 13.
    await writer.WriteAsync(13);

    writer.Complete();
  }
}

class ch09r09B
{
  async Task Test()
  {
    var queue = new BufferBlock<int>(
        new DataflowBlockOptions { BoundedCapacity = 1 });

    // This Send completes immediately.
    await queue.SendAsync(7);

    // This Send (asynchronously) waits for the 7 to be removed
    // before it enqueues the 13.
    await queue.SendAsync(13);

    queue.Complete();
  }
}

class ch09r09C
{
  async Task Test()
  {
    var queue = new AsyncProducerConsumerQueue<int>(maxCount: 1);

    // This Enqueue completes immediately.
    await queue.EnqueueAsync(7);

    // This Enqueue (asynchronously) waits for the 7 to be removed
    // before it enqueues the 13.
    await queue.EnqueueAsync(13);

    queue.CompleteAdding();
  }
}

class ch09r09D
{
  void Test()
  {
    var queue = new BlockingCollection<int>(boundedCapacity: 1);

    // This Add completes immediately.
    queue.Add(7);

    // This Add waits for the 7 to be removed before it adds the 13.
    queue.Add(13);

    queue.CompleteAdding();
  }
}

class ch09r10A
{
  async Task Test()
  {
    Channel<int> queue = Channel.CreateBounded<int>(
        new BoundedChannelOptions(1)
        {
          FullMode = BoundedChannelFullMode.DropOldest,
        });
    ChannelWriter<int> writer = queue.Writer;

    // This Write completes immediately.
    await writer.WriteAsync(7);

    // This Write also completes immediately.
    // The 7 is discarded unless a consumer has already retrieved it.
    await writer.WriteAsync(13);
  }
}

class ch09r10B
{
  async Task Test()
  {
    Channel<int> queue = Channel.CreateBounded<int>(
        new BoundedChannelOptions(1)
        {
          FullMode = BoundedChannelFullMode.DropWrite,
        });
    ChannelWriter<int> writer = queue.Writer;

    // This Write completes immediately.
    await writer.WriteAsync(7);

    // This Write also completes immediately.
    // The 13 is discarded unless a consumer has already retrieved the 7.
    await writer.WriteAsync(13);
  }
}

class ch09r11A
{
  async Task Test()
  {
    var _asyncStack = new AsyncCollection<int>(
        new ConcurrentStack<int>());
    var _asyncBag = new AsyncCollection<int>(
        new ConcurrentBag<int>());


    // Producer code
    await _asyncStack.AddAsync(7);
    await _asyncStack.AddAsync(13);
    _asyncStack.CompleteAdding();

    // Consumer code
    // Displays "13" followed by "7".
    while (await _asyncStack.OutputAvailableAsync())
      Trace.WriteLine(await _asyncStack.TakeAsync());
  }
}

class ch09r11B
{
  async Task Test()
  {
    var _asyncStack = new AsyncCollection<int>(
        new ConcurrentStack<int>(), maxCount: 1);



    // This Add completes immediately.
    await _asyncStack.AddAsync(7);

    // This Add (asynchronously) waits for the 7 to be removed
    // before it enqueues the 13.
    await _asyncStack.AddAsync(13);

    _asyncStack.CompleteAdding();



    while (true)
    {
      int item;
      try
      {
        item = await _asyncStack.TakeAsync();
      }
      catch (InvalidOperationException)
      {
        break;
      }
      Trace.WriteLine(item);
    }
  }
}

class ch09r12A
{
  async Task Test()
  {
    var queue = new BufferBlock<int>();

    // Producer code
    await queue.SendAsync(7);
    await queue.SendAsync(13);
    queue.Complete();

    // Consumer code for a single consumer
    while (await queue.OutputAvailableAsync())
      Trace.WriteLine(await queue.ReceiveAsync());

    // Consumer code for multiple consumers
    while (true)
    {
      int item;
      try
      {
        item = await queue.ReceiveAsync();
      }
      catch (InvalidOperationException)
      {
        break;
      }

      Trace.WriteLine(item);
    }
  }
}

class ch09r12B
{
  void Test()
  {
    var queue = new BufferBlock<int>();

    // Producer code
    queue.Post(7);
    queue.Post(13);
    queue.Complete();

    // Consumer code
    while (true)
    {
      int item;
      try
      {
        item = queue.Receive();
      }
      catch (InvalidOperationException)
      {
        break;
      }

      Trace.WriteLine(item);
    }
  }
}

class ch09r12C
{
  async Task Test()
  {
    var queue = new AsyncProducerConsumerQueue<int>();

    // Asynchronous producer code
    await queue.EnqueueAsync(7);
    await queue.EnqueueAsync(13);

    // Synchronous producer code
    queue.Enqueue(7);
    queue.Enqueue(13);

    queue.CompleteAdding();

    // Asynchronous single consumer code
    while (await queue.OutputAvailableAsync())
      Trace.WriteLine(await queue.DequeueAsync());

    // Asynchronous multi-consumer code
    while (true)
    {
      int item;
      try
      {
        item = await queue.DequeueAsync();
      }
      catch (InvalidOperationException)
      {
        break;
      }
      Trace.WriteLine(item);
    }

    // Synchronous consumer code
    foreach (int item in queue.GetConsumingEnumerable())
      Trace.WriteLine(item);
  }
}

class ch09r12D
{
  void Test()
  {
    Channel<int> queue = Channel.CreateBounded<int>(10);

    // Producer code
    ChannelWriter<int> writer = queue.Writer;
    Task.Run(async () =>
    {
      await writer.WriteAsync(7);
      await writer.WriteAsync(13);
      writer.Complete();
    }).GetAwaiter().GetResult();

    // Consumer code
    ChannelReader<int> reader = queue.Reader;
    Task.Run(async () =>
    {
      while (await reader.WaitToReadAsync())
        while (reader.TryRead(out int value))
          Trace.WriteLine(value);
    }).GetAwaiter().GetResult();
  }
}
