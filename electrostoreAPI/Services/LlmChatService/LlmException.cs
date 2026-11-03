namespace ElectrostoreAPI.Services.LlmChatService;

/// <summary>
/// The LLM backend answered, timed out or kept calling tools in a way the API cannot use.
/// This is an upstream failure (502), not a conflict with the state of the API.
/// </summary>
public class LlmException : Exception
{
    public LlmException(string message) : base(message)
    {
    }

    public LlmException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
