# Release Checklist — LLM CLI

## Local package

```powershell
.\scripts\harness.ps1
.\scripts\release.ps1 -Version 1.0.0
```

Outputs (gitignored under `artifacts/release/`):
- `llm-cli-1.0.0-win-x64.zip`
- `llm-cli-1.0.0-win-x64.zip.sha256`
- Updates `packaging/winget/manifest/*.yaml` PackageVersion + InstallerSha256

## GitHub Actions (recommended)

### CI (every push/PR to main/develop)

- Build + test + harness
- Preview package uploaded as workflow artifact `llm-cli-win-x64-ci`

### Release (auto packaging)

Trigger either:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

Or: **Actions → Release → Run workflow** and enter version `1.0.0`.

The Release workflow will:
1. Run harness + `scripts/release.ps1`
2. Upload zip + sha256 as Actions artifacts
3. Create a GitHub Release with the zip, checksum, and winget YAML attached

## Human verification

- [ ] `.\scripts\install.ps1`
- [ ] `llm setup` / `llm serve`
- [ ] `llm cursor --write`
- [ ] Cursor coding prompt works
- [ ] `llm bench`

## Winget submit

See `packaging/winget/README.md`.

```powershell
winget validate --manifest .\packaging\winget\manifest
```

Copy YAMLs to `manifests/s/Snigdho48/LLMCLI/<version>/` in a fork of `microsoft/winget-pkgs`.
