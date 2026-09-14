# git-encryption.md — repo secrets encrypted with git-crypt

Secret system for this repo: **git-crypt**. All secrets live in `.00-secrets/`, encrypted in git,
plaintext in your working tree.

## New machine / fresh clone (files look like binary / start with GITCRYPT)
    sudo apt-get install -y git-crypt          # Debian/Ubuntu (macOS: brew install git-crypt)
    echo "<BASE64-FROM-PASSWORD-MANAGER>" | base64 -d > /tmp/k
    git-crypt unlock /tmp/k && (shred -u /tmp/k 2>/dev/null || rm -f /tmp/k)
    git-crypt status | grep 00-secrets         # now decrypted (plaintext)

## Daily use (already unlocked)
- Add/edit secrets in `.00-secrets/` → `git add` + `git commit` = encrypted automatically.
- `git pull` / `git checkout` = decrypted automatically in the working tree.
- Nothing secret goes outside `.00-secrets/`.

## Key management
- Export (backup / another machine): `git-crypt export-key /tmp/k && base64 -w0 /tmp/k`
  → save the base64 in your password manager, then `shred -u /tmp/k`.
- ⚠️ Without the key, encrypted secrets are UNRECOVERABLE. Only backup = password manager.

## Verify
    git check-ignore .00-secrets/.key              # empty = git tracks it (not ignored)
    git-crypt status | grep 00-secrets             # encrypted
    git show HEAD:.00-secrets/<file> | head -c 9 | grep -a GITCRYPT   # match = encrypted in the repo
