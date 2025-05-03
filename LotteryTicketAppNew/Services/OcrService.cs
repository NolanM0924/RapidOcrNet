using RapidOcrNet;
using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Reflection;
using Microsoft.ML.OnnxRuntime;
using System.Text;

namespace LotteryTicketAppNew.Services;

public class OcrService : IDisposable
{
    private readonly RapidOcr _ocrEngine;
    private bool _isInitialized = false;
    private bool _isDisposed = false;
    private Exception? _lastInitError = null;

    public OcrService()
    {
        _ocrEngine = new RapidOcr();
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            await CopyModelsAsync();

            string modelsPath = Path.Combine(FileSystem.AppDataDirectory, "models");
            string detPath = Path.Combine(modelsPath, "en_PP-OCRv3_det_infer_opt.onnx");
            string clsPath = Path.Combine(modelsPath, "ch_ppocr_mobile_v2.0_cls_infer_opt.onnx");
            string recPath = Path.Combine(modelsPath, "en_PP-OCRv3_rec_infer_opt.onnx");
            string keysPath = Path.Combine(modelsPath, "en_dict.txt");

            // Check if files exist
            if (!File.Exists(detPath))
                throw new FileNotFoundException($"Detection model not found at {detPath}");
            if (!File.Exists(clsPath))
                throw new FileNotFoundException($"Classification model not found at {clsPath}");
            if (!File.Exists(recPath))
                throw new FileNotFoundException($"Recognition model not found at {recPath}");
            if (!File.Exists(keysPath))
                throw new FileNotFoundException($"Dictionary file not found at {keysPath}");

            try
            {
                // Initialize ONNX Runtime with specific handling for MacCatalyst
                SetupOnnxRuntimeEnvironment();

                // Try creating a session options instance to check if ONNX Runtime is working
                using var sessionOptions = new Microsoft.ML.OnnxRuntime.SessionOptions();
                
                // If we got here, ONNX Runtime appears to be working, so initialize the OCR models
                _ocrEngine.InitModels(detPath, clsPath, recPath, keysPath, 0);
                _isInitialized = true;
            }
            catch (TypeInitializationException ex)
            {
                var innerEx = ex.InnerException;
                var details = new StringBuilder();
                details.AppendLine($"ONNX Runtime initialization error: {ex.Message}");
                
                if (innerEx != null) 
                {
                    details.AppendLine($"Inner exception: {innerEx.Message}");
                    details.AppendLine($"Inner exception type: {innerEx.GetType().FullName}");
                    details.AppendLine($"Inner exception stack trace: {innerEx.StackTrace}");
                    
                    // Check for DllNotFoundException specifically
                    if (innerEx is DllNotFoundException dllEx)
                    {
                        details.AppendLine($"Missing DLL/library: {dllEx.Message}");
                        
                        // Try to get additional environment information
                        details.AppendLine($"Environment variables:");
                        foreach (var key in Environment.GetEnvironmentVariables().Keys)
                        {
                            var name = key?.ToString() ?? "";
                            if (name.StartsWith("DYLD_") || name.StartsWith("LD_") || name.StartsWith("PATH"))
                            {
                                details.AppendLine($"  {name}: {Environment.GetEnvironmentVariable(name)}");
                            }
                        }
                    }
                }
                
                _lastInitError = new Exception(details.ToString(), ex);
                throw _lastInitError;
            }
            catch (DllNotFoundException ex)
            {
                _lastInitError = new Exception($"ONNX Runtime native libraries not found: {ex.Message}", ex);
                throw _lastInitError;
            }
            catch (Exception ex)
            {
                _lastInitError = new Exception($"Failed to initialize OCR models: {ex.Message}", ex);
                throw _lastInitError;
            }
        }
        catch (Exception ex)
        {
            _lastInitError = ex;
            throw new Exception($"Error during OCR initialization: {ex.Message}", ex);
        }
    }

    private void SetupOnnxRuntimeEnvironment()
    {
        try
        {
            // MacCatalyst specific configuration
            if (DeviceInfo.Platform == DevicePlatform.MacCatalyst)
            {
                // Get the assembly location for Microsoft.ML.OnnxRuntime
                var onnxRuntimeAssembly = typeof(Microsoft.ML.OnnxRuntime.SessionOptions).Assembly;
                var assemblyLocation = onnxRuntimeAssembly.Location;
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);
                
                // Print debug information
                Console.WriteLine($"ONNX Runtime assembly location: {assemblyLocation}");
                Console.WriteLine($"ONNX Runtime assembly directory: {assemblyDir}");
                
                if (assemblyDir != null)
                {
                    // Add possible native library locations to path
                    string onnxRuntimeNativePath = Path.Combine(assemblyDir, "runtimes", "osx", "native");
                    if (Directory.Exists(onnxRuntimeNativePath))
                    {
                        Console.WriteLine($"Found ONNX Runtime native path: {onnxRuntimeNativePath}");
                        // On macOS/iOS, DYLD_LIBRARY_PATH is used instead of LD_LIBRARY_PATH
                        string currentPath = Environment.GetEnvironmentVariable("DYLD_LIBRARY_PATH") ?? "";
                        if (!currentPath.Contains(onnxRuntimeNativePath))
                        {
                            string newPath = string.IsNullOrEmpty(currentPath) ? 
                                onnxRuntimeNativePath : 
                                $"{onnxRuntimeNativePath}:{currentPath}";
                            
                            Environment.SetEnvironmentVariable("DYLD_LIBRARY_PATH", newPath);
                            Console.WriteLine($"Set DYLD_LIBRARY_PATH to: {newPath}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ONNX Runtime native path not found at: {onnxRuntimeNativePath}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting up ONNX Runtime environment: {ex.Message}");
        }
    }

    private async Task CopyModelsAsync()
    {
        try
        {
            string targetDir = Path.Combine(FileSystem.AppDataDirectory, "models");
            
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            // Use the models directory from the main RapidOcrNet project
            string sourceDir = Path.Combine(GetSolutionDirectory(), "..", "RapidOcrNet", "models");
            
            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException($"Models directory not found at: {sourceDir}");
            }

            // Copy model files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                if (!File.Exists(targetFile))
                {
                    using (var sourceStream = File.OpenRead(file))
                    using (var targetStream = File.Create(targetFile))
                    {
                        await sourceStream.CopyToAsync(targetStream);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error copying models: {ex.Message}", ex);
        }
    }

    private string GetSolutionDirectory([CallerFilePath] string sourceFilePath = "")
    {
        // Navigate from the current file to the solution directory
        string? projectDir = Path.GetDirectoryName(sourceFilePath);
        if (projectDir == null)
            throw new InvalidOperationException("Cannot determine project directory");
        
        // Get the parent directory of the project directory (solution directory)
        return Path.GetDirectoryName(projectDir) ?? throw new InvalidOperationException("Cannot determine solution directory");
    }

    public async Task<OcrResult> AnalyzeImageAsync(Stream imageStream)
    {
        try
        {
            if (!_isInitialized)
                await InitializeAsync();

            using (var bitmap = SKBitmap.Decode(imageStream))
            {
                if (bitmap == null)
                {
                    throw new InvalidOperationException($"Failed to decode image stream");
                }
                return _ocrEngine.Detect(bitmap, RapidOcrOptions.Default);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"OCR analysis error: {ex.Message}", ex);
        }
    }

    public async Task<OcrResult> AnalyzeImageAsync(string imagePath)
    {
        try
        {
            if (!_isInitialized)
                await InitializeAsync();

            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException($"Image file not found at: {imagePath}");
            }

            using (var bitmap = SKBitmap.Decode(imagePath))
            {
                if (bitmap == null)
                {
                    throw new InvalidOperationException($"Failed to decode image at: {imagePath}");
                }
                return _ocrEngine.Detect(bitmap, RapidOcrOptions.Default);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"OCR analysis error: {ex.Message}", ex);
        }
    }

    public string GetOnnxRuntimeInfo()
    {
        try
        {
            var versionInfo = new StringBuilder();
            
            // ONNX Runtime information
            versionInfo.AppendLine("ONNX Runtime Information:");
            versionInfo.AppendLine($"ONNX Runtime assembly version: {typeof(Microsoft.ML.OnnxRuntime.SessionOptions).Assembly.GetName().Version}");

            // Environment information
            versionInfo.AppendLine("\nEnvironment Info:");
            versionInfo.AppendLine($"OS: {Environment.OSVersion}");
            versionInfo.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
            versionInfo.AppendLine($"Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            
            // OCR Engine status
            versionInfo.AppendLine("\nOCR Engine:");
            versionInfo.AppendLine($"Initialized: {_isInitialized}");
            if (_lastInitError != null)
            {
                versionInfo.AppendLine($"Last initialization error: {_lastInitError.Message}");
                if (_lastInitError.InnerException != null)
                {
                    versionInfo.AppendLine($"Inner error: {_lastInitError.InnerException.Message}");
                }
            }

            // Source model directory
            string sourceDir = Path.Combine(GetSolutionDirectory(), "..", "RapidOcrNet", "models");
            versionInfo.AppendLine("\nModel Locations:");
            versionInfo.AppendLine($"Source Directory: {sourceDir}");
            versionInfo.AppendLine($"Source Directory exists: {Directory.Exists(sourceDir)}");
            
            if (Directory.Exists(sourceDir))
            {
                versionInfo.AppendLine("\nSource Model Files:");
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    versionInfo.AppendLine($" - {Path.GetFileName(file)}");
                }
            }

            // Target model directory
            string targetDir = Path.Combine(FileSystem.AppDataDirectory, "models");
            versionInfo.AppendLine("\nTarget Directory:");
            versionInfo.AppendLine($"Path: {targetDir}");
            versionInfo.AppendLine($"Exists: {Directory.Exists(targetDir)}");
            
            if (Directory.Exists(targetDir))
            {
                versionInfo.AppendLine("\nTarget Model Files:");
                foreach (var file in Directory.GetFiles(targetDir))
                {
                    versionInfo.AppendLine($" - {Path.GetFileName(file)}");
                }
                
                // Check specific required files
                string detPath = Path.Combine(targetDir, "en_PP-OCRv3_det_infer_opt.onnx");
                string clsPath = Path.Combine(targetDir, "ch_ppocr_mobile_v2.0_cls_infer_opt.onnx");
                string recPath = Path.Combine(targetDir, "en_PP-OCRv3_rec_infer_opt.onnx");
                string keysPath = Path.Combine(targetDir, "en_dict.txt");
                
                versionInfo.AppendLine("\nRequired model files:");
                versionInfo.AppendLine($"Detection model exists: {File.Exists(detPath)}");
                versionInfo.AppendLine($"Classification model exists: {File.Exists(clsPath)}");
                versionInfo.AppendLine($"Recognition model exists: {File.Exists(recPath)}");
                versionInfo.AppendLine($"Dictionary file exists: {File.Exists(keysPath)}");
            }
            
            // ONNX Runtime native library information
            versionInfo.AppendLine("\nONNX Runtime Native Library:");
            var onnxRuntimeAssembly = typeof(Microsoft.ML.OnnxRuntime.SessionOptions).Assembly;
            var assemblyLocation = onnxRuntimeAssembly.Location;
            versionInfo.AppendLine($"Assembly location: {assemblyLocation}");
            
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (assemblyDir != null)
            {
                string onnxRuntimeNativePath = Path.Combine(assemblyDir, "runtimes", "osx", "native");
                versionInfo.AppendLine($"Native path: {onnxRuntimeNativePath}");
                versionInfo.AppendLine($"Native path exists: {Directory.Exists(onnxRuntimeNativePath)}");
                
                if (Directory.Exists(onnxRuntimeNativePath))
                {
                    versionInfo.AppendLine("\nNative library files:");
                    foreach (var file in Directory.GetFiles(onnxRuntimeNativePath))
                    {
                        versionInfo.AppendLine($" - {Path.GetFileName(file)}");
                    }
                }
            }
            
            // Environment variables
            versionInfo.AppendLine("\nEnvironment variables:");
            foreach (var key in Environment.GetEnvironmentVariables().Keys)
            {
                var name = key?.ToString() ?? "";
                if (name.StartsWith("DYLD_") || name.StartsWith("LD_") || name.StartsWith("PATH"))
                {
                    versionInfo.AppendLine($"  {name}: {Environment.GetEnvironmentVariable(name)}");
                }
            }

            return versionInfo.ToString();
        }
        catch (Exception ex)
        {
            return $"Error getting ONNX Runtime info: {ex.Message}";
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            if (_isInitialized)
            {
                try
                {
                    _ocrEngine.Dispose();
                }
                catch
                {
                    // Ignore any exceptions during disposal
                }
            }
            _isDisposed = true;
        }
    }
} 