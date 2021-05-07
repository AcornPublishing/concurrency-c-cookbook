using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

class ch01_01
{
  async Task DoSomethingAsync()
  {
    int value = 13;

    // Asynchronously wait 1 second.
    await Task.Delay(TimeSpan.FromSeconds(1));

    value *= 2;

    // Asynchronously wait 1 second.
    await Task.Delay(TimeSpan.FromSeconds(1));

    Trace.WriteLine(value);
  }
}

abstract class ch01_02
{
  async Task DoSomethingAsync()
  {
    int value = 13;

    // Asynchronously wait 1 second.
    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

    value *= 2;

    // Asynchronously wait 1 second.
    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

    Trace.WriteLine(value);
  }
}

abstract class ch01_03
{
  public abstract Task PossibleExceptionAsync();
  public abstract void LogException(Exception ex);

  async Task TrySomethingAsync()
  {
    try
    {
      await PossibleExceptionAsync();
    }
    catch (NotSupportedException ex)
    {
      LogException(ex);
      throw;
    }
  }
}

abstract class ch01_04
{
  public abstract Task PossibleExceptionAsync();
  public abstract void LogException(Exception ex);

  async Task TrySomethingAsync()
  {
    // The exception will end up on the Task, not thrown directly.
    Task task = PossibleExceptionAsync();

    try
    {
      // The Task's exception will be raised here, at the await.
      await task;
    }
    catch (NotSupportedException ex)
    {
      LogException(ex);
      throw;
    }
  }
}

class ch01_05
{
  async Task WaitAsync()
  {
    // This await will capture the current context ...
    await Task.Delay(TimeSpan.FromSeconds(1));
    // ... and will attempt to resume the method here in that context.
  }

  void Deadlock()
  {
    // Start the delay.
    Task task = WaitAsync();

    // Synchronously block, waiting for the async method to complete.
    task.Wait();
  }
}

class ch01_06
{
  abstract class Matrix
  {
    public abstract void Rotate(float degrees);
  }

  void RotateMatrices(IEnumerable<Matrix> matrices, float degrees)
  {
    Parallel.ForEach(matrices, matrix => matrix.Rotate(degrees));
  }
}

abstract class ch01_07
{
  public abstract bool IsPrime(int value);

  IEnumerable<bool> PrimalityTest(IEnumerable<int> values)
  {
    return values.AsParallel().Select(value => IsPrime(value));
  }
}

class ch01_08
{
  void ProcessArray(double[] array)
  {
    Parallel.Invoke(
        () => ProcessPartialArray(array, 0, array.Length / 2),
        () => ProcessPartialArray(array, array.Length / 2, array.Length)
    );
  }

  void ProcessPartialArray(double[] array, int begin, int end)
  {
    // CPU-intensive processing...
  }
}

class ch01_09
{
  void Test()
  {
    try
    {
      Parallel.Invoke(() => { throw new Exception(); },
          () => { throw new Exception(); });
    }
    catch (AggregateException ex)
    {
      ex.Handle(exception =>
      {
        Trace.WriteLine(exception);
        return true; // "handled"
      });
    }
  }
}

class ch01_10
{
  void Test()
  {
    Observable.Interval(TimeSpan.FromSeconds(1))
        .Timestamp()
        .Where(x => x.Value % 2 == 0)
        .Select(x => x.Timestamp)
        .Subscribe(x => Trace.WriteLine(x));
  }

  void Test2()
  {
    IObservable<DateTimeOffset> timestamps =
        Observable.Interval(TimeSpan.FromSeconds(1))
            .Timestamp()
            .Where(x => x.Value % 2 == 0)
            .Select(x => x.Timestamp);
    timestamps.Subscribe(x => Trace.WriteLine(x));
  }

  void Test3()
  {
    Observable.Interval(TimeSpan.FromSeconds(1))
        .Timestamp()
        .Where(x => x.Value % 2 == 0)
        .Select(x => x.Timestamp)
        .Subscribe(x => Trace.WriteLine(x),
            ex => Trace.WriteLine(ex));
  }
}

class ch01_11
{
  void Test()
  {
    try
    {
      var multiplyBlock = new TransformBlock<int, int>(item =>
      {
        if (item == 1)
          throw new InvalidOperationException("Blech.");
        return item * 2;
      });
      var subtractBlock = new TransformBlock<int, int>(item => item - 2);
      multiplyBlock.LinkTo(subtractBlock,
          new DataflowLinkOptions { PropagateCompletion = true });

      multiplyBlock.Post(1);
      subtractBlock.Completion.Wait();
    }
    catch (AggregateException exception)
    {
      AggregateException ex = exception.Flatten();
      Trace.WriteLine(ex.InnerException);
    }
  }
}

