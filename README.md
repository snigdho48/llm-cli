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

- Windows 10/11
- Self-contained Release **exe** / zip (no separate .NET install)
- Optional: up-to-date GPU driver (Vulkan for Intel, NVIDIA Game Ready / Studio, AMD Adrenalin)

Pick the download that matches your CPU:

| Your PC | Download |
|---------|----------|
| Normal 64-bit Windows (most laptops) | `llm-*-win-x64.exe` or `…-win-x64.zip` |
| 32-bit Windows | `llm-*-win-x86.exe` / zip |
| Windows on ARM (Snapdragon / Copilot+ PC) | `llm-*-win-arm64.exe` / zip |

---

## Recommended settings by RAM

Use these as a starting point. Prefer **Q4_K_M** GGUF files (good quality / size balance).  
After changing settings: `llm serve --restart`.

### Quick table

| System RAM | Preferred model | Context | GPU layers | CPU threads | Notes |
|------------|-----------------|---------|------------|-------------|-------|
| **4 GB** | Tiny / avoid if possible | `2048` | `0` (CPU) | `2–4` | Barely usable; close other apps |
| **8 GB** | `qwen2.5-coder-3b` | `4096` | `0–15` | `4` | Light coding only |
| **16 GB** | `qwen2.5-coder-3b` or small 7B | `8192` | see GPU below | `4–6` | Comfortable for 3B; 7B is tight |
| **32 GB** | **`qwen2.5-coder-7b`** ⭐ | `16384` | see GPU below | `6–8` | Best everyday coding setup |
| **64 GB** | 7B–14B / DeepSeek Lite | `16384–32768` | see GPU below | `8–12` | Room for larger context & models |

### 4 GB RAM

| Setting | Value |
|---------|-------|
| Model | Smallest you can find (≤ ~2 GB GGUF). Not recommended for Cursor coding. |
| Context | `2048` |
| GPU layers | `0` |
| Threads | `2`–`4` |
| Profile | `cpu-only` |

```powershell
llm config set Runtime:Context 2048
llm config set Runtime:GpuLayers 0
llm config set Runtime:Threads 4
llm profile use cpu-only
```

### 8 GB RAM

| Setting | Value |
|---------|-------|
| Model | `qwen2.5-coder-3b` (~2 GB) |
| Context | `4096` |
| GPU layers | Integrated GPU: `10–15` · No GPU: `0` |
| Threads | `4` |
| Profile | `cpu-only` or `balanced` |

```powershell
llm model pull qwen2.5-coder-3b --use
llm config set Runtime:Context 4096
llm config set Runtime:GpuLayers 0
llm config set Runtime:Threads 4
```

### 16 GB RAM

| Setting | Value |
|---------|-------|
| Model | Prefer `qwen2.5-coder-3b` for speed, or `qwen2.5-coder-7b` if you close heavy apps |
| Context | `8192` (use `4096` if the PC starts swapping) |
| GPU layers | Intel iGPU: `20` · Discrete NVIDIA/AMD: `35–99` · CPU only: `0` |
| Threads | `4`–`6` |
| Profile | `balanced` |

```powershell
llm model pull qwen2.5-coder-3b --use
llm config set Runtime:Context 8192
llm config set Runtime:GpuLayers 20
llm config set Runtime:Threads 6
llm profile use balanced
```

### 32 GB RAM (recommended)

| Setting | Value |
|---------|-------|
| Model | **`qwen2.5-coder-7b`** (Q4_K_M) — primary pick |
| Alternatives | `qwen3-coder-8b`, `codellama-7b`, `mistral-7b` |
| Context | `16384` |
| GPU layers | Intel Iris Xe: **`29`** · Discrete GPU: **`99`** · CPU only: `0` |
| Threads | `8` |
| Profile | `iris-xe-coding` / `nvidia-cuda` / `amd-rocm` / `cpu-only` |

```powershell
llm model pull qwen2.5-coder-7b --use
llm config set Runtime:Context 16384
llm config set Runtime:GpuLayers 29
llm config set Runtime:Threads 8
llm profile use iris-xe-coding
llm serve --restart
```

### 64 GB RAM

| Setting | Value |
|---------|-------|
| Model | `qwen2.5-coder-7b` (fast) or larger (e.g. DeepSeek-Coder Lite / 14B Q4) |
| Context | `16384`–`32768` |
| GPU layers | Discrete: `99` · Intel iGPU: `29`–`35` · CPU: `0` |
| Threads | `8`–`12` |
| Profile | Vendor profile, then raise context |

```powershell
llm model pull qwen2.5-coder-7b --use
llm config set Runtime:Context 32768
llm config set Runtime:Threads 12
llm serve --restart
```

---

## Recommended settings by CPU cores

`Runtime:Threads` should usually be **about half to ¾ of your logical cores**, not all of them (leave headroom for Windows + Cursor).

| Logical cores (Task Manager) | Suggested `Runtime:Threads` |
|------------------------------|-----------------------------|
| 2–4 | `2`–`4` |
| 6–8 | `4`–`6` |
| 8–12 | `6`–`8` |
| 12–16 | `8`–`12` |
| 16+ | `12`–`16` |

```powershell
llm config set Runtime:Threads 8
```

On `llm init`, press **Enter** to keep the suggested thread count.

---

## Recommended settings by GPU

