using System.Text.Json;

namespace HomeScoutCopilot.Evaluator;

/// <summary>
/// Loads the version-controlled multi-turn eval dataset (JSONL — one <see cref="MultiTurnCase"/> per
/// line, each with an ordered <c>turns</c> array and the <c>expectFinalContains</c> marker).
/// </summary>
public static class MultiTurnDataset
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<MultiTurnCase> Parse(IEnumerable<string> lines)
    {
        var cases = new List<MultiTurnCase>();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var item = JsonSerializer.Deserialize<MultiTurnCase>(trimmed, Json)
                ?? throw new FormatException($"Could not parse multi-turn case: {trimmed}");

            if (item.Turns.Count < 2)
            {
                throw new FormatException($"Multi-turn case '{item.Id}' needs at least two turns to test context-carry.");
            }

            cases.Add(item);
        }

        return cases;
    }

    public static IReadOnlyList<MultiTurnCase> Load(string path) => Parse(File.ReadLines(path));
}
