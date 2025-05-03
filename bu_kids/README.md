# BU Kids OCR App

This is a cross-platform MAUI application that utilizes the RapidOcrNet OCR engine to detect and extract text from images.

## Features

- Select images from your device's storage
- Process images using the RapidOcrNet OCR engine
- Display detected text and processing details
- Works on Android, iOS, MacOS, and Windows

## Prerequisites

- .NET 8.0 SDK or later
- For Android: Android SDK
- For iOS/MacOS: Xcode and a macOS machine
- For Windows: Windows 10 version 1809 or higher

## Setup

1. Clone the repository
2. Make sure the models directory contains the necessary OCR model files
3. Build and run the application

## Usage

1. Launch the app
2. Click on "Pick an Image" to select an image from your device
3. Once an image is selected, click on "Process Image"
4. The detected text will appear in the OCR Results section

## Notes

- The OCR engine initialization may take a few seconds on startup
- The app uses the default OCR settings from RapidOcrNet
- Large images may take longer to process

## Troubleshooting

If you encounter issues:

- Make sure the model files are correctly copied to the application output directory
- Check that the app has the necessary permissions to access the file system
- On mobile devices, ensure storage permissions are granted to the app

## Credits

This app uses the RapidOcrNet library, which is a .NET implementation of the RapidOCR project.
