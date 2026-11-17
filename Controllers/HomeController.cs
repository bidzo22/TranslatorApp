using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Controllers;

public class HomeController : Controller
{
    private static readonly string[] DefaultLanguages = { "en", "de", "pl" };

    private readonly TranslatorService _translator;
    private readonly TranslationHistoryService _history;

    public HomeController(TranslatorService translator, TranslationHistoryService history)
    {
        _translator = translator;
        _history = history;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = new TranslateViewModel
        {
            TargetLanguages = DefaultLanguages.ToList(),
            History = _history.GetAll()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(TranslateViewModel model)
    {
        model.Result = null;
        model.ErrorMessage = null;

        model.TargetLanguages = (model.TargetLanguages ?? new())
            .Where(Languages.Supported.ContainsKey)
            .Distinct()
            .ToList();

        if (model.TargetLanguages.Count == 0 &&
            ModelState.GetFieldValidationState(nameof(model.TargetLanguages)) != ModelValidationState.Invalid)
        {
            ModelState.AddModelError(nameof(model.TargetLanguages), "Виберіть хоча б одну мову перекладу.");
        }

        if (!ModelState.IsValid)
        {
            model.History = _history.GetAll();
            return View(model);
        }

        var text = model.Text.Trim();

        try
        {
            var result = await _translator.TranslateAsync(text, model.TargetLanguages);
            model.Result = result;
            _history.Add(new HistoryEntry { SourceText = text, Result = result });
        }
        catch (TranslatorException ex)
        {
            model.ErrorMessage = ex.Message;
        }

        model.History = _history.GetAll();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ClearHistory()
    {
        _history.Clear();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}