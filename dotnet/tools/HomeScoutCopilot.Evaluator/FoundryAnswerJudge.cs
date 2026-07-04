using Azure.AI.Projects;
using Azure.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace HomeScoutCopilot.Evaluator;

/// <summary>
/// The live LLM judge: a tool-less Foundry agent (same <c>AsAIAgent</c> path the copilot uses)
/// prompted with the <see cref="AnswerJudge"/> rubric. Scores a copilot answer via a real model
/// call; parsing is delegated to the pure, offline-tested <see cref="AnswerJudge.Parse"/>.
/// </summary>
public sealed class FoundryAnswerJudge
{
    private readonly AIAgent _judge;

    public FoundryAnswerJudge(string projectEndpoint, string modelDeploymentName, TokenCredential credential)
    {
        _judge = new AIProjectClient(new Uri(projectEndpoint), credential)
            .AsAIAgent(
                model: modelDeploymentName,
                name: "HomeScout answer judge",
                instructions: AnswerJudge.Instructions,
                tools: new List<AITool>());
    }

    public async Task<JudgeScore?> JudgeAsync(string query, string answer, CancellationToken cancellationToken = default)
    {
        var response = await _judge.RunAsync(AnswerJudge.BuildInput(query, answer), cancellationToken: cancellationToken);
        return AnswerJudge.Parse(response.Text);
    }
}
