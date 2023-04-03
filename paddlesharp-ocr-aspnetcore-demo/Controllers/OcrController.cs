
using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;
using Sdcb.PaddleOCR;
using System.Diagnostics;

namespace paddlesharp_ocr_aspnetcore_demo.Controllers
{
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

    public record OcrResponse(string Text, long ElapsedMs);
}
