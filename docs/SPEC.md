# LLM CLI — Specification

Single source of truth for CLI behavior, configuration, and conventions.

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Unhandled error / crash |

Command handlers print `[FAIL]` messages for user errors without crashing the process.

---

## Commands

See `COMMANDS.md` for full reference.

Top-level commands: `setup`, `init`, `serve`, `doctor`, `config`, `runtime`, `model`, `profile`, `chat`, `cursor`, `logs`, `help`, `version`.

Nested commands receive args after the top-level name (flat dispatcher):

```
llm runtime start  →  RuntimeCommand receives ["start"]
```

---

## Configuration schema

File: `%LOCALAPPDATA%\LLM\config.json`

```json
{
  "rootDirectory": "D:\\AI",
  "activeProfileId": "iris-xe-coding",
  "runtime": {
    "provider": "llama.cpp",
    "version": "",
    "executablePath": "D:\\AI\\runtimes\\llama.cpp\\current\\llama-server.exe",
    "sourceBuildPath": "C:\\llama.cpp",
    "activeModelPath": "D:\\AI\\downloads\\qwen2.5-coder-7b-instruct-q4_k_m.gguf",
    "port": 8080,
    "host": "127.0.0.1",
    "gpuLayers": 29,
    "contextSize": 16384,
    "threads": 8,
    "apiKey": "local-ai",
    "modelsDirectory": "models"
  }
}
```

### Config keys (`llm config set`)

| Key | Type | Default |
|-----|------|---------|
| `RootDirectory` | path | `%USERPROFILE%\LLM` |
| `Runtime:Model` | path | — |
| `Runtime:Port` | int | 8080 |
| `Runtime:Host` | string | 127.0.0.1 |
| `Runtime:GpuLayers` | int | 29 |
| `Runtime:Context` | int | 16384 |
| `Runtime:Provider` | string | llama.cpp |
| `Runtime:ModelsDirectory` | string | models |

---

## Model registry schema

File: `%LOCALAPPDATA%\LLM\models.json`

```json
{
  "defaultModelId": "qwen2.5-coder-7b",
  "models": [
    {
      "id": "qwen2.5-coder-7b",
      "name": "Qwen2.5-Coder 7B Instruct Q4_K_M",
      "path": "D:\\AI\\downloads\\qwen2.5-coder-7b-instruct-q4_k_m.gguf",
      "sizeBytes": 4687891234,
      "addedAt": "2026-07-21T10:00:00+00:00"
    }
  ]
}
```

---

## Profile schema

File: `<workspace>/profiles/<id>.json`

Built-in profiles created by `llm init`:

- `iris-xe-coding` — ngl 29, ctx 16384
- `balanced` — ngl 20, ctx 12288
- `cpu-only` — ngl 0, ctx 8192

---

## Runtime import contract

`llm runtime import <path>` accepts:

- llama.cpp repo root (`C:\llama.cpp`)
- Release folder (`...\build\bin\Release`)

Copies required files into `<workspace>\runtimes\llama.cpp\current\`:

```
llama-server.exe, llama-server-impl.dll, llama.dll,
llama-common.dll, mtmd.dll, ggml.dll, ggml-base.dll,
ggml-cpu.dll, ggml-vulkan.dll
```

Updates `runtime.executablePath` in config. **Never symlinks** to source build.

---

## Model catalog & pull

`llm model search [query]` — curated list filtered by id/name/tags.

`llm model pull <reference>` accepts:

- Catalog id: `qwen2.5-coder-7b`
- Full HF path: `Qwen/Qwen2.5-Coder-7B-Instruct-GGUF/qwen2.5-coder-7b-instruct-q4_k_m.gguf`

Downloads to `<workspace>/downloads/`, registers in models.json.

HF URL format:
```
https://huggingface.co/{repo}/resolve/main/{filename}
```

---

## OpenAI API (consumer contract)

Base URL: `http://127.0.0.1:8080/v1`

Headers:
```
Authorization: Bearer local-ai
Content-Type: application/json
```

Chat completions:
```
POST /v1/chat/completions
{
  "model": "local",
  "messages": [{"role": "user", "content": "..."}],
  "temperature": 0.2,
  "max_tokens": 512
}
```

Health:
```
GET /health
```

---

## Logging

- CLI log: `logs/llm.log` (relative to cwd when running `dotnet run`)
- Runtime log: `<workspace>/logs/llama-server-{timestamp}.log`

---

## Error handling conventions

- User-facing errors: `[FAIL] {message}` + suggested next command
- Success markers: `[ OK ]`
- Doctor checks: `[ OK ]` / `[FAIL]` per check

---

## Plugin contract (future v2.1)

Plugins live in `<workspace>/plugins/`. Hooks:

- `on-init`
- `on-runtime-start`
- `on-runtime-stop`
- `pre-chat`

Not yet implemented.

---

## Runtime provider interface (future v2.0)

```csharp
interface IRuntimeProvider
{
    string Name { get; }
    Task InstallAsync(...);
    Task StartAsync(...);
    Task StopAsync(...);
    RuntimeStatus GetStatus();
}
```

Implemented today via `RuntimeService` (llama.cpp concrete). Abstraction in `LLM.Runtime.LlamaCpp` is placeholder.
