using System.ComponentModel.DataAnnotations;

namespace TranslatorApp.Models;

public class TranslateViewModel
{
    [Required(ErrorMessage = "Введіть текст для перекладу.")]
    [StringLength(5000, ErrorMessage = "Текст не може бути довшим за 5000 символів.")]
    [Display(Name = "Текст")]
    public string Text { get; set; } = "";

    [MinLength(1, ErrorMessage = "Виберіть хоча б одну мову перекладу.")]
    public List<string> TargetLanguages { get; set; } = new();

    // Заповнюється після перекладу
    public TranslationResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<HistoryEntry> History { get; set; } = new List<HistoryEntry>();
}