using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

class ch05r01
{
  void Test()
  {
    var multiplyBlock = new TransformBlock<int, int>(item => item * 2);
    var subtractBlock = new TransformBlock<int, int>(item => item - 2);

    // After linking, values that exit multiplyBlock will enter subtractBlock.
    multiplyBlock.LinkTo(subtractBlock);
  }

  async Task TestB()
  {
    var multiplyBlock = new TransformBlock<int, int>(item => item * 2);
    var subtractBlock = new TransformBlock<int, int>(item => item - 2);

    var options = new DataflowLinkOptions { PropagateCompletion = true };
    multiplyBlock.LinkTo(subtractBlock, options);

    // ...

    // The first block's completion is automatically propagated to the second block.
    multiplyBlock.Complete();
    await subtractBlock.Completion;
  }
}

class ch05r02
{
  void Test()
  {
    var block = new TransformBlock<int, int>(item =>
    {
      if (item == 1)
        throw new InvalidOperationException("Blech.");
      return item * 2;
    });
    block.Post(1);
    block.Post(2);
  }

  async Task Test2()
  {
    try
    {
      var block = new TransformBlock<int, int>(item =>
      {
        if (item == 1)
          throw new InvalidOperationException("Blech.");
        return item * 2;
      });
      block.Post(1);
      await block.Completion;
    }
    catch (InvalidOperationException)
    {
      // The exception is caught here.
    }
  }

  async Task Test3()
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
      await subtractBlock.Completion;
    }
    catch (AggregateException)
    {
      // The exception is caught here.
    }
  }
}

class ch05r03
{
  void Test()
  {
    var multiplyBlock = new TransformBlock<int, int>(item => item * 2);
    var subtractBlock = new TransformBlock<int, int>(item => item - 2);

    IDisposable link = multiplyBlock.LinkTo(subtractBlock);
    multiplyBlock.Post(1);
    multiplyBlock.Post(2);

    // Unlink the blocks.
    // The data posted above may or may not have already gone through the link.
    // In real-world code, consider a using block rather than calling Dispose.
    link.Dispose();
  }
}

class ch05r04
{
  void Test()
  {
    var sourceBlock = new BufferBlock<int>();
    var options = new DataflowBlockOptions { BoundedCapacity = 1 };
    var targetBlockA = new BufferBlock<int>(options);
    var targetBlockB = new BufferBlock<int>(options);

    sourceBlock.LinkTo(targetBlockA);
    sourceBlock.LinkTo(targetBlockB);
  }
}

class ch05r05
{
  void Test()
  {
    var multiplyBlock = new TransformBlock<int, int>(
        item => item * 2,
        new ExecutionDataflowBlockOptions
        {
          MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
        });
    var subtractBlock = new TransformBlock<int, int>(item => item - 2);
    multiplyBlock.LinkTo(subtractBlock);
  }
}

class ch05r06
{
  IPropagatorBlock<int, int> CreateMyCustomBlock()
  {
    var multiplyBlock = new TransformBlock<int, int>(item => item * 2);
    var addBlock = new TransformBlock<int, int>(item => item + 2);
    var divideBlock = new TransformBlock<int, int>(item => item / 2);

    var flowCompletion = new DataflowLinkOptions { PropagateCompletion = true };
    multiplyBlock.LinkTo(addBlock, flowCompletion);
    addBlock.LinkTo(divideBlock, flowCompletion);

    return DataflowBlock.Encapsulate(multiplyBlock, divideBlock);
  }
}
