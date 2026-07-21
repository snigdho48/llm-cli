# Changelog

All notable changes to LLM CLI are documented here.

## [Unreleased]

### Added — 2026-07-21

**Time:** 2026-07-21 17:22 (UTC+6)  
**Author:** Cursor agent

**Summary:** v1.0.4 — `llm install` auto-installs to PATH; no manual mkdir/copy.

**Files:**
- `src/LLM.CLI/Commands/InstallCommand.cs`
- `scripts/install.ps1` / `uninstall.ps1`
- version bump 1.0.4

**Impact:** `.\llm-*-win-x64.exe install` then `llm version` in a new terminal.

### Added — 2026-07-21

**Time:** 2026-07-21 17:20 (UTC+6)  
**Author:** Cursor agent

**Summary:** `llm install` auto-copies the current exe to `%LOCALAPPDATA%\LLM\bin` and adds PATH; install.ps1 also places `llm.exe` there (accepts versioned release exes).

**Files:**
- `src/LLM.CLI/Commands/InstallCommand.cs` — new command (`install` / `--install`)
- `scripts/install.ps1` / `uninstall.ps1` — bin placement + cleanup
- `README.md`, release workflow notes — no more manual mkdir/copy

**Impact:** Download exe → `.\llm-*-win-x64.exe install` → new terminal → `llm version`.

### Changed — 2026-07-21

**Time:** 2026-07-21 17:14 (UTC+6)  
**Author:** Cursor agent

**Summary:** Bump to v1.0.3 so GitHub Release ships without `.sha256` sidecar assets.

**Files:**
- `src/LLM.CLI/LLM.CLI.csproj` — 1.0.3
- `scripts/release.ps1` — default version
- winget manifest PackageVersion

**Impact:** New tag produces exe/zip-only release assets.

### Removed — 2026-07-21

**Time:** 2026-07-21 17:12 (UTC+6)  
**Author:** Cursor agent

**Summary:** Drop internal planning/research docs from the repo; expand `.gitignore` for build junk and those paths.

**Files:**
- `.gitignore` — artifacts, logs, IDE, research doc patterns
- `docs/NEXT_STEPS_RESEARCH.md`, `PRODUCTION_PLAN.md`, `ROADMAP.md` — removed from git
- `src/LLM.CLI/Routing/README_ALPHA04.md` — removed
- `scripts/harness.ps1`, evals/docs references — updated

**Impact:** Public repo keeps product docs only.

### Changed — 2026-07-21

**Time:** 2026-07-21 17:11 (UTC+6)  
**Author:** Cursor agent

**Summary:** MODELS.md rewritten for all hardware — recommendations by RAM, CPU cores, and GPU (Iris Xe, Arc, NVIDIA, AMD, CPU).

**Files:**
- `docs/MODELS.md` — hardware-agnostic model / quant / combo tables

**Impact:** Users pick models from their machine specs, not one laptop profile.

### Changed — 2026-07-21

**Time:** 2026-07-21 17:08 (UTC+6)  
**Author:** Cursor agent

**Summary:** GitHub Releases no longer attach `.sha256` sidecar files; hashes remain only inside winget InstallerSha256.

**Files:**
- `scripts/release.ps1` — stop writing `.sha256` files
- `.github/workflows/release.yml` — drop checksum assets from release
- `.github/workflows/ci.yml` — drop checksum artifacts
- `docs/RELEASE.md`, `packaging/winget/README.md` — docs

**Impact:** Cleaner release downloads (exe + zip only).

### Changed — 2026-07-21

**Time:** 2026-07-21 17:05 (UTC+6)  
**Author:** Cursor agent

**Summary:** MIT license copyright set to MD. Atiquzzaman Snigdho; README and winget locale updated for free/open-source attribution.

**Files:**
- `LICENSE` — copyright holder name
- `README.md` — license section
- `packaging/winget/manifest/Snigdho48.LLMCLI.locale.en-US.yaml` — Author / Copyright
- `Directory.Build.props` — PackageLicenseExpression / Authors

**Impact:** Clear MIT ownership for GitHub, releases, and winget.

