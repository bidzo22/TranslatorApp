using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TranslatorApp.Models;

namespace TranslatorApp.Services;

public class TranslatorOptions
{
    public string Key { get; set; } = "";
    public string Region { get; set; } = "";
    public string Endpoint { get; set; } = "https://api.cognitive.microsofttranslator.com/";
}

public class TranslatorException : Exception
{
    public TranslatorException(string message, Exception? inner = null) : base(message, inner) { }
}

public class TranslatorService
{
    private readonly HttpClient _http;
    private readonly TranslatorOptions _options;
    private readonly ILogger<TranslatorService> _logger;

    public TranslatorService(HttpClient http, IOptions<TranslatorOptions> options, ILogger<TranslatorService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TranslationResult> TranslateAsync(string text, IEnumerable<string> targetLanguages)
    {
        if (string.IsNullOrWhiteSpace(_options.Key))
            throw new TranslatorException("Ключ Azure Translator не налаштовано. Перевірте User Secrets.");

       
        var query = "translate?api-version=3.0&" +
                    string.Join("&", targetLanguages.Select(l => "to=" + Uri.EscapeDataString(l)));
        var url = new Uri(new Uri(_options.Endpoint), query);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _options.Key);
        if (!string.IsNullOrWhiteSpace(_options.Region))
            request.Headers.Add("Ocp-Apim-Subscription-Region", _options.Region);
        request.Content = JsonContent.Create(new[] { new Dictionary<string, string> { ["Text"] = text } });

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Не вдалося з'єднатися з Azure Translator");
            throw new TranslatorException("Не вдалося з'єднатися з Azure Translator. Перевірте інтернет та Endpoint.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Тайм-аут запиту до Azure Translator");
            throw new TranslatorException("Azure Translator не відповів вчасно. Спробуйте ще раз.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Azure Translator повернув {Status}: {Body}", (int)response.StatusCode, body);
                throw new TranslatorException(GetErrorMessage(response.StatusCode));
            }

            var items = await response.Content.ReadFromJsonAsync<List<ApiTranslateItem>>();
            var item = items?.FirstOrDefault()
                       ?? throw new TranslatorException("Azure Translator повернув порожню відповідь.");

            return new TranslationResult
            {
                DetectedLanguageCode = item.DetectedLanguage?.Language ?? "",
                DetectedScore = item.DetectedLanguage?.Score ?? 0,
                Translations = item.Translations
                    .Select(t => new TranslationItem { LanguageCode = t.To, Text = t.Text })
                    .ToList()
            };
        }
    }

    private static string GetErrorMessage(HttpStatusCode code) => code switch
    {
        HttpStatusCode.Unauthorized => "Помилка авторизації: невірний ключ або регіон Azure Translator.",
        HttpStatusCode.Forbidden => "Доступ до Azure Translator заборонено. Перевірте ресурс і підписку.",
        HttpStatusCode.TooManyRequests => "Перевищено ліміт запитів до Azure Translator. Зачекайте і спробуйте знову.",
        HttpStatusCode.BadRequest => "Azure Translator не зміг обробити запит. Перевірте текст і вибрані мови.",
        _ when (int)code >= 500 => "Сервіс Azure Translator тимчасово недоступний. Спробуйте пізніше.",
        _ => $"Azure Translator повернув помилку (код {(int)code})."
    };

    private class ApiTranslateItem
    {
        public ApiDetectedLanguage? DetectedLanguage { get; set; }
        public List<ApiTranslation> Translations { get; set; } = new();
    }

    private class ApiDetectedLanguage
    {
        public string Language { get; set; } = "";
        public double Score { get; set; }
    }

    private class ApiTranslation
    {
        public string Text { get; set; } = "";
        public string To { get; set; } = "";
    }
}