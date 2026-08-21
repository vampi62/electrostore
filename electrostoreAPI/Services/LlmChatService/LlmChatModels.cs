namespace ElectrostoreAPI.Services.LlmChatService;

public class LlmMessage
{
    public required string role { get; set; } // "system" | "user" | "assistant" | "tool"
    public string? content { get; set; }
    public List<LlmToolCall>? tool_calls { get; set; }
    public string? tool_call_id { get; set; }
    public string? name { get; set; }
}

public class LlmToolCall
{
    public required string id { get; set; }
    public string type { get; set; } = "function";
    public required LlmToolCallFunction function { get; set; }
}

public class LlmToolCallFunction
{
    public required string name { get; set; }
    public string arguments { get; set; } = "{}";
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

public class LlmChatCompletionResponse
{
    public List<LlmChatCompletionChoice> choices { get; set; } = new();
}

public class LlmChatCompletionChoice
{
    public LlmMessage? message { get; set; }
    public string? finish_reason { get; set; }
}

public class LlmChatCompletionChunk
{
    public List<LlmChatCompletionChunkChoice> choices { get; set; } = new();
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