### Changed — 2026-07-21

**Time:** 2026-07-21 17:20 (UTC+6)  
**Author:** Cursor agent

**Summary:** README adds preferred model/context/threads/GPU-layer guidance for 4–64 GB RAM, CPU cores, and GPU types.

**Files:**
- `README.md` — hardware recommendation tables + setup examples

**Impact:** Users can pick model and runtime settings from RAM/CPU/GPU without reading internal docs.

### Added — 2026-07-21

**Time:** 2026-07-21 17:15 (UTC+6)  
**Author:** Cursor agent

**Summary:** Multi-arch Windows releases — self-contained `llm.exe` for win-x64, win-x86, and win-arm64.

**Files:**
- `scripts/release.ps1` — builds all RIDs; regenerates multi-arch winget installer
- `scripts/install.ps1` — detects host arch (x64/x86/arm64) when building from source
- `.github/workflows/release.yml` — uploads all exe/zip variants
- `README.md` — arch download table

**Impact:** Users on Intel/AMD x64, 32-bit x86, and Windows on ARM can download a matching binary.

### Added — 2026-07-21

**Time:** 2026-07-21 17:10 (UTC+6)  
**Author:** Cursor agent

**Summary:** Release builds a self-contained single-file `llm.exe` (no separate .NET install) plus standalone download and zip.

**Files:**
- `scripts/release.ps1` — `PublishSingleFile` + `win-x64` self-contained; emits `llm-*-win-x64.exe`
- `scripts/install.ps1` — prefers bundled `llm.exe`; builds single-file from source
- `packaging/winget/manifest/*.yaml` — nested `llm.exe`, no DotNet runtime dependency
- `.github/workflows/release.yml` / `ci.yml` — upload exe + zip
- `README.md` — single-EXE install instructions

**Impact:** Users can download one `.exe` from GitHub Releases and run without installing .NET.

### Changed — 2026-07-21

**Time:** 2026-07-21 17:05 (UTC+6)  
**Author:** Cursor agent

**Summary:** README rewritten for end users (install, setup, everyday commands, Cursor, troubleshooting) instead of developer build details.

**Files:**
- `README.md` — user-facing guide

**Impact:** New users can install from Releases and run `llm setup` / `llm serve` without reading internal docs.

### Added — 2026-07-21

**Time:** 2026-07-21 17:00 (UTC+6)  
**Author:** Cursor agent

**Summary:** GitHub Actions CI package artifacts + Release workflow (tag `v*` / manual) auto-builds zip and publishes GitHub Release.

**Files:**
- `.github/workflows/ci.yml` — package job + artifact upload
- `.github/workflows/release.yml` — tag/dispatch packaging + softprops release
- `docs/RELEASE.md` — Actions release instructions
- `.gitignore` — ignore `artifacts/`

**Impact:** Pushing `v1.0.0` (or running Release workflow) produces downloadable win-x64 zip automatically.

### Added — 2026-07-21

**Time:** 2026-07-21 16:55 (UTC+6)  
**Author:** Cursor agent

**Summary:** `llm init` prompts for context length, GPU layers, threads, and port (Enter keeps defaults); change later via `config set`.

**Files:**
- `src/LLM.CLI/Commands/InitCommand.cs` — runtime parameter wizard + `--context`/`--ngl`/`--threads`/`--port`
- `src/LLM.CLI/Commands/ConfigCommand.cs` — `Runtime:Threads`, clearer keys/help, restart hint
- `src/LLM.CLI/Commands/HelpCommand.cs`, `docs/COMMANDS.md`

**Impact:** First-run sets llama-server `-c`/`-ngl`/`-t`/port; later edits use `llm config set`.

### Added — 2026-07-21

**Time:** 2026-07-21 16:50 (UTC+6)  
**Author:** Cursor agent

**Summary:** Release packaging + winget multi-file portable manifests; auto-download llama.cpp when runtime missing (`serve` / `setup`).

