# Configuration

## Files

| File | Purpose |
|------|---------|
| `%LOCALAPPDATA%\LLM\config.json` | User settings (paths, runtime, tuning) |
| `%LOCALAPPDATA%\LLM\models.json` | Model registry |
| `<workspace>/profiles/*.json` | Hardware tuning profiles |

## Workspace initialization

`llm init` or `llm setup` sets `rootDirectory` and creates:

```
<workspace>/
├── runtimes/
├── models/
├── downloads/
├── cache/
├── logs/
├── plugins/
├── profiles/
└── temp/
```

Default workspace if not specified: `%USERPROFILE%\LLM`

Recommended for your laptop: `D:\AI` (keeps OS drive free on 256 GB NVMe)

## Runtime settings

| Setting | Default | Notes |
|---------|---------|-------|
| `provider` | llama.cpp | |
| `port` | 8080 | Original sprint used 9000; 8080 matches Cursor examples |
| `host` | 127.0.0.1 | |
| `gpuLayers` | 29 | Iris Xe Vulkan offload |
| `contextSize` | 16384 | |
| `threads` | 8 | Matches i5-1145G7 logical cores |
| `apiKey` | local-ai | Sent to llama-server and Cursor |

## Config commands

```powershell
llm config show

llm config set RootDirectory "D:\AI"
llm config set Runtime:Model "D:\MODEL\qwen2.5-coder-7b-instruct-q4_k_m.gguf"
llm config set Runtime:Port 8080
llm config set Runtime:GpuLayers 29
llm config set Runtime:Context 16384
llm config set Runtime:Host 127.0.0.1
```

Prefer `llm model use <id>` over `Runtime:Model` when using the registry.

## Migration from older config

If `config.json` was created before `RootDirectory` existed, delete it and re-run `llm init`, or manually add the field.

## Environment variables

None required today. Paths are resolved via config and `%LOCALAPPDATA%`.
