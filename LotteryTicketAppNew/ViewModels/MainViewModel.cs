using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LotteryTicketAppNew.Models;
using LotteryTicketAppNew.Services;
using System.Collections.ObjectModel;
using System.Text;

namespace LotteryTicketAppNew.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly OcrService _ocrService;

    [ObservableProperty]
    private ImageData? currentImage;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string title = "Lottery Ticket Scanner";

    [ObservableProperty]
    private bool isScanning;

    [ObservableProperty]
    private ObservableCollection<string> detectedText = new();

    public ObservableCollection<ImageData> UploadedImages { get; } = new();

    public MainViewModel(OcrService ocrService)
    {
        _ocrService = ocrService;
    }

    [RelayCommand]
    private async Task PickAndUploadImage()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = "Selecting image...";

            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select Image",
                FileTypes = FilePickerFileType.Images
            });

            if (result == null)
            {
                StatusMessage = "No image selected";
                return;
            }

            StatusMessage = "Loading image...";
            
            // Read the image file
            using var stream = await result.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            
            var imageData = new ImageData
            {
                ImagePath = result.FullPath,
                ImageBytes = memoryStream.ToArray(),
                UploadDate = DateTime.Now
            };

            CurrentImage = imageData;
            UploadedImages.Add(imageData);
            StatusMessage = "Image loaded successfully";

            // Perform OCR 
            await ScanTextAsync(imageData.ImagePath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ScanTextAsync(string imagePath)
    {
        try
        {
            DetectedText.Clear();
            IsScanning = true;
            StatusMessage = "Scanning text...";
            
            // Initialize OCR engine if needed
            var ocrResult = await _ocrService.AnalyzeImageAsync(imagePath);
            
            foreach (var block in ocrResult.TextBlocks)
            {
                string text = string.Join("", block.Chars);
                DetectedText.Add(text);
            }
            
            if (DetectedText.Count == 0)
            {
                StatusMessage = "No text detected";
            }
            else
            {
                StatusMessage = $"Detected {DetectedText.Count} text blocks";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scanning error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task SaveResultsAsync()
    {
        if (DetectedText.Count == 0)
        {
            await Shell.Current.DisplayAlert("No Results", "There are no OCR results to save.", "OK");
            return;
        }

        try
        {
            StringBuilder sb = new StringBuilder();
            foreach (var text in DetectedText)
            {
                sb.AppendLine(text);
            }

            string content = sb.ToString();
            string defaultName = $"OCR_Result_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Text = content,
                Title = "OCR Results",
                Subject = defaultName
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to save results: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void ClearImage()
    {
        CurrentImage = null;
        DetectedText.Clear();
        StatusMessage = string.Empty;
    }

    private bool IsBusy
    {
        get => IsLoading;
        set
        {
            IsLoading = value;
            PickAndUploadImageCommand.NotifyCanExecuteChanged();
        }
    }

    public void Dispose()
    {
        _ocrService.Dispose();
    }
} 