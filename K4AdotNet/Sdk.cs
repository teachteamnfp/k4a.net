﻿using K4AdotNet.Sensor;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

[assembly: CLSCompliant(isCompliant: true)]

namespace K4AdotNet
{
    /// <summary>Static class with common basic things for Sensor, Record and Body Tracking APIs like logging, initializing, loading of dependencies, etc.</summary>
    public static class Sdk
    {
        #region Dependencies

        /// <summary>Name of main library (DLL) from Azure Kinect Sensor SDK.</summary>
        /// <remarks>This library is required for the most of API including Record and Body Tracking parts.</remarks>
        public const string SENSOR_DLL_NAME = "k4a";

        /// <summary>Name of depth engine library (DLL) from Azure Kinect Sensor SDK.</summary>
        /// <remarks>This library is required for <see cref="Device.StartCameras(DeviceConfiguration)"/> and <see cref="Transformation.Transformation(ref Calibration)"/>.</remarks>
        public const string DEPTHENGINE_DLL_NAME = "depthengine_1_0";

        /// <summary>Name of record library (DLL) from Azure Kinect Sensor SDK.</summary>
        /// <remarks>This library is required for Record part of API (see <c>K4AdotNet.Record</c> namespace).</remarks>
        public const string RECORD_DLL_NAME = "k4arecord";

        /// <summary>Name of body tracking library (DLL) from Azure Kinect Body Tracking SDK.</summary>
        /// <remarks>This library is required for Body Tracking part of API (see <c>K4AdotNet.BodyTracking</c> namespace).</remarks>
        public const string BODY_TRACKING_DLL_NAME = "k4abt";

        /// <summary>Name of ONNX runtime library (DLL) which is used by <see cref="BODY_TRACKING_DLL_NAME"/>.</summary>
        /// <remarks>This library is required for Body Tracking part of API (see <c>K4AdotNet.BodyTracking</c> namespace).</remarks>
        public const string ONNX_RUNTIME_DLL_NAME = "onnxruntime";

        /// <summary>Name of ONNX file with model of neural network used by <see cref="BODY_TRACKING_DLL_NAME"/>.</summary>
        /// <remarks>This data file is required for Body Tracking part of API (see <c>K4AdotNet.BodyTracking</c> namespace).</remarks>
        public const string BODY_TRACKING_DNN_MODEL_FILE_NAME = "dnn_model.onnx";

        #endregion

        #region Logging

