# Gitleaks Pre-commit Hook Setup

This directory contains an optional pre-commit hook to scan for secrets before committing.

## Installation

To enable the pre-commit hook, run:

```bash
cp pre-commit.sample ../.git/hooks/pre-commit
chmod +x ../.git/hooks/pre-commit
```

## Requirements

The hook requires gitleaks to be installed. Install it with:

```bash
# macOS
brew install gitleaks

# Linux
wget https://github.com/gitleaks/gitleaks/releases/download/v8.18.2/gitleaks_8.18.2_linux_x64.tar.gz
tar -xzf gitleaks_8.18.2_linux_x64.tar.gz
sudo mv gitleaks /usr/local/bin/
```

## Usage

Once installed, the hook will automatically run every time you attempt to commit. If secrets are detected, the commit will be blocked.

To bypass the hook (not recommended), use:

```bash
git commit --no-verify
```
