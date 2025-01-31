# K4A.Net (K4AdotNet)

<img align="right" width="64" height="64" src="https://github.com/bibigone/k4a.net/raw/master/K4AdotNet-64.png">

**K4A.Net** &mdash; *Three-in-one* managed .NET library to work with [Azure Kinect](https://azure.microsoft.com/en-us/services/kinect-dk/) devices (also known as Kinect for Azure, K4A, Kinect v4). It consists of the following "components":
1. `Sensor API` &mdash; access to depth camera, RGB camera, accelerometer and gyroscope, plus device-calibration data and synchronization control
   * Corresponding namespace: `K4AdotNet.Sensor`
   * Corresponding native API: [`k4a.h`](https://github.com/bibigone/k4a.net/blob/master/externals/k4a/include/k4a/k4a.h)
2. `Record API` &mdash; data recording from device to MKV-files, and data reading from such files
   * Corresponding namespace: `K4AdotNet.Record`
   * Corresponding native API: [`record.h`](https://github.com/bibigone/k4a.net/blob/master/externals/k4a/include/k4arecord/record.h) and [`playback.h`](https://github.com/bibigone/k4a.net/blob/master/externals/k4a/include/k4arecord/playback.h)
3. `Body Tracking API` &mdash; body tracking of multiple skeletons including eyes, ears and nose
   * Corresponding namespace: `K4AdotNet.BodyTracking`
   * Corresponding native API: [`k4abt.h`](https://github.com/bibigone/k4a.net/blob/master/externals/k4abt/include/k4abt.h)


## Key features

* Written fully on C#
* No unsafe code in library **K4AdotNet** itself (only `DllImports`)
* CLS-compliant (can be used from any .Net-compatible language, including C#, VB.Net)
* Library **K4AdotNet** is compiled against **.NET Standard 2.0** and **.NET Framework 4.6.1** target frameworks
  * This makes it compatible with **.NET Core 2.0** and later, **.NET Framework 4.6.1** and later, **Unity 2018.1** and later, etc.
  * See https://docs.microsoft.com/en-us/dotnet/standard/net-standard for details
* Clean API, which is close to C/C++ native API from [Azure Kinect Sensor SDK](https://docs.microsoft.com/en-us/azure/Kinect-dk/sensor-sdk-download) and [Azure Kinect Body Tracking SDK](https://docs.microsoft.com/en-us/azure/Kinect-dk/body-sdk-download).
* Plus useful helper methods, additional checks and meaningful exceptions.
* No additional dependencies
  * Except dependencies on native libraries (DLLs) from [Azure Kinect Sensor SDK](https://docs.microsoft.com/en-us/azure/Kinect-dk/sensor-sdk-download) and [Azure Kinect Body Tracking SDK](https://docs.microsoft.com/en-us/azure/Kinect-dk/body-sdk-download)
  * Native libraries from [Azure Kinect Sensor SDK](https://docs.microsoft.com/en-us/azure/Kinect-dk/sensor-sdk-download) are included to repository(see `externals` directory) and [NuGet package](https://www.nuget.org/packages/K4AdotNet)
  * But native libraries from [Azure Kinect Body Tracking SDK](https://docs.microsoft.com/en-us/azure/Kinect-dk/body-sdk-download) are *not* included to repository. It is recommended to install [Azure Kinect Body Tracking SDK](https://docs.microsoft.com/en-us/azure/Kinect-dk/body-sdk-download) separately. For details see below
* Plenty of powerful samples:
  * for .NET Core
  * for WPF
  * for Unity
  * and even more samples will be available soon (stay tuned)
* Well documented
* Unit-tested (more tests are awaited)
* Potentially multi-platform (Windows, Linux)
  * But currently tested only under Windows
  * And most of samples are written using WPF
* Available as NuGet package: https://www.nuget.org/packages/K4AdotNet


## Dependencies

**K4AdotNet** depends on the following native libraries (DLLs) from [Azure Kinect Sensor SDK](https://docs.microsoft.com/en-us/azure/Kinect-dk/sensor-sdk-download) and [Azure Kinect Body Tracking SDK](https://docs.microsoft.com/en-us/azure/Kinect-dk/body-sdk-download):

| Library "component" | Depends on                       | Version in use | Location in repository                | Included in [NuGet package](https://www.nuget.org/packages/K4AdotNet) 
|---------------------|----------------------------------|----------------|---------------------------------------|--------------------------
| Sensor API          | `k4a.dll`, `depthengine_1_0.dll` | 1.1.1          | `externals/k4a/windows-desktop/amd64` | YES
| Record API          | `k4arecord.dll`                  | 1.1.1          | `externals/k4a/windows-desktop/amd64` | YES
| Body Tracking API   | `k4abt.dll`, `onnxruntime.dll`   | 0.9.1          |                                       | no

Some important notes:
* `depthengine_1_0.dll` is required only if you are using `Transformation` or `Device` classes. All other Sensor API (types from `K4AdotNet.Sensor` namespace) depends only on `k4a.dll`.
* Native libraries from Body Tacking runtime are not included to repository because they, in turn, depend on:
  * bulky `dnn_model.onnx` file (159 MB)
  * [NVIDIA CUDA 10.0](https://developer.nvidia.com/cuda-10.0-download-archive)
  * [NVIDIA cuDNN](https://developer.nvidia.com/cudnn)
* The easiest way to use Body Tracking is to ask user to install **Body Tracking SDK** and all required components by him/herself based on the following step-by-step instruction: https://docs.microsoft.com/en-us/azure/Kinect-dk/body-sdk-setup
* **K4AdotNet** is trying to find Body Tracking runtime in the following locations:
  * directory with executable file
  * directory with **K4AdotNet** assembly
  * installation directory of **Body Tracking SDK** under `Program Files`
* Use `bool Sdk.IsBodyTrackingRuntimeAvailable(out string message)` method to check if Body Tracking runtime and all required components are available/installed
* Also, you can optionally call `bool Sdk.TryInitializeBodyTrackingRuntime(out string message)` method on start of your application to initialize Body Tracking runtime (it can take a few seconds)


## Versions

* [v0.9.0](https://github.com/bibigone/k4a.net/releases/tag/v0.9.0) &mdash; Mostly production-ready version of library
  * NuGet package: https://www.nuget.org/packages/K4AdotNet/0.9.0
  * Corresponding version of Azure Kinect Sensor SDK: 1.1.1 (included in release and [NuGet package](https://www.nuget.org/packages/K4AdotNet/0.9.0))
  * Tested with Azure Kinect Body Tracking SDK 0.9.1 (must be [installed separately](https://docs.microsoft.com/en-us/azure/Kinect-dk/body-sdk-setup))
  * Changes:
    * Icon :)
    * Some minor bug fixes
    * Improvements in exceptions and error messages (especially in `Device` class)
    * Documentation comments for all public API
    * Sample for Unity
    * Switching to version 0.9.1 of Body Tracking runtime

* [v0.5.0](https://github.com/bibigone/k4a.net/releases/tag/v0.5.0) &mdash; First public build. Stable enough to be used in pre-production projects
  * NuGet package: https://www.nuget.org/packages/K4AdotNet/0.5.0
  * Corresponding version of Azure Kinect Sensor SDK: 1.1.1 (included in release and [NuGet package](https://www.nuget.org/packages/K4AdotNet/0.5.0)
  * Tested with Azure Kinect Body Tracking SDK 0.9.0 and 0.9.1 (must be [installed separately](https://docs.microsoft.com/en-us/azure/Kinect-dk/body-sdk-setup))


## Roadmap

* More unit-tests
* More samples (Green screen, Recording, 3D view, Box-man, IMU...)
* Further development of sample for Unity
* Find out how to convert MJPEG -> BGRA faster (implementation in `k4a.dll` is very slow)
* Test under Linux, samples for Linux (using [Avalonia UI Framework](http://avaloniaui.net/)?)
* Some hosting for HTML documentation ([DocFX](https://dotnet.github.io/docfx/) + [github.io](https://pages.github.com/)?)


## How to build

* Open `K4AdotNet.sln` in Visual Studio 2017 or Visual Studio 2019
* Build solution (`Ctrl+Shift+B`)
* After that you can run and explore samples:
  * `K4AdotNet.Samples.BodyTrackingSpeedTest` &mdash; Core .NET sample console application to measure speed of Body Tracking.
  * `K4AdotNet.Samples.Wpf.Viewer` &mdash; WPF sample application to demonstrate usage of Sensor API and Record API.
  * `K4AdotNet.Samples.Wpf.BodyTracker` &mdash; WPF sample application to demonstrate usage of Body Tracking API.
* Instruction on building Unity sample can be found [here](https://github.com/bibigone/k4a.net/blob/master/K4AdotNet.Samples.Unity/readme.txt)
