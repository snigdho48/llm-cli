# LLM CLI — What Next?: Research Report

*Generated: 2026-07-21 | Sources: 25+ | Confidence: High (product decision); Medium (market share claims)*

## Executive Summary

Feature work for a Windows-first llama.cpp + Cursor OpenAI server is largely past the “missing commands” stage. Competitors win on **install → model → API in one breath**, not on GPU doctor checklists. Your clearest next move is **ship + golden path**, not more architecture: freeze a tagged release, publish GitHub artifacts, fix the winget manifest to real zip+SHA portable install, then close the remaining first-run gap (`runtime install` when no local build, Iris Xe reliability knobs). Differentiator vs Ollama is real: Intel Iris Xe Vulkan on Windows is a known pain for Ollama users; lean into that.

## 1. Where you are vs the market

### Competitive pattern (2025–2026)

| Tool | Wins on | Weak for your niche |
|------|---------|---------------------|
| **Ollama** | One-line UX, model library speed, OpenAI API, ecosystem | Intel Iris Xe often mis-detected / CPU fallback on Windows ([ollama#13023](https://github.com/ollama/ollama/issues/13023)) |
| **LM Studio** | GUI discovery, HF integration, also has CLI now | Not a “Git/Docker-like” CLI manager; less scriptable by default |
| **LocalAI** | Multi-backend OpenAI hub, containers | Heavier; Docker-first friction for laptop Iris Xe |
| **Jan / GPT4All** | Offline chat UX | Less “runtime import / profile / Cursor” workstation focus |
| **llama.cpp raw** | Control, Vulkan | No install/model/Cursor packaging |

Sources: [DevToolReviews 2026 comparison](https://www.devtoolreviews.com/reviews/ollama-vs-lm-studio-vs-localai-2026), [daily.dev local LLMs 2026](https://daily.dev/blog/running-llms-locally-ollama-llama-cpp-self-hosted-ai-developers), [Glukhov hosting comparison](https://www.glukhov.org/llm-hosting/comparisons/hosting-llms-ollama-localai-jan-lmstudio-vllm-comparison), [Kunal Ganglani LM Studio vs Ollama 2026](https://www.kunalganglani.com/blog/lm-studio-vs-ollama).

**Inference:** Matching Ollama feature-for-feature is a trap. Matching **time-to-first-token from cold machine** is the bar. Your GPU/Intel doctor stack is a *moat* if the happy path is also short.

### Already shipped (stop rebuilding)

Init/setup with GPU-CPU select, multi-GPU, Vulkan/oneAPI doctor+fix, HF search/pull, serve, daemon, bench, cursor, install.ps1, CI, harness, winget stub — see `docs/PRODUCTION_PLAN.md` execution log Phases 0–8.

## 2. Packaging / winget reality

- WinGet accepts **ZIP + NestedInstallerType: portable** (schema 1.5+); MSI is nicer for enterprises but **not required** for v1 ([installer schema](https://github.com/microsoft/winget-pkgs/blob/master/doc/manifest/schema/1.12.0/installer.md), [Learn: supported formats](https://learn.microsoft.com/en-us/windows/package-manager/winget)).
- Community repo needs: **multi-file** manifest, **stable versioned InstallerUrl**, accurate **SHA256**, silent/unattended install, virus-clean, PR = one version only ([FirstContribution](https://github.com/microsoft/winget-pkgs/blob/master/doc/FirstContribution.md), [Submit policies](https://learn.microsoft.com/en-us/windows/package-manager/package/repository)).
- Current stub `packaging/winget/LLM.CLI.yaml` is a singleton with `PLACEHOLDER` SHA — **not submittable**.

**Recommendation:** Ship portable zip via `scripts/release.ps1` first; upgrade to MSI later for enterprise. Fix manifest to NestedInstallerFiles → `llm.exe` (or shim).

## 3. Iris Xe / Vulkan risks you should encode

- Performance can regress across llama.cpp builds on Iris Xe ([llama.cpp#12754](https://github.com/ggml-org/llama.cpp/issues/12754)).
- Vulkan gibberish on Intel has been mitigated with `GGML_VK_DISABLE_F16` in community reports ([Nov 2025 llama.cpp weekly](https://buttondown.com/weekly-project-news/archive/weekly-github-report-for-llamacpp-november-03-5264)).
- Ollama users report Iris Xe not detected despite working Vulkan ([ollama#13023](https://github.com/ollama/ollama/issues/13023)) — your doctor/use path is a selling point if reliable.

**Recommendation:** Pin a **known-good** llama.cpp Vulkan build for `runtime install`; optional env toggle for F16 disable; surface in `gpu doctor` / `serve` when Intel + bad output reported.

## 4. Recommended next sequence (decision)

### Now (this week) — Ship gate

1. **Human Phase-7 checklist** on your EliteBook (`docs/RELEASE.md`): install → serve → Cursor prompt → bench numbers.
2. **`.\scripts\harness.ps1` ×3** (pass^3).
3. **`.\scripts\release.ps1 -Version 1.0.0`** → GitHub Release + zip.
4. **Replace winget stub** with multi-file portable manifest + real SHA; PR to winget-pkgs *after* URL is live.
5. **Tag `v1.0.0`** and move CHANGELOG `[Unreleased]` → `[1.0.0]`.

### Next sprint (1–2 weeks) — Golden path (highest product ROI)

6. **Cold-start one command:** if no runtime, `setup`/`serve` auto-calls `runtime install` (Vulkan win-x64) then starts.
7. **Recommended model shortcut:** `llm model pull qwen2.5-coder-7b --use` as default post-init prompt (already catalog ID).
8. **Intel reliability:** document + optional auto `GGML_VK_DISABLE_F16`; pin release asset name in `RuntimeInstallService`.
9. **`runtime upgrade` + version pin** under `runtimes/llama.cpp/v{ver}/` (Roadmap v2.0).

### Later (defer)

| Item | Why defer |
|------|-----------|
| Plugin system / CommandRouter | Low user-visible ROI vs ship |
| Hot-swap (llama-swap style) | Nice; after stable serve |
| OpenVINO backend | Niche; after Vulkan path is bulletproof |
| brew | After Windows winget works |
| Opt-in telemetry | Only after privacy defaults + release |

## Key Takeaways

1. **Stop adding commands; ship the binary people can `winget install`.**
2. **Compete on Iris Xe + Cursor workstation UX**, not on being “another Ollama.”
3. **Close time-to-first-chat:** auto runtime download + pinned model + serve.
4. **Pin llama.cpp versions** — Iris Xe is build-sensitive.
5. **Winget needs a real zip+SHA multi-file manifest**, not the current stub.

## Sources

1. [Ollama vs LM Studio vs LocalAI 2026 — DevToolReviews](https://www.devtoolreviews.com/reviews/ollama-vs-lm-studio-vs-localai-2026)
2. [Running LLMs Locally 2026 — daily.dev](https://daily.dev/blog/running-llms-locally-ollama-llama-cpp-self-hosted-ai-developers)
3. [Local LLM hosting comparison — Glukhov](https://www.glukhov.org/llm-hosting/comparisons/hosting-llms-ollama-localai-jan-lmstudio-vllm-comparison)
4. [LM Studio vs Ollama 2026 — Kunal Ganglani](https://www.kunalganglani.com/blog/lm-studio-vs-ollama)
5. [Ollama Iris Xe not detected #13023](https://github.com/ollama/ollama/issues/13023)
6. [llama.cpp Vulkan Iris Xe regression #12754](https://github.com/ggml-org/llama.cpp/issues/12754)
7. [llama.cpp weekly (Vulkan F16 Intel)](https://buttondown.com/weekly-project-news/archive/weekly-github-report-for-llamacpp-november-03-5264)
8. [WinGet supported installers — Microsoft Learn](https://learn.microsoft.com/en-us/windows/package-manager/winget)
9. [Submit package repository — Microsoft Learn](https://learn.microsoft.com/en-us/windows/package-manager/package/repository)
10. [winget-pkgs FirstContribution](https://github.com/microsoft/winget-pkgs/blob/master/doc/FirstContribution.md)
11. [Installer schema NestedInstallerType](https://github.com/microsoft/winget-pkgs/blob/master/doc/manifest/schema/1.12.0/installer.md)
12. [llama-swap hot-swap guide 2026](https://modelslab.com/blog/api/hot-swap-local-llms-instantly-llama-swap-setup-guide-2026)
13. Tavily Research synthesis (competitive packaging + milestones), 2026-07-21

## Methodology

Searched and researched via Tavily (research + search + extract) and web search across competitor comparisons, winget policies, and Intel Vulkan llama.cpp issues. Sub-questions: competitive differentiators, winget packaging, Iris Xe first-run blockers, sequenced milestones for this repo’s current state.
