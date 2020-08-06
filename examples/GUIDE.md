# C# Guide

In this guide I'm going to go through a basic setup for getting started with the Analog SDK using C# (specifically with .NET Core, but .NET Framework can also be used). We will create a simple .NET Core Console app which will output the analog key values.

## Prerequisites

- [Wooting Analog SDK installed](https://github.com/WootingKb/wooting-analog-sdk#installing)
- [.NET Core](https://dotnet.microsoft.com/download)
- IDE or Text Editor of your choice

## Project Setup (CLI)

To get started we'll need to create a new .NET Core project. First navigate to the directory you wish to have your project in.

```bash
# Navigate to the folder you wish to create your project in
cd path/to/my/project
# Create a new Console app with the name 'analog-test'
dotnet new console -n analog-test
```

Once this is finished, you'll now have a folder called `analog-test` with a full Console app project inside.

```bash
# Enter the folder of the new project
cd analog-test
```

In this folder you'll have a `Program.cs` (where your code will go) and `analog-test.csproj` which contains the configuration and information about your project.

Next, you'll need to add a reference to the [WootingAnalogSDK.NET package](https://www.nuget.org/packages/WootingAnalogSDK.NET/0.4.0) which gives us access to all the Analog SDK methods.

```bash
dotnet add package WootingAnalogSDK.NET
```

## Project Setup (GUI [Visual Studio])

### Creating the project

To get started when using Visual Studio, we'll first want to create a new .NET Core project if you haven't got one setup already. I'll be showing you instructions for Visual Studio 2019, however, it should be about the same for previous or newer versions. As a slight side note, you'll need to ensure you've included the ".NET Core cross platform development" package when installing Visual Studio. See [here](https://visualstudio.microsoft.com/vs/features/net-development/) for some details about .NET with Visual Studio.

[![](https://dev.wooting.io/wp-content/uploads/2020/02/vs_0-300x199.png)](https://dev.wooting.io/wp-content/uploads/2020/02/vs_0.png)

First you'll need to click on "Create a new project"

[![](https://dev.wooting.io/wp-content/uploads/2020/02/vs_1-300x199.png)](https://dev.wooting.io/wp-content/uploads/2020/02/vs_1.png)

Then you'll want to select the "Console App (.NET Core)" project template. Making sure to click on the C# one as opposed to the Visual Basic one. There is no inherent requirement for this to be a Console app, so you could create a .NET Core WPF app instead.

[![](https://dev.wooting.io/wp-content/uploads/2020/02/vs_2-300x199.png)](https://dev.wooting.io/wp-content/uploads/2020/02/vs_2.png)

Create a name for your project, in this case I've chosen "analog-test"

[![](https://dev.wooting.io/wp-content/uploads/2020/02/vs_3-300x225.png)](https://dev.wooting.io/wp-content/uploads/2020/02/vs_3.png)

Once your project has been created, you'll be greeted with this page

### Adding the NuGet package

The next important step is to add a reference to the `WootingAnalogSDK.NET` which will give you access to all the Analog API's as well as the required wrapper library. The NuGet package has been set up to work on all the platforms the Analog SDK supports, i.e. Windows, Mac and Linux.

[![](https://dev.wooting.io/wp-content/uploads/2020/02/vs_4-145x300.png)](https://dev.wooting.io/wp-content/uploads/2020/02/vs_4.png)

First you'll need to right click on the project and click "Manage NuGet Packages..."

[![](https://dev.wooting.io/wp-content/uploads/2020/02/vs_5-300x113.png)](https://dev.wooting.io/wp-content/uploads/2020/02/vs_5.png)

Then you'll be brought to this screen, you need to click on "Browse" and search for "WootingAnalogSDK"

[![](https://dev.wooting.io/wp-content/uploads/2020/02/vs_7-300x290.png)](https://dev.wooting.io/wp-content/uploads/2020/02/vs_7.png)

Click on the entry for "WootingAnalogSDK.NET" and click "Install". Once the pop-up is shown, click "OK" to confirm the changes

After doing these steps you're all setup to get started playing around with the Analog API's! You can now skip to the "Initialisation" step.

However, if you're using .NET Framework, keep reading the next couple of steps to ensure your project is configured correctly!

### Additional steps if using .NET Framework

If your project is targeting .NET Framework, a minor additional step is required for it to work properly. Once you add a reference to the `WootingAnalogSDK.NET` NuGet package, a file called `wooting_analog_wrapper.dll` will have been added to your project root. This is the DLL the library uses to communicate with the Analog SDK. By default this is not set to copy to your build directory, so the library will fail to initialise. It is very important to change the `Copy to Output Directory` property to `Copy Always` or `Copy if newer`.

[![](https://dev.wooting.io/wp-content/uploads/2020/02/vs_framework0-125x300.png)](https://dev.wooting.io/wp-content/uploads/2020/02/vs_framework0.png)

[![](https://dev.wooting.io/wp-content/uploads/2020/02/vs_framework1-124x300.png)](https://dev.wooting.io/wp-content/uploads/2020/02/vs_framework1.png)

## Initialisation

First, at the top of the `Program.cs` file you'll need to import the namespace of our package.

```csharp
using System;
using WootingAnalogSDKNET;
// We'll also include this for the Thread.Sleep call we'll have later on
using System.Threading;
```

Then we want to add some code to the `Main` method to initialise the SDK.

```csharp
static void Main(string[] args)
{
	Console.WriteLine("Hello Analog SDK!");

	// Initialise the SDK
	var (noDevices, error) = WootingAnalogSDK.Initialise();
	// If the number of devices is at least 0 it indicates the initialisation was successful
	if (noDevices >= 0) {
		Console.WriteLine($"Analog SDK Successfully initialised with {noDevices} devices!");
	}
	else {
		Console.WriteLine($"Analog SDK failed to initialise: {error}");
	}
}
```

Running this in Visual Studio or calling `dotnet run` in the command line should give you this output:

```bash
dotnet run
Hello Analog SDK!
Analog SDK Successfully initialised with 1 devices!
```

If it says `initialised with 0 devices` don't worry, as in some edge cases your keyboard may not be seen straight away, but the SDK should pick it up shortly after.

However, if the SDK fails to initialise, that usually indicates that there is a problem with your SDK installation. i.e. plugins couldn't be found/loaded or the SDK couldn't be found. Please check again if you installed all the prerequisites. Otherwise, feel free to [open an issue on Github](https://github.com/WootingKb/wooting-analog-sdk/issues).

## Basic Analog Reading

Now, we want to get to the fun part, the actual analog key reading!

Inside the `if (noDevices >= 0) {` we want to do the actual analog reading, as we know the SDK was initialised successfully.

```csharp
Console.WriteLine($"Analog SDK Successfully initialised with {noDevices} devices!");
...
// This can be used to make the SDK give you keycodes from the Windows Virtual Key set that are translated based on the language set in Windows
// By default the keycodes the SDK will give you are the HID keycodes
//WootingAnalogSDK.SetKeycodeMode(KeycodeType.VirtualKeyTranslate);

while (true) {
	var (keys, readErr) = WootingAnalogSDK.ReadFullBuffer(20);
	if (readErr == WootingAnalogResult.Ok)
	{
		// Go through all the keys that were read and output them
		foreach (var analog in keys)
		{
			Console.Write($"({analog.Item1},{analog.Item2})");
		}

		// We want to put on the new line character only if keys have been read and output to the console
		if (keys.Count > 0)
			Console.WriteLine();
	}
	else
	{
		Console.WriteLine($"Read failed with {readErr}");
		// We want to put more of a delay in when we get an error as we don't want to spam the log with the errors
		Thread.Sleep(1000);
	}
	// We want to have a bit of a delay so we don't spam the console with new values
	Thread.Sleep(100);
}
```

With this, now when you run the project you'll get an output of the analog value anytime you press a key! For example:

```bash
dotnet run

Hello Analog SDK!
Analog SDK Successfully initialised with 1 devices!
d(7,0.7152942)
(7,0.8941177)
(7,0.9270589)
(7,0.85176474)
(7,0)
f(9,0.39058825)
(9,1)
(9,0.22588237)
(9,0)
t(23,0.40000004)
(23,1)
(23,0)
```

## Inspecting Connected Devices

It's pretty important to be able to see what devices are connected. To do this, the `GetConnectedDevicesInfo` method is available. The following is a simple code example:

```csharp
    Console.WriteLine($"Analog SDK Successfully initialised with {noDevices} devices!");
    ...

    // Get a list of the connected devices and Associated information
    var (devices, infoErr) = WootingAnalogSDK.GetConnectedDevicesInfo();
    if (infoErr != WootingAnalogResult.Ok)
    		Console.WriteLine($"Error getting devices: {infoErr}");

    foreach (DeviceInfo device in devices)
    {
    		Console.WriteLine($"Device info has: {device}");
    }

    ...

    while (true) {
```

You'll want to make sure you put this code before the while loop from above, as once the program gets to the while loop it'll be stuck there until you close it.

Running this will give you an output similar to the following:

```bash
Hello Analog SDK!
Analog SDK Successfully initialised with 1 devices!
Device info has: {
"vendor_id": 1003,
"product_id": 65282,
"manufacturer_name": "Wooting",
"device_name": "WootingTwo",
"device_id": 17878653816681929137,
"device_type": 1
}
```

## Monitoring Devices Connecting & Disconnecting

The Analog SDK provides a `DeviceEvent` event which will be called everytime an Analog Device is connected and disconnected. A very simple setup is as follows:

```csharp
static void callback(DeviceEventType eventType, DeviceInfo deviceInfo) {
	Console.WriteLine($"Device event cb called with: {eventType} {deviceInfo}");
}

static void Main(string[] args)
{
    ...
    if (noDevices >= 0) {
    	Console.WriteLine($"Analog SDK Successfully initialised with {noDevices} devices!");

    	// Subscribe to the DeviceEvent
    	WootingAnalogSDK.DeviceEvent += callback;

    	...
```

Now when running the project you can disconnect and reconnect your keyboard and see the Device Event callbacks coming in!

## Full Code example

An entire example project can be found on the `wooting-analog-wrapper` repo [here](https://github.com/WootingKb/wooting-analog-wrappers/tree/develop/examples/analog-test).

```csharp
using System;
using WootingAnalogSDKNET;
using System.Threading;

namespace analog_test
{
	class Program
	{
		static void callback(DeviceEventType eventType, DeviceInfo deviceInfo) {
			Console.WriteLine($"Device event cb called with: {eventType} {deviceInfo}");
		}

		static void Main(string[] args)
		{
			Console.WriteLine("Hello Analog SDK!");

			// Initialise the SDK
			var (noDevices, error) = WootingAnalogSDK.Initialise();

			// If the number of devices is at least 0 it indicates the initialisation was successful
			if (noDevices >= 0) {
				Console.WriteLine($"Analog SDK Successfully initialised with {noDevices} devices!");

				// Subscribe to the DeviceEvent
				WootingAnalogSDK.DeviceEvent += callback;


				// Get a list of the connected devices and Associated information
				var (devices, infoErr) = WootingAnalogSDK.GetConnectedDevicesInfo();
				if (infoErr != WootingAnalogResult.Ok)
					Console.WriteLine($"Error getting devices: {infoErr}");

				foreach (DeviceInfo device in devices)
				{
					Console.WriteLine($"Device info has: {device}");
				}

				// This can be used to make the SDK give you keycodes from the Windows Virtual Key set that are translated based on the language set in Windows
				// By default the keycodes the SDK will give you are the HID keycodes
				//WootingAnalogSDK.SetKeycodeMode(KeycodeType.VirtualKeyTranslate);

				while (true) {
					var (keys, readErr) = WootingAnalogSDK.ReadFullBuffer(20);
					if (readErr == WootingAnalogResult.Ok)
					{
						foreach (var analog in keys)
						{
							Console.Write($"({analog.Item1},{analog.Item2})");
						}

						if (keys.Count > 0)
							Console.WriteLine();
					}
					else
					{
						Console.WriteLine($"Read failed with {readErr}");
						// We want to put more of a delay in when we get an error as we don't want to spam the log with the errors
						Thread.Sleep(1000);
					}

					// We want to have a bit of a delay so we don't spam the console with new values
					Thread.Sleep(100);
				}
			}
			else {
				Console.WriteLine($"Analog SDK failed to initialise: {error}");
			}
		}
	}
}
```
