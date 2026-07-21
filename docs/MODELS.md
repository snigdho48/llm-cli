# Models

## Recommended for HP EliteBook 840 G8 (32 GB / Iris Xe)

| Model | Catalog ID | Speed | Coding | Notes |
|-------|------------|-------|--------|-------|
| Qwen2.5-Coder 7B Q4_K_M | `qwen2.5-coder-7b` | 8–15 tok/s | ⭐⭐⭐⭐⭐ | **Primary recommendation** |
| Qwen3-Coder 8B Q4_K_M | `qwen3-coder-8b` | 7–12 tok/s | ⭐⭐⭐⭐⭐ | Newer, slightly heavier |
| Qwen2.5-Coder 3B Q4_K_M | `qwen2.5-coder-3b` | 20–35 tok/s | ⭐⭐⭐⭐ | Fastest, lighter quality |
| CodeLlama 7B Q4_K_M | `codellama-7b` | 8–15 tok/s | ⭐⭐⭐⭐ | Solid alternative |
| Mistral 7B Instruct | `mistral-7b` | 10–18 tok/s | ⭐⭐⭐⭐ | General purpose, fast |
| DeepSeek-Coder V2 Lite 16B | `deepseek-coder-v2-lite` | 2–5 tok/s | ⭐⭐⭐⭐⭐ | Slow but strong |

## Search & pull

```powershell
llm model search
llm model search coder --recommended
llm model pull qwen2.5-coder-7b --use
```

Pull downloads to `<workspace>/downloads/`, registers in `models.json`, and optionally activates.

## Local models

If you already have GGUF files (e.g. `D:\MODEL\`):

```powershell
llm model add "D:\MODEL\qwen2.5-coder-7b-instruct-q4_k_m.gguf" qwen
llm model use qwen
```

Or scan a directory:

```powershell
llm model scan "D:\MODEL" --register
```

## Registry

```powershell
llm model list
llm model use qwen2.5-coder-7b
llm model remove old-model
```

Registry file: `%LOCALAPPDATA%\LLM\models.json`

Active model path is also written to `config.json` → `runtime.activeModelPath`.

## Quantization guide

| Quant | Size (7B) | Quality | Your hardware |
|-------|-----------|---------|---------------|
| Q4_K_M | ~4.7 GB | Best balance | ✅ Recommended |
| Q5_K_M | ~5.5 GB | Higher quality | ✅ Fits 32 GB |
| Q8_0 | ~7.5 GB | Near full | ✅ Fits, slower |
| Q2_K | ~2.5 GB | Lower quality | ✅ Fast but degraded |

**14B models** (Q4_K_M ~8–9 GB) load on 32 GB but run at ~2–5 tok/s.

**32B models** — technically possible with aggressive quant; practically too slow.

## Iris Xe vs CPU

Intel Iris Xe can run Vulkan offload (`-ngl 29`) but shares system RAM with ~50 GB/s bandwidth. On this laptop:

- **CPU (llama.cpp AVX2)** — often the most reliable baseline
- **Vulkan (-ngl 29)** — 5–20% gain possible; worth testing
- **OpenVINO** — future provider option

Use profiles to switch without reconfiguring manually:

```powershell
llm profile use iris-xe-coding   # GPU offload
llm profile use cpu-only         # CPU only
llm runtime restart
```

## Hugging Face URL format

Manual download URL pattern:

```
https://huggingface.co/{org}/{repo}/resolve/main/{filename}.gguf
```

`llm model pull` resolves this automatically from catalog IDs or `repo/file.gguf` references.
