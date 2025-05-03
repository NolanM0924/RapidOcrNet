using RapidOcrNet;
using SkiaSharp;
using System.Reflection;
using System.Text;

namespace bu_kids;

public partial class MainPage : ContentPage
{
	private string? _selectedImagePath;
	private FileResult? _selectedImageFileResult;
	private RapidOcr _ocrEngine;
	private bool _isOcrInitialized = false;

	public MainPage()
	{
		InitializeComponent();
		InitializeOcr();
	}

	private async void InitializeOcr()
	{
		try
		{
			StatusLabel.Text = "Initializing OCR engine...";

			// Initialize OCR engine in background task weeeeeee
			await Task.Run(() =>
			{
				_ocrEngine = new RapidOcr();
				_ocrEngine.InitModels();
				_isOcrInitialized = true;
			});

			StatusLabel.Text = "OCR engine ready!";
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Error initializing OCR: {ex.Message}", "OK");
			StatusLabel.Text = "OCR initialization failed.";
		}
	}

	private async void OnPickImageClicked(object sender, EventArgs e)
	{
		try
		{
			var options = new PickOptions
			{
				PickerTitle = "Select an image",
				FileTypes = FilePickerFileType.Images
			};

			var result = await FilePicker.PickAsync(options);
			
			if (result != null)
			{
				_selectedImageFileResult = result;
				_selectedImagePath = result.FullPath;
				
				// Display the selected image
				DisplayImage.Source = ImageSource.FromFile(_selectedImagePath);
				
				// Enable the process button
				ProcessButton.IsEnabled = true;
				
				StatusLabel.Text = "Image selected. Click 'Process Image' to analyze.";
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Error selecting image: {ex.Message}", "OK");
			StatusLabel.Text = "Image selection failed.";
		}
	}

	private async void OnProcessClicked(object sender, EventArgs e)
	{
		if (!_isOcrInitialized)
		{
			await DisplayAlert("Not Ready", "OCR engine is still initializing. Please wait.", "OK");
			return;
		}

		if (string.IsNullOrEmpty(_selectedImagePath))
		{
			await DisplayAlert("No Image", "Please select an image first.", "OK");
			return;
		}

		try
		{
			StatusLabel.Text = "Processing image...";
			ProcessButton.IsEnabled = false;
			ResultText.Text = "Processing...";

			// Process the image in background task to keep UI responsive
			OcrResult result = await Task.Run(() => 
			{
				using (var bitmap = SKBitmap.Decode(_selectedImagePath))
				{
					return _ocrEngine.Detect(bitmap, RapidOcrOptions.Default);
				}
			});

			// Display results
			ResultText.Text = result.StrRes;

			// Build detailed results
			StringBuilder details = new StringBuilder();
			details.AppendLine(result.StrRes);
			details.AppendLine();
			details.AppendLine($"Detection time: {result.DetectTime}ms");
			details.AppendLine($"Blocks detected: {result.TextBlocks.Length}");

			ResultText.Text = details.ToString();
			StatusLabel.Text = "OCR processing completed!";
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Error processing image: {ex.Message}", "OK");
			ResultText.Text = $"Error: {ex.Message}";
			StatusLabel.Text = "OCR processing failed.";
		}
		finally
		{
			ProcessButton.IsEnabled = true;
		}
	}
}

