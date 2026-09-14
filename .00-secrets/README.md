# .00-secrets/ — project secrets (git-crypt)

This folder holds ALL project secrets, encrypted with git-crypt.
Files are plaintext in your working tree, encrypted in git (commits/remote).

Rules:
- Put EVERY secret here. Nothing secret lives outside this folder.
  Only exceptions: personal/per-developer files and the git-crypt key itself (kept in a password manager).
- On a new machine: `git-crypt unlock <key-from-your-password-manager>` before using the repo.
- Full runbook: `docs/git-encryption.md`.
- The git-crypt key is the ONLY backup — if lost, encrypted secrets are unrecoverable.

## Contents

- `.mcp.json` — MCP server config with API keys/tokens. Symlinked back to the project root
  (`<root>/.mcp.json`, gitignored). Recreate the link if missing:
  Windows: `cmd /c mklink .mcp.json .00-secrets\.mcp.json` · Unix: `ln -s .00-secrets/.mcp.json .mcp.json`
- `.nuget-api-key` — NuGet publish key, read by `publish-nuget.sh`.
