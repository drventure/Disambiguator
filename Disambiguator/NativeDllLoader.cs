using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Disambiguator
{
    /// <summary>
    /// Handles loading of native DLLs from embedded resources
    /// This allows the plugin to work without external native dependencies
    /// </summary>
    internal static class NativeDllLoader
    {
        private static bool _initialized = false;
        private static string _tempDirectory = null;

        /// <summary>
        /// Initialize the native DLL loader by setting up AppDomain resolution
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Create a temp directory for extracting native DLLs
            _tempDirectory = Path.Combine(Path.GetTempPath(), "Disambiguator_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            // Hook into the AppDomain's assembly resolve event
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        /// <summary>
        /// Clean up extracted native DLLs when the plugin is terminated
        /// </summary>
        public static void Cleanup()
        {
            if (_tempDirectory != null && Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, true);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
        }

        /// <summary>
        /// Handle assembly resolution for native DLLs
        /// </summary>
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            // We're only interested in native DLL loading attempts
            // The Patagames.Ocr library will try to load tesseract.dll
            // We intercept this and load from embedded resources
            
            // This event is for managed assemblies, but we need to handle
            // native DLL loading differently. We'll extract the DLLs when
            // the OCR API is first used.
            return null;
        }

        /// <summary>
        /// Extract and load the appropriate native Tesseract DLL for the current platform
        /// This should be called before any Tesseract OCR operations
        /// </summary>
        public static void EnsureTesseractLoaded()
        {
            if (!_initialized)
            {
                Initialize();
            }

            // Determine the platform (x86 or x64)
            bool is64Bit = IntPtr.Size == 8;
            string platformFolder = is64Bit ? "x64" : "x86";
            string resourceName = is64Bit 
                ? "Disambiguator.Resources.NativeDlls.x64.tesseract.dll" 
                : "Disambiguator.Resources.NativeDlls.x86.Tesseract.dll";

            // Create platform-specific subdirectory
            string platformDir = Path.Combine(_tempDirectory, platformFolder);
            Directory.CreateDirectory(platformDir);

            string dllPath = Path.Combine(platformDir, "tesseract.dll");

            // Only extract if not already done
            if (!File.Exists(dllPath))
            {
                ExtractEmbeddedDll(resourceName, dllPath);
            }

            // Load the DLL explicitly
            LoadLibrary(dllPath);

            // Also set the DLL directory so dependent libraries can be found
            SetDllDirectory(platformDir);
        }

        /// <summary>
        /// Extract an embedded DLL resource to a file
        /// </summary>
        private static void ExtractEmbeddedDll(string resourceName, string targetPath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new InvalidOperationException(
                        string.Format("Could not find embedded resource: {0}", resourceName));
                }

                using (FileStream fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }
        }

        // P/Invoke declarations for native DLL loading
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetDllDirectory(string lpPathName);
    }
}
