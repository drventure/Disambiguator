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
        // P/Invoke declarations for native DLL loading
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetDllDirectory(string lpPathName);


        internal static string TessDataPath = null;

        private static bool _initialized = false;
        private static string _tempDirectory = null;

        /// <summary>
        /// Initialize the native DLL loader by setting up AppDomain resolution
        /// </summary>
        public static void Initialize()
        {
            DisambiguatorExt.Debug("NativeDllLoader: Initializing...");
            if (_initialized) return;
            _initialized = true;

            try
            {
                // Create a temp directory for extracting native DLLs
                _tempDirectory = Path.Combine(Path.GetTempPath(), "Disambiguator_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_tempDirectory);
                DisambiguatorExt.Debug("NativeDllLoader: Created temp folder: {0}", _tempDirectory);

                // Extract a tessdata files
                TessDataPath = Path.Combine(_tempDirectory, "tessdata");
                var filename = Path.Combine(TessDataPath, "eng.traineddata");
                var success = NativeDllLoader.ExtractEmbeddedResource("Disambiguator.tessdata.eng.traineddata", filename, overwrite: true);
                DisambiguatorExt.Debug("NativeDllLoader: Save eng: {0}", success);

                filename = Path.Combine(TessDataPath, "equ.traineddata");
                success = NativeDllLoader.ExtractEmbeddedResource("Disambiguator.tessdata.equ.traineddata", filename, overwrite: true);
                DisambiguatorExt.Debug("NativeDllLoader: Save equ: {0}", success);

                filename = Path.Combine(TessDataPath, "osd.traineddata");
                success = NativeDllLoader.ExtractEmbeddedResource("Disambiguator.tessdata.osd.traineddata", filename, overwrite: true);
                DisambiguatorExt.Debug("NativeDllLoader: Save osd: {0}", success);

                filename = Path.Combine(TessDataPath, "pdf.ttf");
                success = NativeDllLoader.ExtractEmbeddedResource("Disambiguator.tessdata.pdf.ttf", filename, overwrite: true);
                DisambiguatorExt.Debug("NativeDllLoader: Save ttf: {0}", success);

                //string configPath = Path.Combine(TessDataPath, "\\configs\\config.cfg");
                //bool success = NativeDllLoader.ExtractEmbeddedResource("Disambiguator.tessdata.configs.config.cfg", configPath, overwrite: true);

                // Hook into the AppDomain's assembly resolve event
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            }
            catch (Exception ex)
            {
                DisambiguatorExt.Debug("NativeDllLoader: Error during initialization: " + ex.ToString());
                throw;
            }
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
        /// Extract an embedded resource to a file at the specified path
        /// </summary>
        /// <param name="resourceName">The full name of the embedded resource</param>
        /// <param name="targetPath">The full path and filename where the resource should be extracted</param>
        /// <param name="overwrite">Whether to overwrite the file if it already exists</param>
        /// <returns>True if extraction was successful, false otherwise</returns>
        private static bool ExtractEmbeddedResource(string resourceName, string targetPath, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                throw new ArgumentException("Resource name cannot be null or empty", "resourceName");
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                throw new ArgumentException("Target path cannot be null or empty", "targetPath");
            }

            // Check if file already exists
            if (File.Exists(targetPath) && !overwrite)
            {
                return false;
            }

            try
            {
                // Ensure the target directory exists
                string directory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Get the executing assembly
                Assembly assembly = Assembly.GetExecutingAssembly();

                // Get the embedded resource stream
                using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                    {
                        throw new InvalidOperationException(
                            string.Format("Could not find embedded resource: {0}", resourceName));
                    }

                    // Write the resource to the target file
                    using (FileStream fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log the error if needed
                DisambiguatorExt.Debug(string.Format("Error extracting embedded resource '{0}' to '{1}': {2}", 
                    resourceName, targetPath, ex.ToString()));
                return false;
            }
        }


        /// <summary>
        /// Get a list of all embedded resource names in the current assembly
        /// Useful for debugging to see what resources are available
        /// </summary>
        /// <returns>Array of resource names</returns>
        public static string[] GetEmbeddedResourceNames()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceNames();
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
    }
}
