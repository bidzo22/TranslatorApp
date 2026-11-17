using Microsoft.AspNetCore.Mvc;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Controllers;

public class ImageController : Controller
{
    private const long MaxFileSize = 20 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
    private static readonly string[] DefaultLanguages = { "uk", "en", "de" };

    private readonly VisionService _vision;
    private readonly TranslatorService _translator;
    private readonly BlobStorageService _blob;
    private readonly ILogger<ImageController> _logger;

    public ImageController(VisionService vision, TranslatorService translator,
                           BlobStorageService blob, ILogger<ImageController> logger)
    {
        _vision = vision;
        _translator = translator;
        _blob = blob;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() =>
        View(new ImageAnalysisViewModel { TargetLanguages = DefaultLanguages.ToList() });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Index(ImageAnalysisViewModel model, IFormFile? image)
    {
        model.TargetLanguages = (model.TargetLanguages ?? new())
            .Where(Languages.Supported.ContainsKey)
            .Distinct()
            .ToList();

        if (image == null || image.Length == 0)
            ModelState.AddModelError("image", "Виберіть зображення.");
        else if (image.Length > MaxFileSize)
            ModelState.AddModelError("image", "Файл завеликий. Максимум — 20 МБ.");
        else if (!AllowedExtensions.Contains(Path.GetExtension(image.FileName).ToLowerInvariant()))
            ModelState.AddModelError("image", "Підтримуються формати: JPG, PNG, BMP, GIF, WEBP.");

        if (model.TargetLanguages.Count == 0)
            ModelState.AddModelError(nameof(model.TargetLanguages), "Виберіть хоча б одну мову перекладу.");

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            if (_blob.IsConfigured)
            {
                model.BlobName = await _blob.UploadAsync(image!);

                var analyzeUri = _blob.GetReadSasUri(model.BlobName, TimeSpan.FromMinutes(10));
                model.Analysis = await _vision.AnalyzeAsync(analyzeUri);

                model.ImageUrl = _blob.GetReadSasUri(model.BlobName, TimeSpan.FromHours(1)).ToString();
            }
            else
            {
                using var ms = new MemoryStream();
                await image!.CopyToAsync(ms);
                var bytes = ms.ToArray();

                model.Analysis = await _vision.AnalyzeAsync(BinaryData.FromBytes(bytes));
                model.ImageUrl = $"data:{image.ContentType};base64,{Convert.ToBase64String(bytes)}";
            }
        }
        catch (VisionException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Помилка Blob Storage");
            model.ErrorMessage = "Не вдалося зберегти зображення в Azure Blob Storage. Перевірте Connection String.";
            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }

        var text = model.Analysis!.Text.Trim();
        if (text.Length > 0)
        {
            if (text.Length > 5000) text = text[..5000];
            try
            {
                model.Translation = await _translator.TranslateAsync(text, model.TargetLanguages);
            }
            catch (TranslatorException ex)
            {
                model.TranslationError = ex.Message;
            }
        }

        return View(model);
    }
}
