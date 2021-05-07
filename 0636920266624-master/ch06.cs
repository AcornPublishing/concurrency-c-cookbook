using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using Timer = System.Timers.Timer;

class ch06r01
{
  void Test()
  {
    var progress = new Progress<int>();
    IObservable<EventPattern<int>> progressReports =
        Observable.FromEventPattern<int>(
            handler => progress.ProgressChanged += handler,
            handler => progress.ProgressChanged -= handler);
    progressReports.Subscribe(data => Trace.WriteLine("OnNext: " + data.EventArgs));
  }

  void Test2()
  {
    var timer = new System.Timers.Timer(interval: 1000) { Enabled = true };
    IObservable<EventPattern<ElapsedEventArgs>> ticks =
        Observable.FromEventPattern<ElapsedEventHandler, ElapsedEventArgs>(
            handler => (s, a) => handler(s, a),
            handler => timer.Elapsed += handler,
            handler => timer.Elapsed -= handler);
    ticks.Subscribe(data => Trace.WriteLine("OnNext: " + data.EventArgs.SignalTime));
  }

  void Test3()
  {
    var timer = new System.Timers.Timer(interval: 1000) { Enabled = true };
    IObservable<EventPattern<object>> ticks =
        Observable.FromEventPattern(timer, nameof(Timer.Elapsed));
    ticks.Subscribe(data => Trace.WriteLine("OnNext: "
        + ((ElapsedEventArgs)data.EventArgs).SignalTime));
  }

  void Test4()
  {
    var client = new WebClient();
    IObservable<EventPattern<object>> downloadedStrings =
        Observable.FromEventPattern(client, nameof(WebClient.DownloadStringCompleted));
    downloadedStrings.Subscribe(
        data =>
        {
          var eventArgs = (DownloadStringCompletedEventArgs)data.EventArgs;
          if (eventArgs.Error != null)
            Trace.WriteLine("OnNext: (Error) " + eventArgs.Error);
          else
            Trace.WriteLine("OnNext: " + eventArgs.Result);
        },
        ex => Trace.WriteLine("OnError: " + ex.ToString()),
        () => Trace.WriteLine("OnCompleted"));
    client.DownloadStringAsync(new Uri("http://invalid.example.com/"));
  }
}

class ch06r02
{
  private void Button_Click(object sender, RoutedEventArgs e)
  {
    Trace.WriteLine($"UI thread is {Environment.CurrentManagedThreadId}");
    Observable.Interval(TimeSpan.FromSeconds(1))
        .Subscribe(x => Trace.WriteLine($"Interval {x} on thread {Environment.CurrentManagedThreadId}"));
  }
}

class ch06r02B
{
  private void Button_Click(object sender, RoutedEventArgs e)
  {
    SynchronizationContext uiContext = SynchronizationContext.Current;
    Trace.WriteLine($"UI thread is {Environment.CurrentManagedThreadId}");
    Observable.Interval(TimeSpan.FromSeconds(1))
        .ObserveOn(uiContext)
        .Subscribe(x => Trace.WriteLine($"Interval {x} on thread {Environment.CurrentManagedThreadId}"));
  }
}

class ch06r02C: Window
{
  private void Button_Click(object sender, RoutedEventArgs e)
  {
    SynchronizationContext uiContext = SynchronizationContext.Current;
    Trace.WriteLine($"UI thread is {Environment.CurrentManagedThreadId}");
    Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
            handler => (s, a) => handler(s, a),
            handler => MouseMove += handler,
            handler => MouseMove -= handler)
        .Select(evt => evt.EventArgs.GetPosition(this))
        .ObserveOn(Scheduler.Default)
        .Select(position =>
        {
          // Complex calculation
          Thread.Sleep(100);
          var result = position.X + position.Y;
          Trace.WriteLine($"Calculated result {result} on thread {Environment.CurrentManagedThreadId}");
          return result;
        })
        .ObserveOn(uiContext)
        .Subscribe(x => Trace.WriteLine($"Result {x} on thread {Environment.CurrentManagedThreadId}"));
  }
}

