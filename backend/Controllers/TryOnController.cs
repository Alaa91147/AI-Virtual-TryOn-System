using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/try-on")]
public sealed class TryOnController(
    IHttpClientFactory httpClientFactory,
    ILogger<TryOnController> logger) : ControllerBase
{
    private const long MaxImageBytes = 6 * 1024 * 1024;
    private const long MaxResultBytes = 20 * 1024 * 1024;

    private static readonly HashSet<string> ImageTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(13 * 1024 * 1024)]
    public async Task<IActionResult> Generate(
        [FromForm] IFormFile? person,
        [FromForm] IFormFile? garment,
        CancellationToken cancellationToken)
    {
        if (!ValidImage(person) || !ValidImage(garment))
        {
            return BadRequest(new
            {
                message = "Provide JPG, PNG, or WebP person and garment images, each under 6 MB."
            });
        }

        using var form = new MultipartFormDataContent();
        await using var personStream = person!.OpenReadStream();
        await using var garmentStream = garment!.OpenReadStream();
        using var personContent = new StreamContent(personStream);
        using var garmentContent = new StreamContent(garmentStream);

        personContent.Headers.ContentType = new MediaTypeHeaderValue(person.ContentType);
        garmentContent.Headers.ContentType = new MediaTypeHeaderValue(garment.ContentType);
        form.Add(personContent, "person", "person" + ImageExtension(person.ContentType));
        form.Add(garmentContent, "garment", "garment" + ImageExtension(garment.ContentType));

        try
        {
            using var client = httpClientFactory.CreateClient("TryOnAi");
            using var response = await client.PostAsync("try-on", form, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    logger.LogWarning("CatVTON rejected the backend service key.");
                    return StatusCode(502, new
                    {
                        message = "The AI connection is not configured correctly. Check the backend service key."
                    });
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return StatusCode((int)response.StatusCode, new
                {
                    message = ReadAiError(body)
                });
            }

            if (!string.Equals(response.Content.Headers.ContentType?.MediaType,
                    "image/png", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(502, new
                {
                    message = "The AI service returned an unexpected result. Check the tunnel connection."
                });
            }

            if (response.Content.Headers.ContentLength is > MaxResultBytes)
            {
                return StatusCode(502, new { message = "The AI result is too large." });
            }

            var image = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (image.Length == 0 || image.LongLength > MaxResultBytes)
            {
                return StatusCode(502, new { message = "The AI service returned an invalid image." });
            }

            Response.Headers.CacheControl = "no-store";
            return File(image, "image/png");
                    }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Could not reach CatVTON.");

            return StatusCode(503, new
            {
                message = "The AI service is offline. Check Colab and the tunnel URL."
            });
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                message = "Try-on took too long. Please try again."
            });
        }
    }

    private static bool ValidImage(IFormFile? file) =>
        file is { Length: > 0 and <= MaxImageBytes }
        && ImageTypes.Contains(file.ContentType);

    private static string ImageExtension(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            _ => ".png"
        };

    private static string ReadAiError(string body)
    {
        const string fallback =
            "The AI service could not generate the image.";

        try
        {
            using var json = JsonDocument.Parse(body);

            if (json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty("detail", out var detail)
                && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString() ?? fallback;
            }
        }
        catch (JsonException)
        {
            // Tunnel errors can be HTML rather than JSON.
        }

        return fallback;
    }
}