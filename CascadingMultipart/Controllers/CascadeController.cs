using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CascadingMultipart.Controllers;

[ApiController]
[Route("cascade")]
public class CascadeController : ControllerBase
{
    private const string BearerToken = "mysecrettoken"; // Set your Bearer token here

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public CascadeController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunCascade([FromBody] List<StepDefinition> steps)
    {
        var results = new List<StepResult>();
        var previousData = new Dictionary<string, object>();

        var client = _httpClientFactory.CreateClient();
        var baseUrl = _configuration["BaseApiUrl"] ?? "http://localhost:5097"; // Set your API's base URL

        foreach (var step in steps)
        {
            // Bearer Token Check: Ensure the token is provided and valid
            if (!Request.Headers.ContainsKey("Authorization"))
                return Unauthorized("Missing Authorization header");

            var authHeader = Request.Headers["Authorization"].ToString();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Unauthorized("Invalid Authorization scheme");

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (token != BearerToken)
                return Unauthorized("Invalid token");

            // Resolve placeholders in the path
            var resolvedPath = ResolvePlaceholders(step.Path, previousData);
            var fullUrl = $"{baseUrl.TrimEnd('/')}/{resolvedPath.TrimStart('/')}";

            // Create the request message based on the method (GET, POST, etc.)
            var request = new HttpRequestMessage(new HttpMethod(step.Method), fullUrl);

            try
            {
                // Send the HTTP request
                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    results.Add(new StepResult
                    {
                        Path = resolvedPath,
                        Success = false,
                        Error = $"Status: {(int)response.StatusCode}, Body: {json}"
                    });
                    break; // Stop on failure
                }

                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                previousData = data ?? new Dictionary<string, object>();
                results.Add(new StepResult
                {
                    Path = resolvedPath,
                    Success = true,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                results.Add(new StepResult
                {
                    Path = resolvedPath,
                    Success = false,
                    Error = ex.Message
                });
                break; // Stop on failure
            }
        }

        // Return only the result of the last step
        return Ok(results.LastOrDefault());
    }

    // Helper method to resolve URL placeholders (e.g., "{$['foo']}")
    private string ResolvePlaceholders(string path, Dictionary<string, object> previous)
    {
        if (string.IsNullOrEmpty(path) || !path.Contains("{$")) return path;

        return Regex.Replace(path, @"{\$\['(.*?)'\]}", match =>
        {
            var key = match.Groups[1].Value;
            return previous.TryGetValue(key, out var value) ? value?.ToString() ?? "" : "";
        });
    }
}