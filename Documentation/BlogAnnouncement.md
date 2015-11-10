# Live Coding with Xamarin iOS

TLDR; I wrote a new Xamarin Studio add-in that dramatically reduces the number of **Build and Run** cycles you need to perform while developing an app. Please follow the [instructions to install Live Code]() and let me know what you think!

## I Love My History

Since the beginning of time, there has been one limitation of running .NET code on iOS using Xamarin - `System.Reflection.Emit` doesn't work. That means you cannot dynamically create executable code.

It's not a serious limitation. .NET has had this ability for years but, as a community, we really only use it for one purpose: to make code fast. In that vain, this missing feature hasn't really been a problem for us because the slow path is often just fine.

But there's a second use of `Emit`: improving the development experience with things like REPLs.

While mono spear-headed the "C# Interactive" movement with the `csharp` REPL, they hadn't been able to give us that tech when running on iOS.

Until now.


## Xamarin Inspector

Xamarin has released their [Xamarin Inspector tool](https://blog.xamarin.com/xamarin-inspector-preview/) that acts like the developer tools that you get with web browsers.

It's really nifty. On one hand it gives you an inspectable visual tree of your live-running app - just like the DOM in a webapp. They even have a cool layer breakout 3D view.

<a href="https://blog.xamarin.com/xamarin-inspector-preview/"><img src="https://blog.xamarin.com/wp-content/uploads/2015/11/mac-3d-view1-1024x554.png" width="420" /></a>

On top of that, there is a REPL so that you can type in C# code and see the result. This acts like the "Command" window in the browser dev tools.

Put these two together and you have a fantastic tool to diagnose what a mess you made of the view hierarchy. ;-) Click the link above and install the Inspector, you won't regret it.



## Yes, And

Yes Xamarin Inspector is great, and I want to see more tools along these lines. I especially can't wait to see if Xamarin uses this tool to help us write UI tests.

And yet, I have always been a bit unenthusiastic about classical REPLs. Surely it's fun to have a command prompt and play around a bit, but I have never been comfortable with the fact that you are not working with "real code" - the code that actually gets built to ship your app.

Since the second dawn of time, IDEs have integrated REPLs with real code with a simple trick: they allow you to select some code from your real code and send that over as a snippet to the REPL.

Even this stupid little convenience makes a world of difference. I use the heck out of **F# Interactive** which gives me this exact feature, and it's amazing.

Thanks to this tool, I find myself doing full app builds far less often.

Builds are the enemy for two reasons:

1. They lock up the IDE as you wait for big compilers to do their thing and as you wait for your app to restart. Of course, the IDE isn't frozen, but my mental state is. I cannot edit code because I might screw up the compilation and because the debugger will get lost. So I go into a mental spin-loop watching the progress bar. It's not healthy. (I used to check Twitter, but fixed that with an edit to `/etc/hosts`.)

2. Second, they re-initialize your context. If I'm working on one part of my app that's far removed from the initial screens, then I have to dig back through the app to get to seeing what I'm actually interested in. If I was a better automated test writer, or a better designer, or a better planner, this wouldn't be such a problem. But back to the real world...

