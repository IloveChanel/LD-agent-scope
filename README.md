# Agent Scope

First agent for the Multi-Agent Builder. Open `LD-agent-scope.sln` in Visual Studio 2022 and press F5.

It reads a project name and description, then prints:

- what is already in the module library
- which of those modules this project still needs
- what has to be built because it is not in the library
- questions the description did not answer
- storage options, with blank placeholders for anything not connected

Unknown keys stay empty in `src/AgentScope/placeholders.json`. Do not invent a database URL or a GitHub token.

```text
dotnet run --project src/AgentScope -- "Notes" "A web notes dashboard that saves notes for me"
```
