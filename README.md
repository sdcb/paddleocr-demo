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

## How to troubleshooting after deploy to IIS?
1. Enable IIS log by change web.config file:
   (By change `stdoutLogEnabled="false"` to `"true"` in `web.config`)
   ![image](https://user-images.githubusercontent.com/1317141/234284668-d437ea2f-4346-4f81-a62c-93baf84723f3.png)
2. Restart IIS website, a logs folder will shown in root folder:
   ![image](https://user-images.githubusercontent.com/1317141/234285326-a14e656c-ece8-48da-b0d9-be0a04fcaf91.png)
3. Trigger the error in website and **turn off** website, check the error log files:
   ![image](https://user-images.githubusercontent.com/1317141/234285772-6bda5a81-a222-4ca5-8fc8-ebcd94e4f09f.png)
   ![image](https://user-images.githubusercontent.com/1317141/234285964-2c0a4026-c4bb-47c8-94a6-33eb9138a628.png)
4. In my case, it's caused by CUDA PATH not set correctly, I can simply set by change using following web.config file:
   ![image](https://user-images.githubusercontent.com/1317141/234286347-5d0dfc0b-9c96-459c-a0df-f11b4af0af73.png)
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <location path="." inheritInChildApplications="false">
       <system.webServer>
         <handlers>
           <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
         </handlers>
         <aspNetCore processPath="dotnet" arguments=".\paddlesharp-ocr-aspnetcore-demo.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
           <environmentVariables>
             <environmentVariable name="PATH" value="%PATH%;C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v10.2\bin" />
           </environmentVariables>
         </aspNetCore>
       </system.webServer>
     </location>
   </configuration>
   ```
   (Your case might be different, but it's a good start to troubleshooting)
