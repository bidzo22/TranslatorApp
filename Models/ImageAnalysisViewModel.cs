namespace TranslatorApp.Models;

public class ImageAnalysisViewModel
{
    public List<string> TargetLanguages { get; set; } = new();

    
    public string? ImageUrl { get; set; }
    public string? BlobName { get; set; }
    public VisionAnalysis? Analysis { get; set; }
    public TranslationResult? Translation { get; set; }
    public string? TranslationError { get; set; }
    public string? ErrorMessage { get; set; }
}

public class VisionAnalysis
{
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public string Text { get; set; } = "";
    public List<VisionObject> Objects { get; set; } = new();
    public List<VisionTag> Tags { get; set; } = new();
}

public class VisionObject
{
    public string Name { get; set; } = "";
    public double Confidence { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class VisionTag
{
    public string Name { get; set; } = "";
    public double Confidence { get; set; }
}