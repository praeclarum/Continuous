# Continuous Coding for .NET

**Continuous Coding** is a live coding environment for .NET. With it, you can visualize your apps instantly as you code them. It currently specializes in the development of iOS and Android apps using Xamarin.

It currently only works in Visual Studio for Mac and only for C#.

## Visual Studio for Mac One-time Installation

Install the **Continuous Coding** add-in for Visual Studio in the **Add-in Manager** by adding a **New Gallery Repository** pointing at the URL:

	https://raw.githubusercontent.com/praeclarum/Continuous/master/Continuous.Client.MonoDevelop/AddinRepo

You will then be able to select the **Continuous Coding** add-in from the **IDE extensions** category.

<img src="https://raw.githubusercontent.com/praeclarum/Continuous/master/Documentation/AddAddinRepo.png" width="420px"/>

## Per-app Installation

1. Reference the [Continuous Coding nuget](https://www.nuget.org/packages/Continuous/).

2. Put this line of code somewhere in the initialization of your app (`AppDelegate.FinishedLaunching` or `Activity.OnCreate` are great places):

```csharp
#if DEBUG
new Continuous.Server.HttpServer(this).Run();
#endif
```

where `this` should refer to a `Context` on Android, and is ignored (can be anything or `null`) on iOS.

### iOS Specific

You may need to perform additional project setup to make your iOS project ready for Continuous:

* **iOS Simulator**: 

	Add `--enable-repl` to your 'Debug|Simulator' mtouch arguments.

* **iOS Device**: 
	
	You can use Continuous Coding with an iOS device if your Xamarin.iOS installation includes `System.Reflection.Emit` (SRE) - at the time of writing, standard installations do not. You can download a (now somewhat outdated) version of Xamarin.iOS with SRE from Xamarin [here](https://devblogs.microsoft.com/xamarin/introducing-xamarin-ios-interpreter), or build your own off recent commits ([this post](https://ryandavis.io/how-to-have-your-ios-13-preview-cake-and-emit-it-too) includes instructions on how to do so). Using preview or self-built versions of Xamarin.iOS should be considered experimental and performed at your own risk.

	Once you have a Xamarin.iOS installation that includes SRE, you can add `--interpreter` to your 'Debug|iPhone' mtouch arguments to allow code to be reloaded on the device. You will also need to select your device from the Device drop-down on the Continuous Coding pad (View -> Pads -> Continuous Coding), or enter its IP Address manually.

### Android Specific

If you want to run with the Android Emulator, you will have to forward the TCP port used by Continuous:

```bash
$ adb forward tcp:9634 tcp:9634
```

## Running

Run the debug version of your app. It should start as normal. 

### Running Snippets

Send snippets of code to be compiled, executed, and visualized using the **Visualize Selection** (Ctrl+Shift+Return) command.

### Continuously Coding a Class

You can mark a class to be monitored and automatically displayed whenever you edit it (and there are no code errors).

Move your cursor to be within a class and select the **Visualize Class** (Ctrl+Shift+C) command.


## Building

Run `make nuget` or `make mpack` from the repo to build the NuGet and IDE extension respectively.

### Prerequisties

Install the Visual Studio for Mac **Addin Maker** from the **Addin Development** group in the Gallery.


## How it Works

**Continuous** uses the class [`Mono.CSharp.Evaluator`](http://www.mono-project.com/docs/about-mono/languages/csharp/) to compile and execute code snippets while the app is running.

The IDE add-in allows the user to send snippets to this evaluator. The code is compiled and executed.

If the code produces a value (if it is an expression) then the value is automatically displayed.

The IDE communicates with the device using HTTP on port 9634. Requests and responses are simple JSON bundles.
