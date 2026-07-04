using System.Text.Json;

namespace HomeScoutCopilot.Evaluator;

/// <summary>Loads the version-controlled eval dataset (JSONL — one <see cref="EvaluationCase"/> per line).</summary>
public static class EvaluationDataset
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<EvaluationCase> Parse(IEnumerable<string> lines)
    {
        var cases = new List<EvaluationCase>();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var item = JsonSerializer.Deserialize<EvaluationCase>(trimmed, Json)
                ?? throw new FormatException($"Could not parse eval case: {trimmed}");
            cases.Add(item);
        }

        return cases;
    }

    public static IReadOnlyList<EvaluationCase> Load(string path) => Parse(File.ReadLines(path));
}
