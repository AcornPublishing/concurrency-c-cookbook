using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

class ch10r00
{
  public void CancelableMethodWithOverload(CancellationToken cancellationToken)
  {
    // code goes here
  }

  public void CancelableMethodWithOverload()
  {
    CancelableMethodWithOverload(CancellationToken.None);
  }

  public void CancelableMethodWithDefault(
      CancellationToken cancellationToken = default)
  {
    // code goes here
  }
}

class ch10r01
{
  public async Task<int> CancelableMethodAsync(CancellationToken cancellationToken)
  {
    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    return 42;
  }

  void IssueCancelRequest()
  {
    using var cts = new CancellationTokenSource();
    var task = CancelableMethodAsync(cts.Token);

    // At this point, the operation has been started.

    // Issue the cancellation request.
    cts.Cancel();
  }

  async Task IssueCancelRequestAsync()
  {
    using var cts = new CancellationTokenSource();
    var task = CancelableMethodAsync(cts.Token);

    // At this point, the operation is happily running.

    // Issue the cancellation request.
    cts.Cancel();

    // (Asynchronously) wait for the operation to finish.
    try
    {
      await task;
      // If we get here, the operation completed successfully
      //  before the cancellation took effect.
    }
    catch (OperationCanceledException)
    {
      // If we get here, the operation was canceled before it completed.
    }
    catch (Exception)
    {
      // If we get here, the operation completed with an error
      //  before the cancellation took effect.
      throw;
    }
  }
}

class ch10r01B : Window
{
  private Button StartButton;
  private Button CancelButton;


  private CancellationTokenSource _cts;

  private async void StartButton_Click(object sender, RoutedEventArgs e)
  {
    StartButton.IsEnabled = false;
    CancelButton.IsEnabled = true;
    try
    {
      _cts = new CancellationTokenSource();
      CancellationToken token = _cts.Token;
      await Task.Delay(TimeSpan.FromSeconds(5), token);
      MessageBox.Show("Delay completed successfully.");
    }
    catch (OperationCanceledException)
    {
      MessageBox.Show("Delay was canceled.");
    }
    catch (Exception)
    {
      MessageBox.Show("Delay completed with error.");
      throw;
    }
    finally
    {
      StartButton.IsEnabled = true;
      CancelButton.IsEnabled = false;
    }
  }

  private void CancelButton_Click(object sender, RoutedEventArgs e)
  {
    _cts.Cancel();
    CancelButton.IsEnabled = false;
  }
}

class ch10r02
{
  public int CancelableMethod(CancellationToken cancellationToken)
  {
    for (int i = 0; i != 100; ++i)
    {
      Thread.Sleep(1000); // Some calculation goes here.
      cancellationToken.ThrowIfCancellationRequested();
    }
    return 42;
  }
}

class ch10r02B
{
  public int CancelableMethod(CancellationToken cancellationToken)
  {
    for (int i = 0; i != 100000; ++i)
    {
      Thread.Sleep(1); // Some calculation goes here.
      if (i % 1000 == 0)
        cancellationToken.ThrowIfCancellationRequested();
    }
    return 42;
  }
}

class ch10r03A
{
  async Task IssueTimeoutAsync()
  {
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    CancellationToken token = cts.Token;
    await Task.Delay(TimeSpan.FromSeconds(10), token);
  }
}

class ch10r03B
{
  async Task IssueTimeoutAsync()
  {
    using var cts = new CancellationTokenSource();
    CancellationToken token = cts.Token;
    cts.CancelAfter(TimeSpan.FromSeconds(5));
    await Task.Delay(TimeSpan.FromSeconds(10), token);
  }
}

class ch10r04
{
  public async Task<int> CancelableMethodAsync(CancellationToken cancellationToken)
  {
    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    return 42;
  }
}

class ch10r05
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

  void RotateMatrices2(IEnumerable<Matrix> matrices, float degrees,
      CancellationToken token)
  {
    // Warning: not recommended; see below.
    Parallel.ForEach(matrices, matrix =>
    {
      matrix.Rotate(degrees);
      token.ThrowIfCancellationRequested();
    });
  }

  IEnumerable<int> MultiplyBy2(IEnumerable<int> values,
      CancellationToken cancellationToken)
  {
    return values.AsParallel()
        .WithCancellation(cancellationToken)
        .Select(item => item * 2);
  }
}

class ch10r06: Window
{
  private Label MousePositionLabel;

  private IDisposable _mouseMovesSubscription;

  private void StartButton_Click(object sender, RoutedEventArgs e)
  {
    IObservable<Point> mouseMoves = Observable
        .FromEventPattern<MouseEventHandler, MouseEventArgs>(
            handler => (s, a) => handler(s, a),
            handler => MouseMove += handler,
            handler => MouseMove -= handler)
        .Select(x => x.EventArgs.GetPosition(this));
    _mouseMovesSubscription = mouseMoves.Subscribe(value =>
    {
      MousePositionLabel.Content = "(" + value.X + ", " + value.Y + ")";
    });
  }

  private void CancelButton_Click(object sender, RoutedEventArgs e)
  {
    if (_mouseMovesSubscription != null)
      _mouseMovesSubscription.Dispose();
  }
}

class ch10r06B
{
  async Task Test()
  {
    CancellationToken cancellationToken = default; // ...
    IObservable<int> observable = Observable.Range(0, 10); // ...
    int lastElement = await observable.TakeLast(1).ToTask(cancellationToken);
  }

  async Task Test2()
  {
    CancellationToken cancellationToken = default; // ...
    IObservable<int> observable = Observable.Range(0, 10); // ...
    IList<int> allElements = await observable.ToList().ToTask(cancellationToken);
  }

  void Test3()
  {
    using (var cancellation = new CancellationDisposable())
    {
      CancellationToken token = cancellation.Token;
      // Pass the token to methods that respond to it.
    }
    // At this point, the token is canceled.
  }
}

class ch10r07
{
  IPropagatorBlock<int, int> CreateMyCustomBlock(
      CancellationToken cancellationToken)
  {
    var blockOptions = new ExecutionDataflowBlockOptions
    {
      CancellationToken = cancellationToken
    };
    var multiplyBlock = new TransformBlock<int, int>(item => item * 2,
        blockOptions);
    var addBlock = new TransformBlock<int, int>(item => item + 2,
        blockOptions);
    var divideBlock = new TransformBlock<int, int>(item => item / 2,
        blockOptions);

    var flowCompletion = new DataflowLinkOptions
    {
      PropagateCompletion = true
    };
    multiplyBlock.LinkTo(addBlock, flowCompletion);
    addBlock.LinkTo(divideBlock, flowCompletion);

    return DataflowBlock.Encapsulate(multiplyBlock, divideBlock);
  }
}

class ch10r08
{
  async Task<HttpResponseMessage> GetWithTimeoutAsync(HttpClient client,
      string url, CancellationToken cancellationToken)
  {
    using CancellationTokenSource cts = CancellationTokenSource
        .CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(TimeSpan.FromSeconds(2));
    CancellationToken combinedToken = cts.Token;

    return await client.GetAsync(url, combinedToken);
  }
}

class ch10r09
{
  async Task<PingReply> PingAsync(string hostNameOrAddress, CancellationToken cancellationToken)
  {
    using var ping = new Ping();
    Task<PingReply> task = ping.SendPingAsync(hostNameOrAddress);
    using CancellationTokenRegistration _ = cancellationToken.Register(() => ping.SendAsyncCancel());
    return await task;
  }
}
