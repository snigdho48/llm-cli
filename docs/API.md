# OpenAI-Compatible API

LLM CLI does not implement its own inference server. It manages **llama-server** from llama.cpp, which exposes an OpenAI-compatible HTTP API.

## Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/health` | Server health (used by `llm doctor`) |
| POST | `/v1/chat/completions` | Chat completions |
| POST | `/v1/completions` | Text completions (llama.cpp) |
| GET | `/v1/models` | List loaded model |

## Connection details

```
Base URL : http://127.0.0.1:8080/v1
API Key  : local-ai
Model    : local  (any string — llama.cpp uses the loaded GGUF)
```

Get these anytime:

```powershell
llm serve
llm cursor
```

## Cursor setup

1. Open **Cursor Settings → Models**
2. Enable **OpenAI API Key** override
3. Set Base URL and API Key as above
4. Verify: `llm doctor` shows `[ OK ] OpenAI API`

## Example: chat completions

```powershell
curl http://127.0.0.1:8080/v1/chat/completions `
  -H "Authorization: Bearer local-ai" `
  -H "Content-Type: application/json" `
  -d '{
    "model": "local",
    "messages": [{"role": "user", "content": "Hello"}],
    "temperature": 0.2,
    "max_tokens": 100
  }'
```

Or use the CLI:

```powershell
llm chat "Explain recursion briefly"
```

## CLI auto-start

`llm chat` and `llm serve` call `EnsureRunningAsync()` — they start llama-server if stopped and wait for `/health` (up to 60 seconds).

Pass `--no-start` to `llm chat` if you want to fail when server is down.

## Future: `llm serve --daemon`

Planned: Windows service / background task integration for always-on local AI.

Today: `llm serve` starts the process in background (CreateNoWindow) and returns immediately.