**Files:**
- `scripts/release.ps1` — zip SHA256 + fill winget InstallerSha256
- `packaging/winget/manifest/Snigdho48.LLMCLI*.yaml` — multi-file portable zip manifest
- `src/LLM.CLI/Services/RuntimeService.cs` — `EnsureInstalledAsync` auto-install
- `src/LLM.CLI/Commands/ServeCommand.cs` — `--no-install`; auto runtime download
- `src/LLM.CLI/Commands/SetupCommand.cs` — Enter downloads runtime; `--install-runtime` / `--no-runtime`
- `src/LLM.CLI/Services/GpuDetectionService.cs` — Intel Vulkan sets `GGML_VK_DISABLE_F16`

**Impact:** Cold-start path and winget submit path are release-ready after GitHub Release upload.

### Changed — 2026-07-21

**Time:** 2026-07-21 16:40 (UTC+6)  
**Author:** Cursor agent

**Summary:** `llm init` now prompts for GPU/CPU device, backend, threads, and profile (with `--gpu` / `--backend` / `--cpu` / `--auto` flags).

**Files:**
- `src/LLM.CLI/Commands/InitCommand.cs` — compute selection wizard
- `src/LLM.CLI/Services/GpuDetectionService.cs` — `ApplyCpuOnly`, soft backend apply, `ResolveProfileId`
- `src/LLM.CLI/Commands/HelpCommand.cs`, `docs/COMMANDS.md` — usage

**Impact:** First-run init persists compute choice instead of leaving GpuBackend=auto unset. `llm setup` prompts for the same when interactive.

### Fixed — 2026-07-21

**Time:** 2026-07-21 16:30 (UTC+6)  
**Author:** Cursor agent

**Summary:** Intel Vulkan ICD check no longer false-fails when Khronos Drivers registry is empty but Intel `igvk64.json` / vulkaninfo work.

**Files:**
- `src/LLM.CLI/Services/IntelPrerequisiteService.cs` — DriverStore ICD scan + vulkaninfo Intel device check
- `tests/LLM.Tests/Services/IntelPrerequisiteServiceTests.cs` — updated check names

**Impact:** `llm gpu doctor` correctly reports Vulkan ready on Iris Xe with modern Intel drivers.

### Added — 2026-07-21

**Time:** 2026-07-21 16:50 (UTC+6)  
**Author:** Cursor agent

**Summary:** Intel Vulkan + oneAPI prerequisite checks with install instructions and winget auto-fix.

**Files:**
- `src/LLM.CLI/Services/IntelPrerequisiteService.cs` — detect vulkan-1.dll, ICD, ggml-vulkan, oneAPI, Level Zero
- `src/LLM.CLI/Services/WingetService.cs` — automate `KhronosGroup.VulkanRT` / `Intel.OneAPI.BaseToolkit`
- `src/LLM.CLI/Commands/GpuCommand.cs` — `gpu doctor`, `gpu fix [--vulkan|--oneapi|--all]`

**Impact:** Iris Xe users get clear guidance and optional one-command install for missing Vulkan/oneAPI deps.

### Added — 2026-07-21  
**Author:** Cursor agent

**Summary:** Multi-GPU detection and selection for Intel / NVIDIA / AMD with backend auto-pick (Vulkan, CUDA, ROCm, CPU).

**Files:**
- `src/LLM.CLI/Services/GpuDetectionService.cs` — WMI GPU scan, backend DLL detect, vendor-relative device index
- `src/LLM.CLI/Commands/GpuCommand.cs` — `gpu list|status|use|auto`
- `src/LLM.CLI/Configuration/GpuDevice.cs` — vendor/backend types
- Profiles: `nvidia-cuda`, `amd-rocm` (+ backend field on all profiles)
- `RuntimeService` — sets CUDA_VISIBLE_DEVICES / GGML_VK_VISIBLE_DEVICES / HIP_VISIBLE_DEVICES
- Doctor/setup/health show and validate selected GPU backend

**Impact:** Users with Iris Xe, NVIDIA, or AMD can list GPUs and select one; runtime uses matching backend.

### Added — 2026-07-21  
**Author:** Cursor agent

