using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;
using Sdcb.PaddleOCR.Models;
using System.Reflection;

namespace Sdcb.PaddleSharp.AspNetDemo;

public class OcrManager : IDisposable
{
    private readonly Dictionary<string, QueuedPaddleOcrAll> _ocrs = [];

    public QueuedPaddleOcrAll this[string engine, string modelText]
    {
        get
        {
            string key = $"{engine}-{modelText}";
            lock(_ocrs)
            {
                if (!_ocrs.TryGetValue(key, out QueuedPaddleOcrAll? value))
                {
                    Console.WriteLine($"Create {engine} - {modelText} in thread #{Environment.CurrentManagedThreadId}");
                    FullOcrModel model = (FullOcrModel)typeof(LocalFullModels).GetProperty(modelText, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
                    value = new QueuedPaddleOcrAll(() => new(model, engine switch
                    {
                        nameof(PaddleDevice.Mkldnn) => PaddleDevice.Mkldnn(cacheCapacity: 10, glogEnabled: true),
                        nameof(PaddleDevice.Openblas) => PaddleDevice.Blas(glogEnabled: true),
                        nameof(PaddleDevice.Onnx) => PaddleDevice.Onnx(glogEnabled: true),
                        nameof(PaddleDevice.Gpu) => PaddleDevice.Gpu(glogEnabled: true),
                        _ => throw new NotSupportedException()
                    }));
                    _ocrs[key] = value;
                }
                Console.WriteLine($"Get {engine} - {modelText} in thread #{Environment.CurrentManagedThreadId}");
                return value;
            }
        }
    }

    public void Dispose()
    {
        foreach (QueuedPaddleOcrAll ocr in _ocrs.Values)
        {
            ocr.Dispose();
        }
    }
}
