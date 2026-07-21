# Commands Reference

## Getting started

| Command | Description |
|---------|-------------|
| `llm setup` | Guided first-time setup (workspace, runtime, model) |
| `llm setup --workspace D:\AI --install-runtime --model D:\MODEL\...` | Non-interactive; downloads Vulkan runtime |
| `llm init [path]` | Initialize workspace + select GPU/CPU/backend + runtime params |
| `llm init D:\AI --auto` | Non-interactive: recommended GPU + defaults |
| `llm init D:\AI --gpu 0 --backend vulkan --context 16384 --ngl 29` | Pin device + params |
| `llm init D:\AI --cpu` | CPU-only profile |
| `llm config show` | Show context, layers, threads, port, … |
| `llm config set Runtime:Context 8192` | Change context (restart with `llm serve --restart`) |
| `llm config set Runtime:GpuLayers 29` | Change GPU layers |
| `llm config set Runtime:Threads 8` | Change CPU threads |
| `llm config set Runtime:Port 11434` | Change API port |
| `llm serve [--restart] [--daemon] [--no-install]` | Ensure server running (auto-downloads runtime) |
| `llm cursor` | Cursor IDE connection guide |
| `llm doctor` | Full environment + API diagnostics |

## Runtime

| Command | Description |
|---------|-------------|
| `llm runtime import <path>` | Copy local llama.cpp Vulkan build into workspace |
| `llm runtime start` | Start llama-server (background) |
| `llm runtime stop` | Stop llama-server |
| `llm runtime restart` | Stop, wait, start |
| `llm runtime status` | Process + config summary |
| `llm logs [--lines N]` | Tail latest llama-server log (default 50 lines) |

## Models

| Command | Description |
|---------|-------------|
| `llm model search [query] [--recommended]` | Browse curated catalog |
| `llm model pull <id\|repo/file.gguf> [--use]` | Download from Hugging Face |
| `llm model list` | List registered models |
| `llm model scan [path] [--register]` | Find local `.gguf` files |
| `llm model add <path> [id]` | Register existing file |
| `llm model download <url> [file] [--register] [--use]` | Download from arbitrary URL |
| `llm model use <id>` | Set active model |
| `llm model remove <id>` | Remove from registry |

### Examples

```powershell
llm model search coder
llm model pull qwen2.5-coder-7b --use
llm model pull Qwen/Qwen2.5-Coder-7B-Instruct-GGUF/qwen2.5-coder-7b-instruct-q4_k_m.gguf --use
llm model list
llm model use qwen2.5-coder-7b
```

## Profiles

| Command | Description |
|---------|-------------|
| `llm profile list` | List tuning profiles |
| `llm profile show <id>` | Show profile details |
| `llm profile use <id>` | Apply GPU/context/thread settings |

Built-in: `iris-xe-coding`, `balanced`, `cpu-only`

## Chat & config

| Command | Description |
|---------|-------------|
| `llm chat [prompt] [--no-start]` | One-shot or interactive inference test |
| `llm config show` | Print user configuration |
| `llm config set <key> <value>` | Update configuration |
| `llm version` | CLI version |
| `llm help` | Command list |

## Typical workflow

```powershell
# Install globally
.\scripts\install.ps1

# First run
llm setup --workspace "D:\AI" --runtime "C:\llama.cpp"

# Or pull model from catalog
llm model pull qwen2.5-coder-7b --use
llm profile use iris-xe-coding

# Start and connect Cursor
llm serve
llm cursor
llm doctor
llm chat "Write a Python fibonacci function"
```
