using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LotteryTicketAppNew.Models;
using System.Collections.ObjectModel;

namespace LotteryTicketAppNew.ViewModels;

public partial class MainViewModel : ObservableObject
{
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

    public ObservableCollection<ImageData> UploadedImages { get; } = new();

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

            // TODO: Implement OCR scanning
            IsScanning = true;
            await Task.Delay(1000); // Simulate scanning
            IsScanning = false;
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

    [RelayCommand]
    private void ClearImage()
    {
        CurrentImage = null;
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
} 