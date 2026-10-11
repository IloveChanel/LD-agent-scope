using System.Text.Json;

namespace AgentScope;

public sealed record LibraryModule(string Id, string Name, string Category, string Note);

public sealed class ModuleCatalog
{
    public required IReadOnlyList<LibraryModule> Modules { get; init; }

    public static ModuleCatalog Load(string path)
    {
        var json = File.ReadAllText(path);
        var modules = JsonSerializer.Deserialize<List<LibraryModule>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? [];
        return new ModuleCatalog { Modules = modules };
    }
}

public sealed class ScopeReport
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> Have { get; init; }
    public required IReadOnlyList<string> Need { get; init; }
    public required IReadOnlyList<string> Build { get; init; }
    public required IReadOnlyList<string> Questions { get; init; }
    public required IReadOnlyList<string> Storage { get; init; }

    public string ToText()
    {
        var lines = new List<string>
        {
            $"Agent Scope read \"{Name}\".",
            string.IsNullOrWhiteSpace(Description) ? "Description: not written yet." : $"Description: {Description}",
            "",
            "What we have",
        };
        lines.AddRange(Have.Count == 0 ? ["- No library module matches this description yet."] : Have.Select(item => $"- {item}"));
        lines.Add("");
        lines.Add("What we need");
        lines.AddRange(Need.Select(item => $"- {item}"));
        lines.Add("");
        lines.Add("What has to be built");
        lines.AddRange(Build.Count == 0
            ? ["- Nothing new. Use a library module or pick a storage option."]
            : Build.Select(item => $"- {item}"));
        lines.Add("");
        lines.Add("Questions");
        lines.AddRange(Questions.Count == 0 ? ["- None. The description is enough for this pass."] : Questions.Select(item => $"- {item}"));
        lines.Add("");
        lines.Add("Where it can be stored");
        lines.AddRange(Storage);
        return string.Join(Environment.NewLine, lines);
    }
}

public static class ScopeAnalyzer
{
    public static ScopeReport Analyze(string name, string description, ModuleCatalog catalog, IReadOnlyList<string> alreadyOnProject)
    {
        var text = $"{name} {description}".ToLowerInvariant();
        var onProject = new HashSet<string>(alreadyOnProject.Select(item => item.ToLowerInvariant()));
        var words = text.Split([' ', ',', '.', '!', '?', '/', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 3)
            .Distinct()
            .ToArray();
        var matches = catalog.Modules.Where(module =>
        {
            var hay = $"{module.Id} {module.Name} {module.Category} {module.Note}".ToLowerInvariant();
            return words.Any(word => hay.Contains(word, StringComparison.Ordinal));
        }).Take(8).ToArray();

        var have = matches.Select(module => module.Name).ToArray();
        var need = matches
            .Where(module => !onProject.Contains(module.Name.ToLowerInvariant()))
            .Select(module => $"Add {module.Name} to this project")
            .ToArray();

        var knownWords = new HashSet<string>(matches.SelectMany(module =>
            $"{module.Id} {module.Name} {module.Category}".ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)));
        var build = words
            .Where(word => word.Length > 5 && !knownWords.Contains(word) && !Stop.Contains(word))
            .Distinct()
            .Take(4)
            .Select(word => $"Confirm whether \"{word}\" is a new module. It is not in the library.")
            .ToArray();

        return new ScopeReport
        {
            Name = name,
            Description = description,
            Have = have,
            Need = need.Length == 0 ? ["No extra library module is required yet."] : need,
            Build = build,
            Questions = QuestionsFor(text),
            Storage = StorageFor(text),
        };
    }

    private static string[] QuestionsFor(string text)
    {
        var questions = new List<string>();
        var mobile = text.Contains("mobile") || text.Contains("iphone") || text.Contains("android") || text.Contains("expo");
        var web = text.Contains("web") || text.Contains("dashboard") || text.Contains("site") || text.Contains("browser");
        if (!mobile && !web) questions.Add("Is this a website, a phone app, or both?");
        if ((text.Contains("save") || text.Contains("note") || text.Contains("user") || text.Contains("account"))
            && !text.Contains("sqlite") && !text.Contains("postgres") && !text.Contains("database"))
        {
            questions.Add("Should the data stay on this computer, or in a database you already have?");
        }
        if (!text.Contains("only me") && !text.Contains("team") && !text.Contains("customer"))
        {
            questions.Add("Who uses this besides you?");
        }
        return questions.ToArray();
    }

    private static string[] StorageFor(string text)
    {
        var options = new List<string>
        {
            "1. This computer: free JSON files. Placeholder: STORAGE_DIR=./data",
            "2. SQLite file: free, one file next to the app. Placeholder: SQLITE_PATH=",
        };
        if (text.Contains("team") || text.Contains("customer") || text.Contains("login") || text.Contains("account") || text.Contains("online"))
        {
            options.Add("3. PostgreSQL: only if you already have one. Placeholder: DATABASE_URL=");
        }
        options.Add($"{options.Count + 1}. GitHub: code can be saved after a token exists. Placeholder: GITHUB_TOKEN=");
        return options.ToArray();
    }

    private static readonly HashSet<string> Stop =
    [
        "small", "notes", "dashboard", "project", "building", "should", "their", "about", "using", "with",
    ];
}
