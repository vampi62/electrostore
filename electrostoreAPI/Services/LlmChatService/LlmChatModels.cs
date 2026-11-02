using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElectrostoreAPI.Services.LlmChatService;

public class LlmMessage
{
    public required string role { get; set; } // "system" | "user" | "assistant" | "tool"
    public string? content { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<LlmToolCall>? tool_calls { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? tool_call_id { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? name { get; set; }
}

public class LlmToolCall
{
    // Some backends (native Ollama) may leave the id out, a tool result still needs one to be paired with its call
    public string id { get; set; } = $"call_{Guid.NewGuid():N}"[..13];
    public string type { get; set; } = "function";
    public required LlmToolCallFunction function { get; set; }
}

public class LlmToolCallFunction
{
    public required string name { get; set; }

    // JSON text of the arguments: OpenAI-compatible backends send it as a string, native Ollama as an object
    [JsonConverter(typeof(LlmToolArgumentsConverter))]
    public string arguments { get; set; } = "{}";
}

/// <summary>Reads the arguments of a tool call whether the backend sends them as a JSON string or as a JSON object.</summary>
public class LlmToolArgumentsConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var text = reader.GetString();
                return string.IsNullOrWhiteSpace(text) ? "{}" : text;
            case JsonTokenType.Null:
                return "{}";
            default:
                using (var document = JsonDocument.ParseValue(ref reader))
                {
                    return document.RootElement.GetRawText();
                }
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

public class LlmToolDefinition
{
    public string type { get; set; } = "function";
    public required LlmFunctionDefinition function { get; set; }
}

public class LlmFunctionDefinition
{
    public required string name { get; set; }
    public string? description { get; set; }
    public object? parameters { get; set; } // JSON schema object
}

public class LlmChatResult
{
    public string? content { get; set; }
    public List<LlmToolCall>? tool_calls { get; set; }
    public string? finish_reason { get; set; }
}

/// <summary>Per-call settings, a null value falls back to the configuration (Llm:Think, Llm:Temperature).</summary>
public class LlmChatOptions
{
    /// <summary>Let a reasoning model think before answering: slower and much longer, off by default.</summary>
    public bool? Think { get; set; }
    public double? Temperature { get; set; }
}

// OpenAI-compatible answer ("choices") or native Ollama answer ("message" at the root)
public class LlmChatCompletionResponse
{
    public List<LlmChatCompletionChoice> choices { get; set; } = new();
    public LlmMessage? message { get; set; }
    public string? done_reason { get; set; }
}

public class LlmChatCompletionChoice
{
    public LlmMessage? message { get; set; }
    public string? finish_reason { get; set; }
}

// OpenAI-compatible chunk ("choices[].delta") or native Ollama chunk ("message" at the root)
public class LlmChatCompletionChunk
{
    public List<LlmChatCompletionChunkChoice> choices { get; set; } = new();
    public LlmChatCompletionDelta? message { get; set; }
    public bool done { get; set; }
}

public class LlmChatCompletionChunkChoice
{
    public LlmChatCompletionDelta? delta { get; set; }
    public string? finish_reason { get; set; }
}

public class LlmChatCompletionDelta
{
    public string? content { get; set; }
}
