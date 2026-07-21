# Roadmap

Vision: **Git + Docker + Ollama** for local AI — installable via `winget install llm-cli` / `brew install llm-cli`.

See `ARCHITECTURE.md` for system design and `SPEC.md` for the CLI contract.

---

## Phase map (original sprint plan → status)

| Phase | Feature | Status |
|-------|---------|--------|
| Sprint 0 | Architecture + roadmap docs | ✅ Done |
| Phase 1 | `llm init` — workspace wizard | ✅ Done |
| Phase 2 | `llm runtime install` — download releases | 🔄 Partial (`runtime import` today) |
| Phase 3 | `llm runtime start/stop/restart/status` | ✅ Done |
| Phase 4 | `llm model search/pull/list/remove` | ✅ Done |
| Phase 5 | `llm chat` + `llm serve` + OpenAI API | ✅ Done |
| Phase 6 | Plugin system | ⏳ v2.1 |
| v1.0 | Production release + global installer | ✅ Done (`scripts/install.ps1`) |

---

## Version history

| Version | Feature |
|---------|---------|
| v0.1 | CLI foundation, DI, config, logging |
| v0.2 | Runtime manager, config persistence, status |
| v0.3 | Model registry (list, scan, add, use, remove) |
| v0.4 | Doctor, `/health` check, Cursor hints |
| v1.0 | Global `llm` installer |
| v1.1 | Setup wizard |
| v1.2 | Hardware profiles (Iris Xe, balanced, CPU-only) |
| v1.3 | Chat command |
| v1.4 | Cursor guide |
| v1.5 | Model download (URL) |
| v1.6 | Runtime logging + `llm logs` |
| **v1.7** | **Model search + pull (HF catalog)** |
| **v1.8** | **`llm serve`** |

---

## Next

### v1.9 — Cursor auto-config
- `llm cursor --write` — patch Cursor settings.json
- Detect Cursor install on Windows

### v2.0 — Runtime install from GitHub
- `llm runtime install llama.cpp` — download Vulkan build for win-x64
- Version pinning: `runtimes/llama.cpp/v{version}/`
- `llm runtime upgrade`

### v2.1 — Plugin system
- Load from `<workspace>/plugins`
- Hooks: on-init, on-start, on-stop, pre-chat

### v2.2 — Benchmark
- `llm bench` — tokens/sec, suggest optimal GpuLayers
- Compare CPU vs Vulkan on Iris Xe

### v2.3 — Package managers
- `winget install llm-cli`
- `brew install llm-cli`

### v2.4 — Core router migration
- `LLM.Core.Routing.CommandRouter` replaces flat dispatcher

---

## Hardware target

HP EliteBook 840 G8 · i5-1145G7 · 32 GB · Iris Xe · Qwen2.5-Coder 7B Q4_K_M

Primary model catalog ID: `qwen2.5-coder-7b`

```powershell
llm model pull qwen2.5-coder-7b --use
llm serve
llm cursor
```