A little while ago, I took a stab at doing something different from the REPL and wrote [Calca](http://calca.io). After some futzing around I found an environment that allowed me to see results as quickly as I could type them and it didn't have the annoying necessity to keep *sending code* to the evaluator.

I want something like Calca for my day to day work. I want to write code and see the results immediately.


## Xamarin Released Something Awesome and I Hadn't Realized it Yet

While watching [James Montemagno](http://motzcod.es)'s live stream on the Inspector, I started to wonder how it worked.

I started to wonder if Xamarin snuck in dynamic assembly support into their newest versions. I wrote a quick app that referenced `Mono.CSharp` which hosts mono's awesome dynamic evaluator, then tried to run the evaluator and got what I expected:

    System.InvalidOperationException

No dynamic code for you.

After James finished up, I installed the Inspector and laughed at some of my view hierarchies. Great tool.

And on a whim I ran my test app again, and you won't believe what happened next. The stupid thing ran.

That's right, **installing Xamarin Inspector makes dynamic assemblies work**. (On the simulator at least.) I don't know what dark and old magic makes this possible but the Xamarin engineers have come through again.

Well, we're given a hint into this dark magic. In the [Inspector docs](https://developer.xamarin.com/guides/cross-platform/inspector/#Known_Limitations), this passage appears as a "known limitation":

> As long as the Inspector addin/extension is installed and enabled in your IDE, we are injecting code into your app every time it starts in Debug mode

Haha, they call that a limitation. Dear Xamarin, enabling dynamic assemblies in all apps, at least in the development environment, is not only OK but please keep doing it. Please don't see this as a limitation - this is a feature I never knew was possible and I don't want to lose it.

When I saw my test program successfully evaluate code dynamically, I was aghast. Shocked because I didn't expect it to work, and horrified that by all the ideas that occurred to me. With great power comes great, oh forget it.


## An Idea

Little known fact: I spam Xamarin with bug reports and feature requests on a monthly basis. They are very tolerant of me and I appreciate it.

One of my last crazy ideas was a tool that I want to see integrated into the IDE that would enable live coding scenarios - all in an attempt to break the **Build and Run** cycle. It was a play off of Inspector with a little bit of influence from [Example Centric Programming (pdf)](http://www.subtext-lang.org/OOPSLA04.pdf).

The whole premise was that I wanted to see live evaluations of whole classes and modules while I was working on them without having to manually send snippets to a REPL. I wanted the tool to monitor certain classes and to visualize them whenever I changed them.

Imagine creating a UI layout. We have two options: we can use a designer or we can write it in code. With a designer, we pay the price of being separated from logic but are awarded with instantaneous feedback (or instantaneousish if using autolayout). With code, we have the full power of logic and data, but are stuck with the Build and Run cycle.

With live code, we can have the best of both worlds. We write the UI using code, but we see the effects of our code instantaneously.

## Time to Hack

[In two days I have been able to put together on tenth of the tool I described in my email](https://github.com/praeclarum/LiveCode/commits/master). But even this small version of it has me really excited.

It is able to do two things:

1. Send code to the iOS simulator to be evaluated and then visualized. This is to enable classic scenarios where I sometimes just want to know the value of a particular expression.

2. Monitor whole classes that are evaluated and visualized whenever they are edited. This makes creating UIs super fun and is the part I'm most excited about.

[Please go follow the instructions to run it and let me know what you think.](https://github.com/praeclarum/LiveCode/blob/master/README.md) (This only works in Xamarin Studio.)

I am not sure how well words can describe the tool, so I took the time to record a video of me using it. The video's a bit long, but I think you can get the general idea after just a few minutes (and if you skip the first 6 minutes describing installation).

Check it out:

<iframe width="420" height="315" src="https://www.youtube.com/embed/uani1bvOEIQ?rel=0&start=384" frameborder="0" allowfullscreen></iframe>


## Now What?

I hacked together a cool little tool that I'm pretty sure will become an invaluable asset. I still want to implement more of the features I described in my original design and make it work on other platforms.

Speaking of platforms, there is one major limitation: it only works in C#. While most won't see that as a limitation, I have been doing a lot of coding in F# lately and would prefer the tool to work with that.

Unfortunately F# doesn't ship with a simple compiler service like `Mono.CSharp` and I haven't tried yet to get the compiler to compile itself under Xamarin. I'm sure that this is technically possible, but gosh that F# compiler is intimidating and I don't know where to begin.

I'm also interested in seeing how much feedback this blog post and tool get. I often wonder if I'm just a nutter for hating Build cycles and can't wait to be validated or invalidated by your response. 

So say hello to me **@praeclarum** on Twitter and let me know if any of this looks good to you.




