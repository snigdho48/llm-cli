# Winget packaging — LLM CLI

Submit to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) **after** a GitHub Release exists with the zip attached.

## Files (multi-file manifest)

YAML lives in `manifest/` (keep README out of that folder — `winget validate` parses every file).

| File | Role |
|------|------|
| `manifest/Snigdho48.LLMCLI.yaml` | Version |
| `manifest/Snigdho48.LLMCLI.locale.en-US.yaml` | Locale / metadata |
| `manifest/Snigdho48.LLMCLI.installer.yaml` | Zip + portable nested installer |

## Build + fill SHA

```powershell
.\scripts\release.ps1 -Version 1.0.0
```

This publishes `artifacts/release/llm-cli-1.0.0-win-x64.zip`, writes `.sha256`, and updates `InstallerSha256` in the installer manifest.

## Local validate / test

```powershell
winget validate --manifest .\packaging\winget\manifest
winget settings --enable LocalManifestFiles   # elevated
winget install --manifest .\packaging\winget\manifest
```

## Submit

1. Upload zip to GitHub Release `v1.0.0`
2. Confirm InstallerUrl + SHA256 match the release asset
3. Copy the three YAML files to:

```text
manifests/s/Snigdho48/LLMCLI/1.0.0/
```

4. Open a PR with **only** those manifest files (one package version)
