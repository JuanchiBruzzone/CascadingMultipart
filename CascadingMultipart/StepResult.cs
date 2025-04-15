namespace CascadingMultipart;

public class StepResult
{
    public string? Path { get; set; }
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
}