        /// <summary>The Sensor SDK can log data to the console, files, or to a custom handler.</summary>
        /// <param name="level">Level of logging.</param>
        /// <param name="logToStdout">Log messages to STDOUT?</param>
        /// <param name="logToFile">
        /// Log all messages to the path and file specified.
        /// Must end in '.log' to be considered a valid entry.
        /// Use <see langword="null"/> or empty string to completely disable logging to a file.
        /// </param>
        /// <remarks>Call this method before any usage of Sensor and Record APIs.</remarks>
        public static void ConfigureLogging(TraceLevel level, bool logToStdout = false, string logToFile = null)
        {
            Environment.SetEnvironmentVariable("K4A_LOG_LEVEL", level.ToSdkLogLevelLetter(), EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("K4A_ENABLE_LOG_TO_STDOUT", logToStdout ? "1" : "0", EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("K4A_ENABLE_LOG_TO_A_FILE", string.IsNullOrWhiteSpace(logToFile) ? "0" : logToFile, EnvironmentVariableTarget.Process);
        }

        /// <summary>Current version of Body Tracking SDK can log data only to the console.</summary>
        /// <param name="level">Level of logging.</param>
        /// <param name="logToStdout">Log messages to STDOUT?</param>
        /// <param name="logToFile">
        /// Log all messages to the path and file specified.
        /// Must end in '.log' to be considered a valid entry.
        /// Use <see langword="null"/> or empty string to completely disable logging to a file.
        /// </param>
        /// <remarks>Call this method before any usage of Body Tracking API.</remarks>
        public static void ConfigureBodyTrackingLogging(TraceLevel level, bool logToStdout = false, string logToFile = null)
        {
            Environment.SetEnvironmentVariable("K4ABT_LOG_LEVEL", level.ToSdkLogLevelLetter(), EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("K4ABT_ENABLE_LOG_TO_STDOUT", logToStdout ? "1" : "0", EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("K4ABT_ENABLE_LOG_TO_A_FILE", string.IsNullOrWhiteSpace(logToFile) ? "0" : logToFile, EnvironmentVariableTarget.Process);
        }

        private static string ToSdkLogLevelLetter(this TraceLevel level)
        {
            switch (level)
            {
                case TraceLevel.Off: return "c";
                case TraceLevel.Error: return "e";
                case TraceLevel.Warning: return "w";
                case TraceLevel.Info: return "i";
                case TraceLevel.Verbose: return "t";
                default: throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        #endregion

        #region Body tracking SDK availability and initialization

        /// <summary>URL to step-by-step instruction "How to set up Body Tracking SDK". Helpful for UI and user messages.</summary>
        public static readonly string BodyTrackingSdkInstallationGuideUrl
            = "https://docs.microsoft.com/en-us/azure/Kinect-dk/body-sdk-setup";

        /// <summary>Checks that Body Tracking runtime is available.</summary>
        /// <param name="message">
        /// Detailed information about troubles with Body Tracking runtime if method returns <see langword="false"/>,
        /// or <see langword="null"/> if method returns <see langword="true"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if Body Tracking runtime is available and can be used, in this case <paramref name="message"/> is <see langword="null"/>,
        /// <see langword="false"/> if Body Tracking runtime is not available for some reason, in this case <paramref name="message"/> contains user-friendly description of this reason.
        /// </returns>
        /// <remarks><para>
        /// This method tries to find Body Tracking runtime in one of the following locations:
        /// directory with executable file,
        /// directory with <c>K4AdotNet</c> assembly,
        /// installation directory of Body Tracking SDK under <c>Program Files</c>.
        /// </para></remarks>
        /// <seealso cref="TryInitializeBodyTrackingRuntime(out string)"/>
        public static bool IsBodyTrackingRuntimeAvailable(out string message)
        {
            if (!IsOSCompatibleWithBodyTracking(out message))
                return false;

            var cudaBinPath = GetCudaPathForBodyTracking(out message);
            if (string.IsNullOrEmpty(cudaBinPath))
                return false;

            if (!IsCudnnAvailableForBodyTracking(cudaBinPath, out message))
                return false;

            var bodyTrackingRuntimePath = GetBodyTrackingRuntimePath(out message);
            if (string.IsNullOrEmpty(bodyTrackingRuntimePath))
                return false;

            message = null;
            return true;
        }

        /// <summary>Call this method to initialization of Body Tracking runtime.</summary>
        /// <param name="message">
        /// If Body Tracking runtime was initialized successfully, this parameter is <see langword="null"/>,
        /// otherwise it contains user-friendly description of failure reason.
        /// </param>
        /// <returns>
        /// <see langword="true"/> - if Body Tracking runtime was initialized successfully (in this case <paramref name="message"/> is <see langword="null"/>),
        /// <see langword="false"/> - otherwise and in this case <paramref name="message"/> contains user-friendly description of failure.
        /// </returns>
        /// <remarks><para>
        /// It is rather time consuming operation: initialization of ONNX runtime, loading and parsing of neural network model, etc.
        /// For this reason, it is recommended to initialize Body Tracking runtime in advance and show some progress window for user.
        /// But this initialization is optional. If it wasn't called explicitly, it will be called implicitly during first construction of
        /// <see cref="BodyTracking.Tracker"/> object.
        /// </para><para>
        /// This method tries to find Body Tracking runtime in one of the following locations:
        /// directory with executable file,
        /// directory with <c>K4AdotNet</c> assembly,
        /// installation directory of Body Tracking SDK under <c>Program Files</c>.
        /// </para></remarks>
        /// <seealso cref="IsBodyTrackingRuntimeAvailable(out string)"/>
        /// <seealso cref="BodyTracking.Tracker.Tracker(ref Calibration)"/>
        public static bool TryInitializeBodyTrackingRuntime(out string message)
        {
            Calibration.CreateDummy(DepthMode.NarrowView2x2Binned, ColorResolution.Off, out var calibration);
            if (!TryCreateTrackerHandle(ref calibration, out var trackerHandle, out message))
            {
                return false;
            }

            trackerHandle.Dispose();
            message = null;
            return true;
        }

        private static string GetBodyTrackingRuntimePath(out string message)
        {
            const string BODY_TRACKING_SDK_BIN_PATH = @"Azure Kinect Body Tracking SDK\sdk\windows-desktop\amd64\release\bin";

            message = null;

            // Try current directory
            var currentDir = Path.GetFullPath(Environment.CurrentDirectory);
            if (ProbePathForBodyTrackingRuntime(currentDir))
                return currentDir;

            // Try base directory of current app domain
            var baseDir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
            if (!baseDir.Equals(currentDir, StringComparison.InvariantCultureIgnoreCase))
            {
                if (ProbePathForBodyTrackingRuntime(baseDir))
                    return baseDir;
            }

            // Try location of this assembly
            var asm = Assembly.GetExecutingAssembly();
            var asmDir = Path.GetFullPath(Path.GetDirectoryName(new Uri(asm.GetName().CodeBase).LocalPath));
            if (!asmDir.Equals(currentDir, StringComparison.InvariantCultureIgnoreCase)
                && !asmDir.Equals(baseDir, StringComparison.InvariantCultureIgnoreCase))
            {
                if (ProbePathForBodyTrackingRuntime(asmDir))
                    return asmDir;
            }

            // Try standard location of installed Body Tracking SDK
            var sdkBinDir = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), BODY_TRACKING_SDK_BIN_PATH));
            if (ProbePathForBodyTrackingRuntime(sdkBinDir))
                return sdkBinDir;

            message = "Cannot find Body Tracking runtime (neither in application directory, nor in Body Tracking SDK directory).";
            return null;
        }

        private static bool IsOSCompatibleWithBodyTracking(out string message)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT
                || Environment.OSVersion.Version < new Version(6, 2)
                || !Environment.Is64BitOperatingSystem)
            {
                message = "Current version of Body Tracking supports only 64-bit Windows 8.1/10.";
                return false;
            }

            if (!Environment.Is64BitProcess)
            {
                message = "Process must be 64-bit.";
                return false;
            }

            message = null;
            return true;
        }

        private static string GetCudaPathForBodyTracking(out string message)
        {
            const string CUDA_VERSION = "10.0";
            const string CUDA_PATH_VARIABLE_NAME = "CUDA_PATH";
            const string CUDA_10_0_PATH_VARIABLE_NAME = "CUDA_PATH_V10_0";
            const string PATH_VARIABLE_NAME = "Path";
            const char PATH_ITEMS_SEPARATOR = ';';
            const string CUDA_RUNTIME_DLL = "cudart64_100.dll";

            var cudaPath = Environment.GetEnvironmentVariable(CUDA_10_0_PATH_VARIABLE_NAME);
            if (string.IsNullOrWhiteSpace(cudaPath))
                cudaPath = Environment.GetEnvironmentVariable(CUDA_PATH_VARIABLE_NAME);
            if (string.IsNullOrWhiteSpace(cudaPath)
                || cudaPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0
                || !Directory.Exists(cudaPath))
            {
                message = $"CUDA {CUDA_VERSION} must be installed.";
                return null;
            }
            var cudaDir = new DirectoryInfo(cudaPath);

            var pathVariable = Environment.GetEnvironmentVariable(PATH_VARIABLE_NAME) ?? string.Empty;
            var pathVariableItems = pathVariable.Split(PATH_ITEMS_SEPARATOR);
            string cudaBinPath = null;
            var wasButWrongVersion = false;
            foreach (var item in pathVariableItems)
            {
                if (!string.IsNullOrWhiteSpace(item) && item.IndexOfAny(Path.GetInvalidPathChars()) < 0)
                {
                    var itemDir = new DirectoryInfo(item);
                    if (Helpers.IsSubdirOf(itemDir, cudaDir) && itemDir.Exists)
                    {
                        var cudaRuntimeDllPath = Path.Combine(itemDir.FullName, CUDA_RUNTIME_DLL);
                        if (File.Exists(cudaRuntimeDllPath))
                        {
                            var ver = FileVersionInfo.GetVersionInfo(cudaRuntimeDllPath);
                            if (ver != null && ver.FileDescription != null && ver.FileDescription.Contains($"Version {CUDA_VERSION}."))
                            {
                                cudaBinPath = itemDir.FullName;
                                break;
                            }
                            else
                            {
                                wasButWrongVersion = true;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(cudaBinPath))
            {
                message = wasButWrongVersion
                    ? $"Version {CUDA_VERSION} of CUDA is required. Different version found. Please install CUDA {CUDA_VERSION}."
                    : $"CUDA {CUDA_VERSION} is not installed or environment variable {PATH_VARIABLE_NAME} does not contain path to binary directory of CUDA {CUDA_VERSION}.";
                return null;
            }

            message = null;
            return cudaBinPath;
        }

        private static bool IsCudnnAvailableForBodyTracking(string cudaBinPath, out string message)
        {
            const string CUDNN_DLL = "cudnn64_7.dll";

            var cudnnPath = Path.Combine(cudaBinPath, CUDNN_DLL);
            if (!File.Exists(cudnnPath))
            {
                message = $"cuDNN v7.5.x for CUDA 10.0 is not installed: {CUDNN_DLL} library is not found in CUDA binary directory \"{cudaBinPath}\".";
                return false;
            }

            message = null;
            return true;
        }

        private static bool ProbePathForBodyTrackingRuntime(string path)
        {
            const string DLL_EXTENSION = ".dll";

            if (!Directory.Exists(path))
                return false;

            var k4abt = Path.Combine(path, BODY_TRACKING_DLL_NAME + DLL_EXTENSION);
            if (!File.Exists(k4abt))
                return false;

            var onnxruntime = Path.Combine(path, ONNX_RUNTIME_DLL_NAME + DLL_EXTENSION);
            if (!File.Exists(onnxruntime))
                return false;

            var dnnmodel = Path.Combine(path, BODY_TRACKING_DNN_MODEL_FILE_NAME);
            if (!File.Exists(dnnmodel))
                return false;

            return true;
        }

        internal static bool TryCreateTrackerHandle(ref Calibration calibration, out NativeHandles.TrackerHandle trackerHandle, out string message)
        {
            string runtimePath;

            lock (bodyTrackingRuntimeInitializationSync)
            {
                if (string.IsNullOrEmpty(bodyTrackingRuntimePath))
                {
                    bodyTrackingRuntimePath = GetBodyTrackingRuntimePath(out message);
                    if (string.IsNullOrEmpty(bodyTrackingRuntimePath))
                    {
                        trackerHandle = null;
                        return false;
                    }
                }

                runtimePath = bodyTrackingRuntimePath;
            }

            // Force loading of k4a.dll,
            // because k4abt.dll depends on it
            NativeHandles.NativeApi.CaptureRelease(IntPtr.Zero);

            // We have to change current directory,
            // because Body Tracking runtime will try to load ONNX file from current directory
            // (Adding to Path environment variable doesn't work.)
            using (new CurrentDirectoryOverrider(runtimePath))
            {
                if (BodyTracking.NativeApi.TrackerCreate(ref calibration, out trackerHandle) != NativeCallResults.Result.Succeeded
                    || trackerHandle == null || trackerHandle.IsInvalid)
                {
                    if (IsBodyTrackingRuntimeAvailable(out message))
                        message = "Cannot initialize body tracking runtime. See logs for details.";
                    return false;
                }
            }

            message = null;
            return true;
        }

        private static string bodyTrackingRuntimePath;
        private static readonly object bodyTrackingRuntimeInitializationSync = new object();

        #endregion
    }
}
