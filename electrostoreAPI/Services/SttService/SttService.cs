using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ElectrostoreAPI.Services.SttService;

public class SttService : ISttService
{
    public const string HttpClientName = "Stt";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SttService> _logger;

    public SttService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<SttService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsEnabled => _configuration.GetValue<bool>("Stt:Enable");

    public async Task<string> TranscribeAsync(IFormFile audioFile, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("STT integration is disabled");
        }
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var model = _configuration.GetValue<string>("Stt:Model") ?? "whisper-1";
        var apiKey = _configuration.GetValue<string>("Stt:ApiKey");

        using var content = new MultipartFormDataContent();
        await using var fileStream = audioFile.OpenReadStream();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrEmpty(audioFile.ContentType) ? "application/octet-stream" : audioFile.ContentType);
        content.Add(streamContent, "file", string.IsNullOrEmpty(audioFile.FileName) ? "audio" : audioFile.FileName);
        content.Add(new StringContent(model), "model");

        using var request = new HttpRequestMessage(HttpMethod.Post, "audio/transcriptions") { Content = content };
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error while calling the STT transcription endpoint");
            throw;
        }
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("STT transcription request failed with status {Status}: {Body}", response.StatusCode, body);
            throw new HttpRequestException($"STT transcription request failed with status {(int)response.StatusCode}");
        }
        var result = await response.Content.ReadFromJsonAsync<SttTranscriptionResponse>(cancellationToken: cancellationToken);
        return result?.text ?? string.Empty;
    }
}

internal class SttTranscriptionResponse
{
    public string? text { get; set; }
}
