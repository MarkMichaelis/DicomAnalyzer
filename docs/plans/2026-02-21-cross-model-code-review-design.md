# Cross-Model Code Review via CLI — Design

**Date:** 2026-02-21
**Status:** Approved

## Problem

The dev-loop currently assigns code reviews to GitHub Copilot via PR review
(gh pr edit --add-reviewer copilot). This couples the workflow to GitHub's
infrastructure and does not guarantee the review runs on a different model
family than the one that wrote the code.

## Solution: Model-Aware Routing

The authoring agent identifies its own model family, then selects a reviewer
CLI tool from a **different** family. This ensures independent perspective.

### Model Family Classification

| Model | Family |
|---|---|
| Claude (Opus, Sonnet, Haiku — any version) | `anthropic` |
| GPT, o-series, Codex (any version) | `openai` |
| Gemini (any version) | `google` |

### Reviewer Selection

| Authoring Family | 1st Choice | 2nd Choice |
|---|---|---|
| `anthropic` | `codex review` (OpenAI) | `copilot -p` with OpenAI model |
| `openai` | `claude -p` (Anthropic) | `copilot -p` with Claude model |
| `google` | `claude -p` or `codex review` | whichever available |

### CLI Invocation Patterns

**codex review** (purpose-built):
```powershell
codex review --base main "<review instructions>"
```

**claude -p** (non-interactive print mode):
```powershell
claude -p "<review prompt>" --model sonnet --allowedTools "Bash(git:*),Read"
```

**copilot -p** (non-interactive, supports multiple model families):
```powershell
copilot -p "<review prompt>" --model claude-sonnet-4.6 -s --allow-all-tools
```

### Fallback

If no cross-model CLI is available, fall back to the VS Code `@code-review`
chat agent with a warning that the review is same-context.

## Files to Update

| File | Change |
|---|---|
| `.github/agents/dev-loop.agent.md` | Replace Phase 7 (GitHub PR) with CLI cross-model review |
| `.github/agents/code-review.agent.md` | Reframe as review prompt/instructions for CLI invocation |
| `.github/copilot-instructions.md` | Update Agent Files table and workflow description |

## Scope

- Phase 7 and Phase 9 of the dev-loop change from GitHub PR review to CLI invocation
- The code-review agent becomes the source of the review prompt content
- No changes to other phases (TDD, refactor, functional testing, etc.)
