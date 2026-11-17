namespace TranslatorApp.Models;

public static class Languages
{
    public static readonly IReadOnlyDictionary<string, string> Supported = new Dictionary<string, string>
    {
        ["uk"] = "Українська",
        ["en"] = "Англійська",
        ["de"] = "Німецька",
        ["fr"] = "Французька",
        ["es"] = "Іспанська",
        ["it"] = "Італійська",
        ["pl"] = "Польська",
        ["cs"] = "Чеська",
        ["pt"] = "Португальська",
        ["tr"] = "Турецька",
        ["ja"] = "Японська",
        ["ko"] = "Корейська",
        ["zh-Hans"] = "Китайська (спрощена)"
    };

    private static readonly Dictionary<string, string> Extra = new()
    {
        ["ru"] = "Російська",
        ["be"] = "Білоруська",
        ["nl"] = "Нідерландська",
        ["sk"] = "Словацька",
        ["ro"] = "Румунська",
        ["hu"] = "Угорська"
    };

    public static string GetName(string code)
    {
        if (Supported.TryGetValue(code, out var name)) return name;
        if (Extra.TryGetValue(code, out var extra)) return extra;
        return code;
    }
}