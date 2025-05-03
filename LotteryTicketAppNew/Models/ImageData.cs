namespace LotteryTicketAppNew.Models;

public class ImageData
{
    public string ImagePath { get; set; } = string.Empty;
    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
    public DateTime UploadDate { get; set; }

    public ImageData()
    {
        ImagePath = string.Empty;
        ImageBytes = Array.Empty<byte>();
        UploadDate = DateTime.Now;
    }
} 