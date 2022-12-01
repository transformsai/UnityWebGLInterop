### JS-Interop Package

The purpose of this library is to write c# code in Unity to access browser functions:

For example 

```csharp

// csharp code.

var window = JsRuntime.GetGlobalValue("window");
var location = window.GetProp("location").GetProp("href");

var console = JsRuntime.GetGlobalValue("console");
console.Call("log", "Outputting our location", location);
```

While this library is highly optimized to reduce allocations in the unity heap, it's still slower than direct interop and something like tinyutils.

The primary objective of the library is to allow you to write algorithms in C# that may need access Javascript libraries.

I used this to implement https://github.com/transformsai/UnityWebGLHttpHandler

I decided to do this since it would facilitate writing a C# HTTP handler while delegating calls to JS Fetch. 

One of the main benefits of using this library is the ability to hold references to JS objects in C#, for example:

```
// We're still in C#
var element =  JSRuntime.GetGlobalValue("document").Call("getElementById", "some-element-id");
var element2 =  JSRuntime.GetGlobalValue("document").Call("getElementById", "some-other-element-id");

element2.Call("appendChild", element2);
```

There's also some cool stuff in this library like:

Support for callbacks
Support for awaiting JS promises from inside C# (!!!)
Avoiding garbage collection (and segfaults )for long-lived callbacks.
Two-way garbage collection synchronization.
Support for sharing memory across C# and JS. (use `SharedTypeArray`)

That said, I've moved to a different project at TransformsAI and haven't had time to continue working on this stack. Hopefully this comment helps :)
