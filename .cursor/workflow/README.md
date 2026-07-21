# LLM CLI — Cursor Workflow

This folder tracks how the project is developed inside Cursor.

## Branch strategy

| Branch | Purpose |
|--------|---------|
| `main` | Stable releases |
| `develop` | Active integration |
| `feature/*` | One feature per branch |

## Daily workflow

1. Pull latest `develop`.
2. Create `feature/<name>` from `develop`.
3. Implement in small commits that always build.
4. Run `dotnet build` and `dotnet test` before pushing.
5. Merge back to `develop` via PR.

## Local testing (Windows)

```powershell
cd C:\Users\Snigdho\Documents\llm\llm-cli-foundation-v0.1-fixed

dotnet build LLM.sln
dotnet test

# Initialize workspace
dotnet run --project src/LLM.CLI -- init "D:\AI"

# Import your local llama.cpp build
dotnet run --project src/LLM.CLI -- runtime import "C:\llama.cpp"

# Configure model (your tuned settings)
dotnet run --project src/LLM.CLI -- model add "D:\MODEL\qwen2.5-coder-7b-instruct-q4_k_m.gguf" qwen
dotnet run --project src/LLM.CLI -- model use qwen
dotnet run --project src/LLM.CLI -- config set Runtime:Port 8080
dotnet run --project src/LLM.CLI -- config set Runtime:GpuLayers 29

# Start / stop / restart
dotnet run --project src/LLM.CLI -- runtime start
dotnet run --project src/LLM.CLI -- runtime status
dotnet run --project src/LLM.CLI -- runtime restart
dotnet run --project src/LLM.CLI -- runtime stop
dotnet run --project src/LLM.CLI -- doctor
```

## Configuration locations

| Item | Path |
|------|------|
| User settings | `%LOCALAPPDATA%\LLM\config.json` |
| Model registry | `%LOCALAPPDATA%\LLM\models.json` |
| Workspace root | Set via `llm init` (e.g. `D:\AI`) |
| Managed runtime | `<workspace>\runtimes\llama.cpp\current\` |
| Models | User-defined path or `<workspace>\models\` |

## Hardware profile (Snigdho's laptop)

- CPU: Intel i5-1145G7
- GPU: Intel Iris Xe (Vulkan)
- Recommended: `-ngl 29`, `-c 16384`, `-t 8`
- Model: Qwen2.5-Coder 7B Q4_K_M

## Milestones

- [x] v0.1 — Solution, DI, commands, config persistence
- [x] v0.2 — Workspace init, runtime import/start/stop
- [x] v0.3 — Model registry (`model list`, `model scan`, `model add/use/remove`)
- [x] v0.4 — OpenAI-compatible health check + Cursor integration (`doctor`, `/health`)
- [x] v1.0 — Global `llm` command installer (`scripts/install.ps1`)
- [x] v1.1–v1.6 — Setup wizard, profiles, chat, cursor guide, model download, logs
- [x] v1.7 — Model search + pull (Hugging Face catalog)
- [x] v1.8 — `llm serve`

Shipped features through v1.0+ are covered in the root `README.md` and `docs/`.

## Agent rules

See `.cursor/rules/llm-cli-workflow.mdc` for project-specific Cursor rules.
