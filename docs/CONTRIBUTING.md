# Contributing

## Development setup

```powershell
git clone https://github.com/snigdho48/llm-cli.git
cd llm-cli
dotnet build LLM.sln
dotnet test
```

## Branch strategy

| Branch | Purpose |
|--------|---------|
| `main` | Stable releases |
| `develop` | Active integration |
| `feature/*` | One feature per branch |

## Before submitting changes

1. `dotnet build LLM.sln` — 0 errors
2. `dotnet test` — all tests pass
3. Update `.cursor/workflow/CHANGELOG.md` under `[Unreleased]`
4. Update relevant docs in `docs/` if behavior changed
5. Register new commands in `Program.cs` and `HelpCommand.cs`

## Architecture rules

- Business logic in **services**, not commands or `Program.cs`
- **LLM.Core** — no Microsoft.Extensions dependency
- **LLM.CLI.Commands.ICommand** — flat dispatcher until router migration
- Do not hardcode machine paths in source
- Do not duplicate `ICommand` interfaces

## Commit message style

```
feat(model): add search and pull from Hugging Face catalog
docs: add ARCHITECTURE and SPEC
fix(runtime): wait for health after start
```

## Adding a command

1. Create `src/LLM.CLI/Commands/MyCommand.cs` implementing `ICommand`
2. Create service(s) in `src/LLM.CLI/Services/` if needed
3. Register in `Program.cs`: `services.AddSingleton<ICommand, MyCommand>()`
4. Add to `HelpCommand.cs`
5. Document in `docs/COMMANDS.md` and `docs/SPEC.md`
6. Add tests in `tests/LLM.Tests/`

## Testing

Prefer tests that cover real behavior:

- Service logic with temp directories (`RootDirectoryOverride` on `UserDataService`)
- Path resolution, catalog search, profile application

Skip trivial "assert true" tests.

## Cursor workflow

See `.cursor/workflow/README.md` and `.cursor/rules/llm-cli-workflow.mdc`.
