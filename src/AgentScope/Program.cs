namespace AgentScope;

public static class Program
{
    public static void Main(string[] args)
    {
        var name = args.Length > 0 ? args[0] : Ask("Project name");
        var description = args.Length > 1 ? args[1] : Ask("Description of what you are building");
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Project name is required.");
            return;
        }

        var catalogPath = Path.Combine(AppContext.BaseDirectory, "modules.json");
        var catalog = ModuleCatalog.Load(catalogPath);
        var report = ScopeAnalyzer.Analyze(name.Trim(), description.Trim(), catalog, alreadyOnProject: []);
        Console.WriteLine(report.ToText());
    }

    private static string Ask(string label)
    {
        Console.Write($"{label}: ");
        return Console.ReadLine() ?? "";
    }
}
