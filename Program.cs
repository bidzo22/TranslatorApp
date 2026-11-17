using TranslatorApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.Configure<TranslatorOptions>(builder.Configuration.GetSection("AzureTranslator"));
builder.Services.AddHttpClient<TranslatorService>(c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddSingleton<TranslationHistoryService>();

builder.Services.Configure<VisionOptions>(builder.Configuration.GetSection("AzureVision"));
builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection("BlobStorage"));
builder.Services.AddSingleton<VisionService>();
builder.Services.AddSingleton<BlobStorageService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();