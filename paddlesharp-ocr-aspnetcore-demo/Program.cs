using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.LocalV3;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton(s =>
{
    Action<PaddleConfig> device = builder.Configuration["PaddleDevice"] == "GPU" ? PaddleDevice.Gpu() : PaddleDevice.Mkldnn();
    return new QueuedPaddleOcrAll(() => new PaddleOcrAll(LocalFullModels.ChineseV3, device)
    {
        Enable180Classification = true,
        AllowRotateDetection = true,
    }, consumerCount: 1);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
