# LLM CLI

Run a local AI coding assistant on your Windows PC — no cloud API key required.

LLM CLI sets up **llama.cpp**, downloads models, picks your GPU (Intel / NVIDIA / AMD), and starts an **OpenAI-compatible server** you can use with **Cursor** and other tools.

---

## What you get

- One command to start a local chat/API server (`llm serve`)
- Works with **Intel Iris Xe** (Vulkan), NVIDIA (CUDA), AMD (ROCm), or CPU
- Download models from Hugging Face
- Connect Cursor to `http://127.0.0.1:11434/v1` (or your chosen port)
- Simple settings: context length, GPU layers, threads, port

---

## Requirements

- Windows 10/11 (64-bit)
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (Desktop or Runtime)
- About **16–32 GB RAM** for a 7B coding model (Q4)
- Optional: Intel Graphics driver with Vulkan (for Iris Xe)

---

## Install

### Option A — GitHub Release (recommended)

1. Download the latest **`llm-cli-*-win-x64.zip`** from  
   [Releases](https://github.com/snigdho48/llm-cli/releases)
2. Extract the zip
3. In PowerShell, from that folder:

```powershell
.\install.ps1
```

4. Open a **new** terminal, then check:

```powershell
llm version
llm help
```

### Option B — From this repo

```powershell
.\scripts\install.ps1
```

To uninstall the global `llm` command later:

```powershell
.\scripts\uninstall.ps1
```

---

## First-time setup (5 minutes)

Pick a folder for AI files (models, runtime, logs), for example `D:\AI`.

### Guided setup (easiest)

```powershell
llm setup --workspace "D:\AI" --install-runtime
```

This will:

1. Create the workspace folders  
2. Ask which **GPU or CPU** to use (or auto-pick)  
3. Download a Windows **Vulkan** llama.cpp build if you don’t have one  
4. Optionally register a model path when prompted  

If you already have a `.gguf` model:

```powershell
llm setup --workspace "D:\AI" --install-runtime --model "D:\MODEL\your-model.gguf" --no-start
```

### Step by step

```powershell
# 1) Create workspace + choose GPU/CPU + set context / layers / port
llm init "D:\AI"

# 2) Get llama.cpp (auto-download) OR import your own build
llm runtime install
# llm runtime import "C:\path\to\llama.cpp\build"

# 3) Get a model
llm model search coder
llm model pull qwen2.5-coder-7b --use
# or use a file you already have:
# llm model add "D:\MODEL\qwen2.5-coder-7b-instruct-q4_k_m.gguf" --use

# 4) Start the server
llm serve

# 5) Test it
llm chat "Say hello in one sentence"
llm doctor
```

On `init`, press **Enter** on any question to keep the recommended default  
(context length, GPU layers, threads, port, etc.).

---

## Everyday use

### Start / stop the AI server

| What you want | Command |
|---------------|---------|
| Start server + show API URL | `llm serve` |
| Restart after changing settings | `llm serve --restart` |
| Start at Windows logon | `llm serve --daemon` |
| Don’t auto-download runtime | `llm serve --no-install` |
| Status | `llm runtime status` |
| Stop | `llm runtime stop` |
| Recent logs | `llm logs` |

When `serve` succeeds you’ll see something like:

```text
OpenAI API : http://127.0.0.1:11434/v1
API Key    : local-ai
```

### Chat

```powershell
llm chat "Explain recursion simply"
llm chat          # interactive mode
```

### Models

| Command | Meaning |
|---------|---------|
| `llm model search coder` | Browse suggested models |
| `llm model search qwen --live` | Search Hugging Face |
| `llm model pull qwen2.5-coder-7b --use` | Download and activate |
| `llm model list` | Models you already registered |
| `llm model use <id>` | Switch active model |
| `llm model add "D:\path\file.gguf"` | Register a local file |

After changing the model, restart:

```powershell
llm serve --restart
```

### GPU / CPU

| Command | Meaning |
|---------|---------|
| `llm gpu list` | Show GPUs and backends |
| `llm gpu use 0 --backend vulkan --apply-profile` | Select GPU 0 + Vulkan |
| `llm gpu auto` | Auto-pick GPU + profile |
| `llm gpu doctor` | Intel: check Vulkan / oneAPI |
| `llm gpu fix --vulkan` | Try to install Vulkan Runtime via winget |

### Settings (context, layers, port, …)

View:

```powershell
llm config show
```

Change (then restart the server):

```powershell
llm config set Runtime:Context 16384
llm config set Runtime:GpuLayers 29
llm config set Runtime:Threads 8
llm config set Runtime:Port 11434
llm serve --restart
```

| Setting | What it does | Typical value |
|---------|----------------|---------------|
| `Runtime:Context` | How much text the model can “see” | `8192` or `16384` |
| `Runtime:GpuLayers` | How much of the model runs on GPU | `29` (Iris Xe), `0` = CPU |
| `Runtime:Threads` | CPU threads | `8` |
| `Runtime:Port` | Local API port | `11434` (if 8080 is taken) |
| `Runtime:GpuBackend` | `vulkan` / `cuda` / `rocm` / `cpu` | `vulkan` on Intel |

You can also switch a full preset:

```powershell
llm profile list
llm profile use iris-xe-coding
llm profile use cpu-only
```

---

## Use with Cursor

1. Start the server: `llm serve`
2. Show connection details: `llm cursor`  
   Or write a settings snippet: `llm cursor --write`
3. In Cursor, point the OpenAI-compatible base URL to:

```text
http://127.0.0.1:11434/v1
```

API key (default): `local-ai`

If your port is different, use the URL printed by `llm serve`.

---

## Quick troubleshooting

| Problem | Try this |
|---------|----------|
| `llm` not found | Open a **new** terminal after `install.ps1`, or check PATH |
| Server won’t start | `llm doctor` then `llm logs` |
| Port already in use | `llm config set Runtime:Port 11434` then `llm serve --restart` |
| Slow / no GPU | `llm gpu list` → `llm gpu use 0 --backend vulkan --apply-profile` |
| Intel Vulkan issues | `llm gpu doctor` → `llm gpu fix --vulkan` |
| Wrong context / layers | `llm config show` → set values → `llm serve --restart` |
| No model | `llm model pull qwen2.5-coder-7b --use` |

---

## Recommended model (32 GB laptop)

For coding on Intel Iris Xe–class machines:

```powershell
llm model pull qwen2.5-coder-7b --use
llm serve --restart
```

More model notes: [docs/MODELS.md](docs/MODELS.md)

---

## More help

```powershell
llm help
```

| Guide | For |
|-------|-----|
| [Commands](docs/COMMANDS.md) | Full command list |
| [Configuration](docs/CONFIGURATION.md) | Config file location & keys |
| [Release / install zip](docs/RELEASE.md) | How releases are built |
| [Contributing](docs/CONTRIBUTING.md) | Developers building from source |

---

## License

MIT — see [LICENSE](LICENSE)

**Repo:** https://github.com/snigdho48/llm-cli  
**Releases:** https://github.com/snigdho48/llm-cli/releases
