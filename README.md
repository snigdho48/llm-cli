# LLM CLI

Local AI runtime manager for Windows — manage llama.cpp, models, and inference from one CLI.

## Quick start (recommended)

```powershell
dotnet build LLM.sln
dotnet test

# One-shot guided setup
dotnet run --project src/LLM.CLI -- setup --workspace "D:\AI" --runtime "C:\llama.cpp" --model "D:\MODEL\qwen2.5-coder-7b-instruct-q4_k_m.gguf"

# Or step by step:
dotnet run --project src/LLM.CLI -- init "D:\AI"
dotnet run --project src/LLM.CLI -- runtime import "C:\llama.cpp"
dotnet run --project src/LLM.CLI -- model add "D:\MODEL\qwen2.5-coder-7b-instruct-q4_k_m.gguf" qwen
dotnet run --project src/LLM.CLI -- profile use iris-xe-coding
dotnet run --project src/LLM.CLI -- model use qwen
dotnet run --project src/LLM.CLI -- runtime start
dotnet run --project src/LLM.CLI -- doctor
dotnet run --project src/LLM.CLI -- cursor
dotnet run --project src/LLM.CLI -- chat "Explain recursion in one paragraph"
```

## Commands

| Command | Description |
|---------|-------------|
| `setup` | Guided first-time setup wizard |
| `init [path]` | Create workspace directories |
| `cursor` | Cursor IDE connection guide |
| `profile list/show/use` | Hardware tuning profiles |
| `chat [prompt]` | Test inference / interactive chat |
| `logs [--lines N]` | Tail llama-server logs |
| `model download <url>` | Download GGUF with progress |
| `config show` | Show user configuration |
| `config set <key> <value>` | Update configuration |
| `runtime import <path>` | Copy llama.cpp Release build into workspace |
| `runtime start` | Start llama-server (background) |
| `runtime stop` | Stop llama-server |
| `runtime restart` | Restart llama-server |
| `runtime status` | Show process and config status |
| `model list` | List registered models |
| `model scan [path] [--register]` | Discover GGUF files |
| `model add <path> [id]` | Register a model |
| `model use <id>` | Set active model |
| `model remove <id>` | Remove a model |
| `doctor` | Environment and API diagnostics |
| `version` | CLI version |

## Config keys

- `RootDirectory` — workspace root
- `Runtime:Model` — path to active `.gguf` model
- `Runtime:Port` — server port (default 8080)
- `Runtime:GpuLayers` — GPU offload layers (29 for Iris Xe)
- `Runtime:Context` — context size (16384)
- `Runtime:Host` — bind address (127.0.0.1)

## Project layout

```
docs/               Architecture, SPEC, commands, models, API
src/
  LLM.CLI/          Commands, services, configuration
  LLM.Core/         Routing foundation
  LLM.Runtime/      Runtime abstractions (future)
tests/
  LLM.Tests/
.cursor/
  workflow/         Changelog + dev guide
  rules/            Cursor agent rules
scripts/            install.ps1, uninstall.ps1
```

## Production readiness

This project uses **harness engineering** (eval-driven development) and **loop engineering** (continuous verification):

```powershell
# Full eval harness (build + test + smoke)
.\scripts\harness.ps1

# Continuous loop until pass^3 (release gate)
.\scripts\loop-verify.ps1 -IntervalMinutes 5

# Release build
.\scripts\release.ps1 -Version 1.0.0
```

See [PRODUCTION_PLAN.md](docs/PRODUCTION_PLAN.md) for the full phased plan.

## Documentation

| Doc | Description |
|-----|-------------|
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | System design and vision |
| [SPEC.md](docs/SPEC.md) | CLI contract (single source of truth) |
| [COMMANDS.md](docs/COMMANDS.md) | Full command reference |
| [MODELS.md](docs/MODELS.md) | Model recommendations for your hardware |
| [PRODUCTION_PLAN.md](docs/PRODUCTION_PLAN.md) | Harness/loop engineering + release phases |
| [RELEASE.md](docs/RELEASE.md) | v1.0 release checklist |

## Global install (Windows)

```powershell
.\scripts\install.ps1
# Then in a new terminal:
llm init "D:\AI"
llm doctor
```

To remove the shim: `.\scripts\uninstall.ps1`

## Repository

https://github.com/snigdho48/llm-cli
