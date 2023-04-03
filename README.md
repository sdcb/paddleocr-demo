# paddlesharp-ocr-aspnetcore-demo

This is a fast demo to the upstream repository: [PaddleSharp](https://github.com/sdcb/PaddleSharp)'s PaddleOCR.

You can try it here: [https://paddlesharp-ocr-demo.starworks.cc:88/](https://paddlesharp-ocr-demo.starworks.cc:88/) (if my website still works).

## How to integrate:

In your service builder code, register a QueuedPaddleOcrAll Singleton:
```csharp
builder.Services.AddSingleton(s =>
{
    Action<PaddleConfig> device = builder.Configuration["PaddleDevice"] == "GPU" ? PaddleDevice.Gpu() : PaddleDevice.Mkldnn();
    return new QueuedPaddleOcrAll(() => new PaddleOcrAll(LocalFullModels.ChineseV3, device)
    {
        Enable180Classification = true,
        AllowRotateDetection = true,
    }, consumerCount: 1);
});
```

In your controller, use the registered `QueuedPaddleOcrAll` singleton:
```csharp
public class OcrController : Controller
{
    private readonly QueuedPaddleOcrAll _ocr;

    public OcrController(QueuedPaddleOcrAll ocr) { _ocr = ocr; }

    [Route("ocr")]
    public async Task<OcrResponse> Ocr(IFormFile file)
    {
        using MemoryStream ms = new();
        using Stream stream = file.OpenReadStream();
        stream.CopyTo(ms);
        using Mat src = Cv2.ImDecode(ms.ToArray(), ImreadModes.Color);
        double scale = 1;
        using Mat scaled = src.Resize(Size.Zero, scale, scale);

        Stopwatch sw = Stopwatch.StartNew();
        string textResult = (await _ocr.Run(scaled)).Text;
        sw.Stop();

        return new OcrResponse(textResult, sw.ElapsedMilliseconds);
    }
}
```