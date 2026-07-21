# LLM CLI Architecture

## Vision

LLM CLI is a cross-platform AI runtime manager inspired by **Git**, **Docker**, and **Ollama**.

Goal: install once (`winget install llm-cli` / `brew install llm-cli`) and manage your entire local AI coding stack from one command.

```
"This feels like Git + Docker + Ollama."
```

### Product goals

- Manage local AI runtimes (llama.cpp first)
- Manage local AI models (search, pull, register, activate)
- Run local inference with hardware-aware profiles
- Expose OpenAI-compatible APIs for Cursor / VS Code
- Support multiple runtime providers (future)
- Offline-first, plugin-friendly, production-ready

---

## System layers

```
LLM CLI
│
├── CLI              Commands, dispatcher, help
├── Core             Routing foundation (LLM.Core)
├── Runtime          Provider abstractions (llama.cpp, future ONNX/MLX)
├── Configuration    User config + workspace layout
├── Download Manager Model/runtime downloads
├── Model Manager    Registry, catalog, scan
├── Storage          Workspace directories
├── Diagnostics      Doctor, health checks
└── Plugins          Future extension hooks
```

---

## Runtime providers

| Provider | Status | Notes |
|----------|--------|-------|
| llama.cpp | Active | Vulkan build for Intel Iris Xe |
| ONNX Runtime | Planned | |
| MLX | Planned | macOS |
| vLLM | Planned | Server GPU farms |
| TensorRT-LLM | Planned | NVIDIA |

---

## Directory layout

User settings (metadata only):

| OS | Path |
|----|------|
| Windows | `%LOCALAPPDATA%\LLM\config.json` |
| Linux | `~/.config/llm/config.json` |

Workspace root (user-chosen via `llm init`, e.g. `D:\AI`):

```
<workspace>/
├── runtimes/llama.cpp/current/   Managed runtime (copied, not symlinked)
├── models/                       Optional local model store
├── downloads/                    Pulled GGUF files
├── cache/                        Future cache
├── logs/                         llama-server stdout/stderr
├── plugins/                      Future extensions
├── profiles/                     Hardware tuning profiles (JSON)
└── temp/                         Temporary files
```

Model registry: `%LOCALAPPDATA%\LLM\models.json`

---

## Configuration principles

- **User config** stores paths and settings only — not model weights
- **Workspace** stores runtimes, downloads, logs, profiles
- **Never hardcode** machine-specific paths in source (`C:\llama.cpp`, `D:\MODEL`)
- **Never mutate** `appsettings.json` at runtime

---

## Hardware target (primary dev machine)

| Component | Value |
|-----------|-------|
| Laptop | HP EliteBook 840 G8 |
| CPU | Intel i5-1145G7 (4C/8T, AVX2) |
| GPU | Intel Iris Xe (80 EU, Vulkan) |
| RAM | 32 GB DDR4 |
| Storage | 256 GB NVMe |

### Inference strategy

On Iris Xe laptops, **well-tuned CPU inference is often competitive** with GPU offload because LLMs are memory-bandwidth bound and Iris Xe shares system RAM (~50 GB/s vs hundreds of GB/s on discrete GPUs).

LLM CLI supports both approaches via profiles:

| Profile | GPU Layers | Context | Use when |
|---------|------------|---------|----------|
| `iris-xe-coding` | 29 | 16384 | Vulkan build tuned for Qwen-Coder |
| `balanced` | 20 | 12288 | Memory pressure / stability |
| `cpu-only` | 0 | 8192 | Maximum compatibility |

**Recommended model:** Qwen2.5-Coder 7B Instruct Q4_K_M (~8–15 tok/s on this hardware).

---

## OpenAI-compatible API

llama-server exposes:

```
POST http://127.0.0.1:8080/v1/chat/completions
GET  http://127.0.0.1:8080/health
```

Default API key: `local-ai`

Used by Cursor, Continue, and `llm chat`.

---

## Command flow

```
llm setup / init
    → workspace + profiles

llm runtime import
    → copy llama.cpp Release build

llm model pull qwen2.5-coder-7b --use
    → download + register + activate

llm serve
    → start llama-server, print OpenAI URL

llm cursor
    → IDE connection guide
```

---

## Principles

- Cross-platform (Windows-first today)
- Provider-agnostic runtime layer
- Offline-first
- Modular services + DI
- Plugin-friendly workspace layout
- Production-ready error messages and doctor checks

See also: `SPEC.md`, `COMMANDS.md`, `MODELS.md`, `RELEASE.md`.
