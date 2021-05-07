using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;

class ch13r01
{
  void Test()
  {
    Task task = Task.Run(() =>
    {
      Thread.Sleep(TimeSpan.FromSeconds(2));
    });
  }

  void Test2()
  {
    Task<int> task = Task.Run(async () =>
    {
      await Task.Delay(TimeSpan.FromSeconds(2));
      return 13;
    });
  }
}

class ch13r02
{
  void Test()
  {
    TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
  }

  void Test2()
  {
    var schedulerPair = new ConcurrentExclusiveSchedulerPair();
    TaskScheduler concurrent = schedulerPair.ConcurrentScheduler;
    TaskScheduler exclusive = schedulerPair.ExclusiveScheduler;
  }

  void Test3()
  {
    var schedulerPair = new ConcurrentExclusiveSchedulerPair(
        TaskScheduler.Default, maxConcurrencyLevel: 8);
    TaskScheduler scheduler = schedulerPair.ConcurrentScheduler;
  }
}

class ch13r03
{
  abstract class Matrix
  {
    public abstract void Rotate(float degrees);
  }

  void RotateMatrices(IEnumerable<IEnumerable<Matrix>> collections, float degrees)
  {
    var schedulerPair = new ConcurrentExclusiveSchedulerPair(
        TaskScheduler.Default, maxConcurrencyLevel: 8);
    TaskScheduler scheduler = schedulerPair.ConcurrentScheduler;
    ParallelOptions options = new ParallelOptions { TaskScheduler = scheduler };
    Parallel.ForEach(collections, options,
        matrices => Parallel.ForEach(matrices, options,
            matrix => matrix.Rotate(degrees)));
  }
}

class ch13r04: Window
{
  private ListBox ListBox;

  void Test()
  {
    var options = new ExecutionDataflowBlockOptions
    {
      TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext(),
    };
    var multiplyBlock = new TransformBlock<int, int>(item => item * 2);
    var displayBlock = new ActionBlock<int>(
        result => ListBox.Items.Add(result), options);
    multiplyBlock.LinkTo(displayBlock);
  }
}