class ch06r03
{
  void Test()
  {
    Observable.Interval(TimeSpan.FromSeconds(1))
        .Buffer(2)
        .Subscribe(x => Trace.WriteLine(
            $"{DateTime.Now.Second}: Got {x[0]} and {x[1]}"));
  }
}

class ch06r03B
{
  void Test()
  {
    Observable.Interval(TimeSpan.FromSeconds(1))
        .Window(2)
        .Subscribe(group =>
        {
          Trace.WriteLine($"{DateTime.Now.Second}: Starting new group");
          group.Subscribe(
              x => Trace.WriteLine($"{DateTime.Now.Second}: Saw {x}"),
              () => Trace.WriteLine($"{DateTime.Now.Second}: Ending group"));
        });
  }
}

class ch06r03C: Window
{
  private void Button_Click(object sender, RoutedEventArgs e)
  {
    Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
            handler => (s, a) => handler(s, a),
            handler => MouseMove += handler,
            handler => MouseMove -= handler)
        .Buffer(TimeSpan.FromSeconds(1))
        .Subscribe(x => Trace.WriteLine(
            $"{DateTime.Now.Second}: Saw {x.Count} items."));
  }
}

class ch06r04: Window
{
  private void Button_Click(object sender, RoutedEventArgs e)
  {
    Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
            handler => (s, a) => handler(s, a),
            handler => MouseMove += handler,
            handler => MouseMove -= handler)
        .Select(x => x.EventArgs.GetPosition(this))
        .Throttle(TimeSpan.FromSeconds(1))
        .Subscribe(x => Trace.WriteLine(
            $"{DateTime.Now.Second}: Saw {x.X + x.Y}"));
  }
}

class ch06r04B: Window
{
  private void Button_Click(object sender, RoutedEventArgs e)
  {
    Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
            handler => (s, a) => handler(s, a),
            handler => MouseMove += handler,
            handler => MouseMove -= handler)
        .Select(x => x.EventArgs.GetPosition(this))
        .Sample(TimeSpan.FromSeconds(1))
        .Subscribe(x => Trace.WriteLine(
            $"{DateTime.Now.Second}: Saw {x.X + x.Y}"));
  }
}

class ch06r05
{
  void GetWithTimeout(HttpClient client)
  {
    client.GetStringAsync("http://www.example.com/").ToObservable()
        .Timeout(TimeSpan.FromSeconds(1))
        .Subscribe(
            x => Trace.WriteLine($"{DateTime.Now.Second}: Saw {x.Length}"),
            ex => Trace.WriteLine(ex));
  }
}

class ch06r05B: Window
{
  private void Button_Click(object sender, RoutedEventArgs e)
  {
    Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
            handler => (s, a) => handler(s, a),
            handler => MouseMove += handler,
            handler => MouseMove -= handler)
        .Select(x => x.EventArgs.GetPosition(this))
        .Timeout(TimeSpan.FromSeconds(1))
        .Subscribe(
            x => Trace.WriteLine($"{DateTime.Now.Second}: Saw {x.X + x.Y}"),
            ex => Trace.WriteLine(ex));
  }
}

class ch06r05C: Window
{
  private void Button_Click(object sender, RoutedEventArgs e)
  {
    IObservable<Point> clicks =
        Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
            handler => (s, a) => handler(s, a),
            handler => MouseDown += handler,
            handler => MouseDown -= handler)
        .Select(x => x.EventArgs.GetPosition(this));

    Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
            handler => (s, a) => handler(s, a),
            handler => MouseMove += handler,
            handler => MouseMove -= handler)
        .Select(x => x.EventArgs.GetPosition(this))
        .Timeout(TimeSpan.FromSeconds(1), clicks)
        .Subscribe(
            x => Trace.WriteLine($"{DateTime.Now.Second}: Saw {x.X},{x.Y}"),
            ex => Trace.WriteLine(ex));
  }
}
