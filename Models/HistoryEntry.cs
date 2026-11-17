namespace TranslatorApp.Models;

public class HistoryEntry
{
    public DateTime Time { get; set; } = DateTime.Now;
    public string SourceText { get; set; } = "";
    public TranslationResult Result { get; set; } = new();
}