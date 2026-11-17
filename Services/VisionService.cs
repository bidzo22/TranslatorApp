using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Options;
using TranslatorApp.Models;

namespace TranslatorApp.Services;

public class VisionOptions
{
    public string Endpoint { get; set; } = "";
    public string Key { get; set; } = "";
}

public class VisionException : Exception
{
    public VisionException(string message, Exception? inner = null) : base(message, inner) { }
}

public class VisionService
{
    private const VisualFeatures Features = VisualFeatures.Read | VisualFeatures.Objects | VisualFeatures.Tags;

    private readonly VisionOptions _options;
    private readonly ILogger<VisionService> _logger;

    public VisionService(IOptions<VisionOptions> options, ILogger<VisionService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private ImageAnalysisClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint) || string.IsNullOrWhiteSpace(_options.Key))
            throw new VisionException("Endpoint або ключ Azure AI Vision не налаштовано. Перевірте User Secrets.");

        return new ImageAnalysisClient(new Uri(_options.Endpoint), new AzureKeyCredential(_options.Key));
    }

    public Task<VisionAnalysis> AnalyzeAsync(Uri imageUrl) =>
        RunAsync(client => client.AnalyzeAsync(imageUrl, Features));

    public Task<VisionAnalysis> AnalyzeAsync(BinaryData imageData) =>
        RunAsync(client => client.AnalyzeAsync(imageData, Features));

    private async Task<VisionAnalysis> RunAsync(Func<ImageAnalysisClient, Task<Response<ImageAnalysisResult>>> call)
    {
        ImageAnalysisResult result;
        try
        {
            result = (await call(CreateClient())).Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Azure AI Vision повернув помилку {Status} {Code}", ex.Status, ex.ErrorCode);
            throw new VisionException(GetErrorMessage(ex), ex);
        }

        var analysis = new VisionAnalysis
        {
            ImageWidth = result.Metadata.Width,
            ImageHeight = result.Metadata.Height
        };

        if (result.Read != null)
        {
            var lines = result.Read.Blocks.SelectMany(b => b.Lines).Select(l => l.Text);
            analysis.Text = string.Join(Environment.NewLine, lines);
        }

        if (result.Objects != null)
        {
            foreach (var obj in result.Objects.Values)
            {
                var tag = obj.Tags.OrderByDescending(t => t.Confidence).FirstOrDefault();
                analysis.Objects.Add(new VisionObject
                {
                    Name = tag?.Name ?? "object",
                    Confidence = tag?.Confidence ?? 0,
                    X = obj.BoundingBox.X,
                    Y = obj.BoundingBox.Y,
                    Width = obj.BoundingBox.Width,
                    Height = obj.BoundingBox.Height
                });
            }
        }

        if (result.Tags != null)
        {
            analysis.Tags = result.Tags.Values
                .OrderByDescending(t => t.Confidence)
                .Select(t => new VisionTag { Name = t.Name, Confidence = t.Confidence })
                .ToList();
        }

        return analysis;
    }

    private static string GetErrorMessage(RequestFailedException ex) => ex.Status switch
    {
        401 => "Помилка авторизації: невірний ключ або Endpoint Azure AI Vision.",
        403 => "Доступ до Azure AI Vision заборонено. Перевірте ресурс і підписку.",
        429 => "Перевищено ліміт запитів до Azure AI Vision (на F0 — 20 за хвилину). Зачекайте і спробуйте знову.",
        400 => "Azure AI Vision не зміг обробити зображення. Перевірте формат (JPG/PNG/BMP/GIF/WEBP), розмір до 20 МБ і мінімум 50×50 px.",
        >= 500 => "Сервіс Azure AI Vision тимчасово недоступний. Спробуйте пізніше.",
        _ => $"Azure AI Vision повернув помилку (код {ex.Status})."
    };
}