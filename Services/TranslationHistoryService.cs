using TranslatorApp.Models;

namespace TranslatorApp.Services;

public class TranslationHistoryService
{
    private const int MaxItems = 50;
    private readonly List<HistoryEntry> _items = new();
    private readonly object _lock = new();

    public void Add(HistoryEntry entry)
    {
        lock (_lock)
        {
            _items.Insert(0, entry); 
            if (_items.Count > MaxItems)
                _items.RemoveAt(_items.Count - 1);
        }
    }

    public IReadOnlyList<HistoryEntry> GetAll()
    {
        lock (_lock) return _items.ToList();
    }

    public void Clear()
    {
        lock (_lock) _items.Clear();
    }
}