| GPU | Backend | `GpuLayers` | Profile | Notes |
|-----|---------|-------------|---------|-------|
| **Intel Iris Xe / UHD** (laptop) | `vulkan` | `20`–`29` | `iris-xe-coding` | Shared memory with RAM; don’t set layers too high |
| **Intel Arc** | `vulkan` | `35`–`99` | `iris-xe-coding` or custom | Stronger than Iris Xe |
| **NVIDIA** (GTX/RTX) | `cuda` (or `vulkan`) | `99` | `nvidia-cuda` | Best speed when CUDA build is present |
| **AMD** (Radeon) | `rocm` or `vulkan` | `99` | `amd-rocm` | Prefer ROCm if available, else Vulkan |
| **No usable GPU / unstable** | `cpu` | `0` | `cpu-only` | Slowest but most reliable |

```powershell
llm gpu list
llm gpu use 0 --backend vulkan --apply-profile   # Intel example
llm gpu use 0 --backend cuda --apply-profile     # NVIDIA example
llm gpu auto
llm gpu doctor                                   # Intel Vulkan / oneAPI check
```

**Rule of thumb:** more VRAM / discrete GPU → higher `GpuLayers` (often `99`). Integrated Intel → stay around `20–29`.

---

## Install

### Option A — Single EXE (easiest)

1. Download the matching EXE from [Releases](https://github.com/snigdho48/llm-cli/releases)
2. Run install (copies itself to `%LOCALAPPDATA%\LLM\bin` and adds PATH):

```powershell
.\llm-1.0.4-win-x64.exe install
```

Open a **new** terminal, then:

```powershell
llm version
```

### Option B — Zip + installer

```powershell
.\install.ps1
```

Same result: `llm.exe` on PATH under `%LOCALAPPDATA%\LLM\bin`. Open a **new** terminal, then `llm help`.

### Option C — From this repo

```powershell
.\scripts\install.ps1
```

Uninstall: `.\scripts\uninstall.ps1` (add `-RemoveBinaries` to delete installed files).

---

## First-time setup (5 minutes)

Pick a workspace folder (example: `D:\AI`).

### Guided

```powershell
llm setup --workspace "D:\AI" --install-runtime
```

### Step by step

```powershell
llm init "D:\AI"                  # GPU/CPU + context / layers / threads / port
llm runtime install               # or: llm runtime import "C:\path\to\build"
llm model pull qwen2.5-coder-7b --use   # pick model for your RAM (table above)
llm serve
llm chat "Say hello in one sentence"
llm doctor
```

On `init`, press **Enter** to keep each recommended default.

---

## Everyday use

### Server

| Goal | Command |
|------|---------|
| Start + show API URL | `llm serve` |
| Restart after config change | `llm serve --restart` |
| Auto-start at logon | `llm serve --daemon` |
| Status / stop / logs | `llm runtime status` · `llm runtime stop` · `llm logs` |

Example output:

```text
OpenAI API : http://127.0.0.1:11434/v1
API Key    : local-ai
```

### Chat & models

```powershell
llm chat "Explain recursion simply"
llm model search coder
llm model pull qwen2.5-coder-7b --use
llm model list
```

### Change settings later

```powershell
llm config show
llm config set Runtime:Context 16384
llm config set Runtime:GpuLayers 29
llm config set Runtime:Threads 8
llm config set Runtime:Port 11434
llm serve --restart
```

| Key | Meaning |
|-----|---------|
| `Runtime:Context` | How much text the model can “see” |
| `Runtime:GpuLayers` | How much runs on GPU (`0` = CPU only) |
| `Runtime:Threads` | CPU threads |
| `Runtime:Port` | Local API port |
| `Runtime:GpuBackend` | `vulkan` / `cuda` / `rocm` / `cpu` |

Presets: `llm profile list` → `llm profile use iris-xe-coding`

---

## Use with Cursor

1. `llm serve`
2. `llm cursor` or `llm cursor --write`
3. In Cursor, set OpenAI-compatible base URL to:

```text
http://127.0.0.1:11434/v1
```

API key (default): `local-ai`

---

## Quick troubleshooting

| Problem | Try this |
|---------|----------|
| `llm` not found | New terminal after install; check PATH |
| Won’t start | `llm doctor` · `llm logs` |
| Port in use | `llm config set Runtime:Port 11434` → `llm serve --restart` |
| Slow / no GPU | `llm gpu list` → `llm gpu use 0 --backend vulkan --apply-profile` |
| Out of memory / thrashing | Smaller model, lower context, `GpuLayers 0` |
| Intel Vulkan issues | `llm gpu doctor` → `llm gpu fix --vulkan` |

---

## More help

```powershell
llm help
```

| Guide | For |
|-------|-----|
| [Models](docs/MODELS.md) | Extra model notes |
| [Commands](docs/COMMANDS.md) | Full command list |
| [Configuration](docs/CONFIGURATION.md) | Config file location |
| [Contributing](docs/CONTRIBUTING.md) | Building from source |

---

## License

**MIT License** — free and open source.

Copyright (c) 2026 **MD. Atiquzzaman Snigdho**

You may use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of this software, subject to including the copyright and permission
notice. Full text: [LICENSE](LICENSE).

**Repo:** https://github.com/snigdho48/llm-cli  
**Releases:** https://github.com/snigdho48/llm-cli/releases
