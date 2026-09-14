# Security (non-violable rules)

1. Every secret lives ONLY in `.00-secrets/` (git-crypt). Never elsewhere, never plaintext in git.
2. Only exceptions: personal/per-developer files and the git-crypt key (password manager, out-of-band).
3. Never commit secret values in config/manifests/scripts → reference the secret instead of inlining it.
4. The git-crypt key is the ONLY backup → password manager. Lost = secrets unrecoverable.
5. On a new machine: `git-crypt unlock` BEFORE using/building the project.
