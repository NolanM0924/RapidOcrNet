using SkiaSharp;
using RapidOcrNet;

// Get the path to the models directory
string modelsPath = Path.Combine("..", "RapidOcrNet", "models");
string detPath = Path.Combine(modelsPath, "en_PP-OCRv3_det_infer_opt.onnx");
string clsPath = Path.Combine(modelsPath, "ch_ppocr_mobile_v2.0_cls_infer_opt.onnx");
string recPath = Path.Combine(modelsPath, "en_PP-OCRv3_rec_infer_opt.onnx");
string keysPath = Path.Combine(modelsPath, "en_dict.txt");

var ocrEngine = new RapidOcr();
ocrEngine.InitModels(detPath, clsPath, recPath, keysPath, 0);

string[] imagePaths = new[]
{
    "../RapidOcrNet.Tests/images/1997.png",
    "../RapidOcrNet.Tests/images/rotated.PNG",
    "../RapidOcrNet.Tests/images/rotated2.PNG",
    "../RapidOcrNet.Tests/images/img_10.jpg",
    "../RapidOcrNet.Tests/images/img_11.jpg",
    "../RapidOcrNet.Tests/images/img_12.jpg",
    "../RapidOcrNet.Tests/images/img_195.jpg"
};

foreach (var path in imagePaths)
{
    Console.WriteLine($"\nProcessing image: {Path.GetFileName(path)}");
    Console.WriteLine("----------------------------------------");

    if (!File.Exists(path))
    {
        Console.WriteLine($"File not found: {path}");
        continue;
    }

    using (var image = SKBitmap.Decode(path))
                        {
        var result = ocrEngine.Detect(image, RapidOcrOptions.Default);
        
        foreach (var block in result.TextBlocks)
        {
            Console.WriteLine(string.Join("", block.Chars));
                    }
                }
}

ocrEngine.Dispose();
