namespace TranslatorApp.Models;

public class TranslationResult
{
    public string DetectedLanguageCode { get; set; } = "";
    public double DetectedScore { get; set; }
    public string DetectedLanguageName => Languages.GetName(DetectedLanguageCode);

    public List<TranslationItem> Translations { get; set; } = new();
}

public class TranslationItem
{
    public string LanguageCode { get; set; } = "";
    public string Text { get; set; } = "";
    public string LanguageName => Languages.GetName(LanguageCode);
}