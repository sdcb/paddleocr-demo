using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;
using Sdcb.PaddleOCR.Models;
using System.Reflection;

namespace Sdcb.PaddleSharp.AspNetDemo;

public class OcrManager : IDisposable
{
    Dictionary<string, PaddleOcrAll> _ocrs = [];

    public PaddleOcrAll this[string engine, string modelText]
    {
        get
        {
            string key = $"{engine}-{modelText}";
            if (!_ocrs.ContainsKey(key))
            {
                FullOcrModel model = (FullOcrModel)typeof(LocalFullModels).GetProperty(modelText, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
                PaddleOcrAll all = new(model, engine switch
                {
                    nameof(PaddleDevice.Mkldnn) => PaddleDevice.Mkldnn(),
                    nameof(PaddleDevice.Openblas) => PaddleDevice.Openblas(),
                    nameof(PaddleDevice.Onnx) => PaddleDevice.Onnx(),
                    nameof(PaddleDevice.Gpu) => PaddleDevice.Gpu(),
                    _ => throw new NotSupportedException()
                });

                _ocrs[key] = all;
            }
            return _ocrs[key];
        }
    }

    public void Dispose()
    {
        foreach (PaddleOcrAll ocr in _ocrs.Values)
        {
            ocr.Dispose();
        }
    }
}
