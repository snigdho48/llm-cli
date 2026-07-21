# Runtime Management

## Overview

LLM CLI manages **llama.cpp** as the primary runtime provider. The managed copy lives at:

```
<workspace>\runtimes\llama.cpp\current\
```

## Import (current)

For developers who build llama.cpp locally with Vulkan:

```powershell
llm runtime import "C:\llama.cpp"
# or
llm runtime import "C:\llama.cpp\build\bin\Release"
```

This **copies** (never symlinks) these files:

- `llama-server.exe`
- `llama-server-impl.dll`, `llama.dll`, `llama-common.dll`, `mtmd.dll`
- `ggml.dll`, `ggml-base.dll`, `ggml-cpu.dll`, `ggml-vulkan.dll`

Updates `runtime.executablePath` in config.

## Start / stop

```powershell
llm runtime start
llm runtime status
llm runtime stop
llm runtime restart
llm serve              # ensure running + print URL
```

### llama-server arguments (auto-generated)

```
-m <model.gguf>
-ngl <gpuLayers>
-c <contextSize>
-t <threads>
--host 127.0.0.1
--port 8080
--api-key local-ai
--metrics
```

## Logs

Runtime stdout/stderr → `<workspace>\logs\llama-server-{timestamp}.log`

```powershell
llm logs
llm logs --lines 100
```

## Install from release (planned v2.0)

Future command:

```powershell
llm runtime install llama.cpp
```

Will download the correct GitHub Release for OS/arch/backend, verify checksum, and install into versioned directory:

```
runtimes/llama.cpp/v{version}/
runtimes/llama.cpp/current/  → symlink or copy
```

Today: use `runtime import` with your local Vulkan build.

## Health check

```powershell
llm doctor
```

Checks: workspace, runtime installed, model exists, `ggml-vulkan.dll` present, `/health` API response.

## Profiles

Apply before starting:

```powershell
llm profile use iris-xe-coding   # ngl 29 — your tuned setup
llm profile use cpu-only         # ngl 0  — if Vulkan underperforms
llm runtime restart
```

On Iris Xe hardware, **CPU-only can match or beat GPU offload** for 7B models. Benchmark both with `llm chat` and pick what feels faster.