**Summary:** Phase 7 machine setup, live Hugging Face search, daemon auto-start, config migration defaults, port conflict detection.

**Files:**
- `src/LLM.CLI/Services/HuggingFaceSearchService.cs` — Hub search + GGUF file listing
- `src/LLM.CLI/Commands/ModelCommand.cs` — `search --live`, `model files`
- `src/LLM.CLI/Services/DaemonService.cs` + `Commands/DaemonCommand.cs` — Task Scheduler logon auto-start
- `src/LLM.CLI/Commands/ServeCommand.cs` — `--daemon` flag
- `src/LLM.CLI/Services/UserDataService.cs` — migrate missing runtime defaults on load
- `src/LLM.CLI/Services/HealthCheckService.cs` — detect HTML responses on /health
- `src/LLM.CLI/Services/RuntimeService.cs` — wait for healthy; refuse start if port in use

**Impact:** Workspace D:\AI live; Hub search + Windows auto-start; server on port 11434 (8080 taken by Apache httpd).

### Changed — 2026-07-21

**Time:** 2026-07-21 15:55 (UTC+6)  
**Author:** Cursor agent

**Summary:** All CLI commands now return exit codes via `Task<int> ExecuteAsync` and `CommandResults.Success` / `CommandResults.Failure`.

**Files:**
- `src/LLM.CLI/Commands/HelpCommand.cs` — already migrated
- `src/LLM.CLI/Commands/ModelCommand.cs`, `ServeCommand.cs`, `DoctorCommand.cs`, `InitCommand.cs`, `SetupCommand.cs`, `LogsCommand.cs`, `CursorCommand.cs`, `ChatCommand.cs`, `ProfileCommand.cs`, `RuntimeCommand.cs`, `ConfigCommand.cs` — return exit codes

**Impact:** CLI process exit code reflects command success/failure for scripting and CI.

### Added — 2026-07-21  
**Author:** Cursor agent

**Summary:** Production plan with harness/loop engineering, exit codes, runtime install, bench, CI, and release packaging.

**Files:**
- `docs/PRODUCTION_PLAN.md` — phased plan with eval definitions
- `evals/` — baseline.json + capability eval specs
- `scripts/harness.ps1`, `scripts/loop-verify.ps1`, `scripts/release.ps1`
- `src/LLM.CLI/Services/RuntimeInstallService.cs` — GitHub release install
- `src/LLM.CLI/Services/CursorSettingsService.cs` — `cursor --write`
- `src/LLM.CLI/Services/BenchService.cs` + `Commands/BenchCommand.cs`
- `src/LLM.CLI/Commands/ICommand.cs` — exit codes (CommandResults)
- `.github/workflows/ci.yml` — build + test + harness
- `docs/RELEASE.md`, `packaging/winget/LLM.CLI.yaml`

**Impact:** Production gate via harness (pass^3); v1.0 release path defined.

### Added — 2026-07-21  
**Author:** Cursor agent

**Summary:** Sprint 0 docs package, model search/pull (HF catalog), and `llm serve` aligned with Git+Docker+Ollama product vision.

**Files:**
- `docs/ARCHITECTURE.md`, `SPEC.md`, `COMMANDS.md`, `CONFIGURATION.md`, `RUNTIME.md`, `MODELS.md`, `API.md`, `CONTRIBUTING.md` — full docs set
- `docs/ROADMAP.md` — merged sprint plan + version history
- `src/LLM.CLI/Services/ModelCatalogService.cs` — curated models for 32 GB / Iris Xe
- `src/LLM.CLI/Commands/ServeCommand.cs` — ensure running + print OpenAI URL
- `src/LLM.CLI/Commands/ModelCommand.cs` — `search`, `pull` subcommands
- `tests/LLM.Tests/Services/ModelCatalogServiceTests.cs`

**Impact:** Phase 4–5 of original sprint plan complete; docs match production CLI contract.

### Added — 2026-07-21  
**Author:** Cursor agent

**Summary:** v1.1–v1.6 workstation features — setup wizard, hardware profiles, chat/cursor commands, model download, runtime logging.

