using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class ch04r01
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

class ch04r01B
{
  abstract class Matrix
  {
    public bool IsInvertible => true;
    public abstract void Invert();
  }

  void InvertMatrices(IEnumerable<Matrix> matrices)
  {
    Parallel.ForEach(matrices, (matrix, state) =>
    {
      if (!matrix.IsInvertible)
        state.Stop();
      else
        matrix.Invert();
    });
  }
}

class ch04r01C
{
  abstract class Matrix
  {
    public abstract void Rotate(float degrees);
  }

  void RotateMatrices(IEnumerable<Matrix> matrices, float degrees,
      CancellationToken token)
  {
    Parallel.ForEach(matrices,
        new ParallelOptions { CancellationToken = token },
        matrix => matrix.Rotate(degrees));
  }
}

class ch04r01D
{
  abstract class Matrix
  {
    public bool IsInvertible => true;
    public abstract void Invert();
  }

  // Note: this is not the most efficient implementation.
  // This is just an example of using a lock to protect shared state.
  int InvertMatrices(IEnumerable<Matrix> matrices)
  {
    object mutex = new object();
    int nonInvertibleCount = 0;
    Parallel.ForEach(matrices, matrix =>
    {
      if (matrix.IsInvertible)
      {
        matrix.Invert();
      }
      else
      {
        lock (mutex)
        {
          ++nonInvertibleCount;
        }
      }
    });
    return nonInvertibleCount;
  }
}

class ch04r02
{
  // Note: this is not the most efficient implementation.
  // This is just an example of using a lock to protect shared state.
  int ParallelSum(IEnumerable<int> values)
  {
    object mutex = new object();
    int result = 0;
    Parallel.ForEach(source: values,
        localInit: () => 0,
        body: (item, state, localValue) => localValue + item,
        localFinally: localValue =>
        {
          lock (mutex)
            result += localValue;
        });
    return result;
  }
}

class ch04r02B
{
  int ParallelSum(IEnumerable<int> values)
  {
    return values.AsParallel().Sum();
  }
}

class ch04r02C
{
  int ParallelSum(IEnumerable<int> values)
  {
    return values.AsParallel().Aggregate(
        seed: 0,
        func: (sum, item) => sum + item
    );
  }
}

class ch04r03
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

  void DoAction20Times(Action action)
  {
    Action[] actions = Enumerable.Repeat(action, 20).ToArray();
    Parallel.Invoke(actions);
  }

  void DoAction20Times(Action action, CancellationToken token)
  {
    Action[] actions = Enumerable.Repeat(action, 20).ToArray();
    Parallel.Invoke(new ParallelOptions { CancellationToken = token }, actions);
  }
}

class ch04r04
{
  class Node
  {
    public Node Left { get; }
    public Node Right { get; }
  }

  void DoExpensiveActionOnNode(Node node) { }

  void Traverse(Node current)
  {
    DoExpensiveActionOnNode(current);
    if (current.Left != null)
    {
      Task.Factory.StartNew(
          () => Traverse(current.Left),
          CancellationToken.None,
          TaskCreationOptions.AttachedToParent,
          TaskScheduler.Default);
    }
    if (current.Right != null)
    {
      Task.Factory.StartNew(
          () => Traverse(current.Right),
          CancellationToken.None,
          TaskCreationOptions.AttachedToParent,
          TaskScheduler.Default);
    }
  }

  void ProcessTree(Node root)
  {
    Task task = Task.Factory.StartNew(
        () => Traverse(root),
        CancellationToken.None,
        TaskCreationOptions.None,
        TaskScheduler.Default);
    task.Wait();
  }
}

class ch04r04B
{
  void Test()
  {
    Task task = Task.Factory.StartNew(
        () => Thread.Sleep(TimeSpan.FromSeconds(2)),
        CancellationToken.None,
        TaskCreationOptions.None,
        TaskScheduler.Default);
    Task continuation = task.ContinueWith(
        t => Trace.WriteLine("Task is done"),
        CancellationToken.None,
        TaskContinuationOptions.None,
        TaskScheduler.Default);
    // The "t" argument to the continuation is the same as "task".
  }
}

class ch04r05A
{
  IEnumerable<int> MultiplyBy2(IEnumerable<int> values)
  {
    return values.AsParallel().Select(value => value * 2);
  }
}

class ch04r05B
{
  IEnumerable<int> MultiplyBy2(IEnumerable<int> values)
  {
    return values.AsParallel().AsOrdered().Select(value => value * 2);
  }

  int ParallelSum(IEnumerable<int> values)
  {
    return values.AsParallel().Sum();
  }
}
