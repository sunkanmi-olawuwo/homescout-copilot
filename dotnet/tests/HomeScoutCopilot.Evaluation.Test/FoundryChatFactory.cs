using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace HomeScoutCopilot.Evaluation.Test;

/// <summary>
/// Builds the keyless <see cref="ChatConfiguration"/> that the standard-library evaluators (and our
/// bespoke judge) use as their judge model — the same Foundry <c>chat</c> deployment the copilot
/// runs on. The judge endpoint is the Foundry account root (derived from the project endpoint's
/// authority, or an explicit <c>AZURE_FOUNDRY_ACCOUNT_ENDPOINT</c>), reached with
/// <see cref="DefaultAzureCredential"/> — no keys. Returns null when Foundry isn't configured so
/// the harness can skip cleanly off the gate.
/// </summary>
public static class FoundryChatFactory
{
    public static ChatConfiguration? TryCreate()
    {
        var projectEndpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT");
        // Prefer a dedicated, higher-capability judge deployment over the copilot's generator model
        // (avoids self-judging); fall back to the generator when no judge is provisioned.
        var deployment = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_JUDGE_DEPLOYMENT")
            ?? Environment.GetEnvironmentVariable("AZURE_FOUNDRY_MODEL_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(projectEndpoint) || string.IsNullOrWhiteSpace(deployment))
        {
            return null;
        }

        // The Azure OpenAI surface lives at the AIServices account root, not the /api/projects/... path.
        var accountEndpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_ACCOUNT_ENDPOINT")
            ?? new Uri(projectEndpoint).GetLeftPart(UriPartial.Authority);

        var azureClient = new AzureOpenAIClient(new Uri(accountEndpoint), new DefaultAzureCredential());
        // The chat deployment is a gpt-5-family reasoning model; strip the temperature the built-in
        // evaluators (and any caller) set, so it isn't rejected for using a non-default value.
        IChatClient chatClient = new DefaultTemperatureChatClient(
            azureClient.GetChatClient(deployment).AsIChatClient());
        return new ChatConfiguration(chatClient);
    }
}