**Files:**
- `src/LLM.CLI/Commands/SetupCommand.cs` — guided first-time setup
- `src/LLM.CLI/Services/ProfileService.cs` — Iris Xe / balanced / CPU profiles
- `src/LLM.CLI/Commands/ProfileCommand.cs` — profile list/show/use
- `src/LLM.CLI/Services/OpenAiService.cs` + `Commands/ChatCommand.cs` — test inference
- `src/LLM.CLI/Commands/CursorCommand.cs` — Cursor IDE connection guide
- `src/LLM.CLI/Services/ModelDownloadService.cs` — `model download` with progress
- `src/LLM.CLI/Services/LogService.cs` + `Commands/LogsCommand.cs` — tail runtime logs
- `src/LLM.CLI/Services/HardwareService.cs` — hardware recommendations in doctor/setup
- `src/LLM.CLI/Services/RuntimeService.cs` — log redirection, EnsureRunningAsync
- `docs/ROADMAP.md` — full product roadmap
- Removed dead stub commands in Config/, Runtime/, Doctor/

**Impact:** CLI is a complete local coding workstation manager from setup through Cursor integration.

### Added — 2026-07-21

**Time:** 2026-07-21 16:00 (UTC+6)  
**Author:** Cursor agent

**Summary:** Model registry commands, enhanced doctor with OpenAI health checks, runtime restart, and global Windows installer script.

**Files:**
- `src/LLM.CLI/Services/ModelService.cs` — model registry at `%LOCALAPPDATA%\LLM\models.json`
- `src/LLM.CLI/Commands/ModelCommand.cs` — `list`, `scan`, `add`, `use`, `remove`
- `src/LLM.CLI/Services/HealthCheckService.cs` — workspace, runtime, model, Vulkan, `/health` checks
- `src/LLM.CLI/Commands/DoctorCommand.cs` — full diagnostics with Cursor connection hints
- `src/LLM.CLI/Services/RuntimeService.cs` — `Restart()` support
- `src/LLM.CLI/Program.cs` — register ModelService, HealthCheckService, ModelCommand
- `scripts/install.ps1` — publish CLI and add global `llm` shim to user PATH
- `scripts/uninstall.ps1` — remove shim from PATH
- `tests/LLM.Tests/Services/ModelServiceTests.cs` — add/use/scan tests

**Impact:** CLI supports model management, API health verification for Cursor, and one-command global install on Windows.

### Added — 2026-07-21

**Time:** 2026-07-21 15:35 (UTC+6)  
**Author:** Cursor agent

**Summary:** Runtime import from local llama.cpp builds, start/stop/status commands, extended configuration, Cursor workflow docs.

**Files:**
- `src/LLM.CLI/Services/RuntimeImportService.cs` — copy Vulkan llama.cpp runtime into managed workspace
- `src/LLM.CLI/Services/RuntimeService.cs` — start/stop llama-server with tuned args
- `src/LLM.CLI/Commands/RuntimeCommand.cs` — `import`, `start`, `stop`, `status`
- `src/LLM.CLI/Configuration/UserConfiguration.cs` — GPU layers, model path, host, threads
- `tests/LLM.Tests/Services/RuntimeImportServiceTests.cs` — import path resolution tests
- `.cursor/workflow/README.md` — development workflow guide
- `.cursor/workflow/CHANGELOG.md` — this file
- `.cursor/rules/llm-cli-workflow.mdc` — project Cursor rules

**Impact:** CLI can manage a copied llama.cpp runtime inside the user workspace; configuration supports Iris Xe tuning (ngl 29).

### Added — 2026-07-21

**Time:** 2026-07-21 14:00 (UTC+6)  
**Author:** Cursor agent

**Summary:** Workspace initialization with managed directory structure and persistent user config.

**Files:**
- `src/LLM.CLI/Services/WorkspaceService.cs`
- `src/LLM.CLI/Commands/InitCommand.cs`
- `tests/LLM.Tests/Services/WorkspaceServiceTests.cs`

**Impact:** `llm init` creates runtimes/models/cache/logs workspace layout.
