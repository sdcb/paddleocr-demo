using Gradio.Net;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;
using System.Diagnostics;
using System.Reflection;

namespace Sdcb.PaddleSharp.AspNetDemo;

public class Program
{
    public static void Main(string[] args)
    {
        //Environment.SetEnvironmentVariable("GLOG_v", "2");
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddGradio();
        builder.Services.AddSingleton<OcrManager>();
        builder.Services.AddSingleton<FileService>();
        WebApplication webApplication = builder.Build();
        webApplication.UseGradio(CreateBlocks(webApplication.Services), c =>
        {
            c.Stylesheets = [
                "https://fonts.font.im/css2?family=Source+Sans+Pro:wght@400;600&display=swap",
                "https://fonts.font.im/css2?family=IBM+Plex+Mono:wght@400;600&display=swap"
                ];
        });
        webApplication.Run();
    }

    static Blocks CreateBlocks(IServiceProvider sp)
    {
        using Blocks blocks = gr.Blocks();

        gr.Markdown("## PaddleSharp OCR Demo");

        string[] knownDevices = [nameof(PaddleDevice.Mkldnn), nameof(PaddleDevice.Openblas), nameof(PaddleDevice.Onnx), nameof(PaddleDevice.Gpu)];
        string[] knownModels = [.. typeof(LocalFullModels).GetProperties(BindingFlags.Public | BindingFlags.Static).Select(p => p.Name).SkipLast(1)];
        Radio devicesRadio;
        Dropdown modelsDropdown;
        using (gr.Row())
        {
            devicesRadio = gr.Radio(knownDevices, knownDevices[0], label: "Device");
            modelsDropdown = gr.Dropdown(knownModels, knownModels[0], label: "Model");
        }

        Image inputImage;
        Checkbox r180ClsEnabledCheckedbox;
        Number detectorMaxSizeNumber, detectorDilateSizeNumber;
        using (gr.Row())
        {
            inputImage = gr.Image();
            using (gr.Column())
            {
                r180ClsEnabledCheckedbox = gr.Checkbox(label: "Enable r180_cls", value: false);
                detectorMaxSizeNumber = gr.Number(label: "Detector Max Size", minimum: 64, maximum: 4096, step: 64, value: 1536);
                detectorDilateSizeNumber = gr.Number(label: "Detector Dilate Size", minimum: 0, maximum: 8, step: 1, value: 2);
            }
        }

        Button inferButton = gr.Button("Run");

        Image outputImage;
        Number elapsedNumber;
        Textbox textbox;
        using (gr.Row())
        {
            outputImage = gr.Image(interactive: false, scale: 1);
            using (gr.Column())
            {
                elapsedNumber = gr.Number(label: "Elapsed(ms)", interactive: false);
                textbox = gr.Textbox("Output", lines: 10, interactive: false);
            }
        }

        inferButton.Click(fn: async c =>
        {
            string? image = Image.Payload(c.Data[2]);
            if (image != null)
            {
                (Mat dest, PaddleOcrResult result, int elapsed) = Infer(
                    Radio.Payload(c.Data[0]).Single(), 
                    Dropdown.Payload(c.Data[1]).Single(), 
                    image, 
                    Checkbox.Payload(c.Data[3]),
                    (int)Number.Payload(c.Data[4])!.Value,
                    (int)Number.Payload(c.Data[5])!.Value,
                    sp);
                using var _ = dest;
                string destFile = await sp.GetRequiredService<FileService>().SaveFile($"ocr-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.jpg", dest.ToBytes(".jpg"));
                return gr.Output(destFile, result.Text, elapsed);
            }
            else
            {
                return gr.Output(null!, "", 0);
            }
        }, [devicesRadio, modelsDropdown, inputImage, r180ClsEnabledCheckedbox, detectorMaxSizeNumber, detectorDilateSizeNumber], [outputImage, textbox, elapsedNumber]);

        using (gr.Row())
        {
            gr.Markdown("""
		    ### Github: 
		    * https://github.com/sdcb/PaddleSharp
		    * https://github.com/sdcb/paddleocr-demo
		    """);

            gr.Markdown("""		
		    ### QQ群: 579060605
		    """);
        }

        return blocks;
    }

    static (Mat dest, PaddleOcrResult result, int elapsed) Infer(string engine, string modelText, string srcImageFile, 
        bool enabled180Cls,
        int detectorMaxSize,
        int detectorDilateSize,
        IServiceProvider sp)
    {
        OcrManager om = sp.GetRequiredService<OcrManager>();
        QueuedPaddleOcrAll all = om[engine, modelText];

        using Mat src = Cv2.ImRead(srcImageFile, ImreadModes.Color);
        Stopwatch sw = Stopwatch.StartNew();
        PaddleOcrResult result = all.Run(src, configure: all =>
        {
            all.Detector.MaxSize = detectorMaxSize;
            all.Detector.DilatedSize = detectorDilateSize;
            all.Enable180Classification = enabled180Cls;
            all.Detector.UnclipRatio = 1.2f;
        }).GetAwaiter().GetResult();
        long elapsed = sw.ElapsedMilliseconds;
        Mat dest = PaddleOcrDetector.Visualize(src, result.Regions.Select(x => x.Rect).ToArray(), Scalar.Red, thickness: 1);

        return (dest, result, (int)elapsed);
    }
}
