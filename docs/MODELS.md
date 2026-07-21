# Models

Pick a GGUF by **system RAM** first, then tune **CPU threads** and **GPU layers** for your hardware. Prefer **Q4_K_M** unless noted.

Quick pull (replace the catalog id with one from the tables below):

```powershell
llm model search
llm model search coder --recommended
llm model pull qwen2.5-coder-7b --use
```

Pull downloads to `<workspace>/downloads/`, registers in `models.json`, and optionally activates.

---

## Recommended models by RAM

| System RAM | Preferred catalog ID | GGUF size (approx) | Context start | Notes |
|------------|----------------------|--------------------|---------------|-------|
| **4 GB** | Tiny models only (≤ ~2 GB file) | ≤ 2 GB | `2048` | Barely usable; close other apps. Not for Cursor coding. |
| **8 GB** | `qwen2.5-coder-3b` | ~2 GB | `4096` | Light coding / chat |
| **16 GB** | `qwen2.5-coder-3b` or small 7B | ~2–5 GB | `8192` | 3B comfortable; 7B is tight with browser open |
| **32 GB** | **`qwen2.5-coder-7b`** ⭐ | ~4.7 GB | `16384` | Best everyday coding setup |
| **64 GB+** | 7B–14B / `deepseek-coder-v2-lite` | ~5–10 GB | `16384`–`32768` | Room for larger context & heavier models |

### Catalog options (coding-focused)

| Catalog ID | Typical RAM fit | Relative speed | Coding | Notes |
|------------|-----------------|----------------|--------|-------|
| `qwen2.5-coder-3b` | 8–16 GB | Fast | Good | Best on low RAM |
| `qwen2.5-coder-7b` | 16–32 GB+ | Medium | Excellent | **Default recommendation** when you have ≥ 16–32 GB |
| `qwen3-coder-8b` | 32 GB+ | Medium | Excellent | Newer; slightly heavier than 7B |
| `codellama-7b` | 16–32 GB+ | Medium | Good | Solid alternative |
| `mistral-7b` | 16–32 GB+ | Medium–fast | Good | General purpose, not coder-only |
| `deepseek-coder-v2-lite` | 32–64 GB+ | Slow | Excellent | Strong but heavy; needs headroom |

Rule of thumb: leave **~8–12 GB** free for OS + IDE + browser. Model file size ≠ total RAM used (context and GPU layers add more).

---

## CPU threads

Set `Runtime:Threads` near **physical cores** (not hyperthreads). Leave 1–2 cores free for the OS / Cursor.

| CPU cores (physical) | Suggested `Threads` |
|----------------------|---------------------|
| 2 | `2` |
| 4 | `3`–`4` |
| 6 | `4`–`6` |
| 8 | `6`–`8` |
| 12+ | `8`–`12` |

```powershell
llm config set Runtime:Threads 8
```

More threads help **CPU-only** and prompt processing; they help less once most layers are on a strong discrete GPU.

---

## GPU backends & layers

| GPU | Backend | Suggested `GpuLayers` | Profile | Notes |
|-----|---------|------------------------|---------|-------|
| **Intel Iris Xe / UHD** (laptop iGPU) | `vulkan` | `20`–`29` | `iris-xe-coding` | Shares system RAM; don’t push layers too high |
| **Intel Arc** | `vulkan` | `35`–`99` | custom / `iris-xe-coding` | Stronger than Iris Xe |
| **NVIDIA** (GTX / RTX) | `cuda` (or `vulkan`) | `99` | `nvidia-cuda` | Usually fastest with a CUDA build |
| **AMD** (Radeon) | `rocm` or `vulkan` | `99` | `amd-rocm` | Prefer ROCm when available |
| **No usable GPU / unstable** | `cpu` | `0` | `cpu-only` | Slowest, most reliable |

```powershell
llm gpu list
llm gpu use 0 --backend vulkan --apply-profile
llm profile use iris-xe-coding   # or nvidia-cuda / amd-rocm / cpu-only
llm config set Runtime:GpuLayers 29
```

**Intel Iris Xe:** Vulkan offload can help a little (often ~5–20%), but shared memory bandwidth is limited. If generation feels worse or VRAM thrashing appears, drop layers or use `cpu-only`.

**Discrete NVIDIA/AMD:** prefer full offload (`99`) when VRAM allows; pair with a 7B–14B model on 16–64 GB system RAM.

---

## Suggested combos (RAM + GPU)

| Hardware | Model | Context | Layers | Threads |
|----------|-------|---------|--------|---------|
| 8 GB, any / weak iGPU | `qwen2.5-coder-3b` | `4096` | `0`–`15` | `4` |
| 16 GB, Iris Xe / UHD | `qwen2.5-coder-3b` (or small 7B) | `8192` | `20` | `4`–`6` |
| 16 GB, NVIDIA/AMD | `qwen2.5-coder-7b` | `8192` | `99` | `4`–`6` |
| 32 GB, Iris Xe | `qwen2.5-coder-7b` | `16384` | `29` | `8` |
| 32 GB, NVIDIA/AMD | `qwen2.5-coder-7b` or `qwen3-coder-8b` | `16384` | `99` | `6`–`8` |
| 64 GB, discrete GPU | 7B–14B / DeepSeek Lite | `32768` | `99` | `8`–`12` |
| Any, GPU broken | Match RAM table | lower context | `0` | cores − 1 |

Apply after pull:

```powershell
llm config set Runtime:Context 16384
llm config set Runtime:GpuLayers 29
llm config set Runtime:Threads 8
llm runtime restart
```

---

## Quantization guide

| Quant | Size (7B approx) | Quality | When to use |
|-------|------------------|---------|-------------|
| **Q4_K_M** | ~4.7 GB | Best balance | **Default** for most PCs |
| Q5_K_M | ~5.5 GB | Higher | ≥ 32 GB RAM and you want quality |
| Q8_0 | ~7.5 GB | Near full | ≥ 32–64 GB; slower / heavier |
| Q2_K / Q3_K | ~2–3 GB | Lower | Only when RAM is very tight |

**14B** Q4_K_M (~8–9 GB): needs ~32–64 GB system RAM; expect slower tokens on iGPU/CPU.

**32B+**: only on high-RAM machines with a strong discrete GPU; often too slow for interactive coding on laptops.

---

## Local models

If you already have GGUF files:

```powershell
llm model add "D:\path\to\model.gguf" my-model
llm model use my-model
```

Or scan a directory:

```powershell
llm model scan "D:\path\to\models" --register
```

## Registry

```powershell
llm model list
llm model use qwen2.5-coder-7b
llm model remove old-model
```

Registry file: `%LOCALAPPDATA%\LLM\models.json`

Active model path is also written to `config.json` → `runtime.activeModelPath`.

## Hugging Face URL format

```
https://huggingface.co/{org}/{repo}/resolve/main/{filename}.gguf
```

`llm model pull` resolves this from catalog IDs or `repo/file.gguf` references.
