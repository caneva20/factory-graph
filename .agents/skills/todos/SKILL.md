---
name: todos
description: Use when the user asks what todos exist, wants a backlog issue summarized or reviewed, or asks to complete tracked work in GitHub Issues.
---

# Todos

GitHub Issues labeled `todo` are the only backlog source. The selected GitHub Project supplies project metadata such as priority.

## Issue Repository

All issue operations target `caneva20/factory-planner`. The current repository contains the implementation, but does not host its own backlog.

## Project Resolution

1. Use a project explicitly requested by the user for the current operation.
2. Otherwise read `git config --local --get todo.project-owner` and `git config --local --get todo.project-number`.
3. If either value is missing, run `gh project list --owner caneva20 --format json`, ask the user to choose once, and save the answer with `git config --local todo.project-owner <owner>` and `git config --local todo.project-number <number>`.
4. Change the saved default only when the user asks.
5. Validate the project with `gh project view <number> --owner <owner> --format json` and fetch fields with `gh project field-list <number> --owner <owner> --format json`.

The project's single-select field named `Priority` and its current option order are authoritative. Never read, create, apply, validate, or sort by priority labels.

## Preflight

1. Run `gh auth status`.
2. Run `gh repo view caneva20/factory-planner --json nameWithOwner,url,owner`.
3. Resolve the selected/default project and its fields.
4. Stop with an actionable message if any command fails; never fall back to a local file. For missing Projects access, report the `gh auth refresh --hostname github.com --scopes project` remediation.

## List Todos

1. Query the selected project with `gh project item-list <number> --owner <owner> --limit 100 --format json`, restricting results to open `todo` issues in `caneva20/factory-planner`.
2. Sort by the `Priority` field's current option order, then issue number. Put items with no project priority last.
3. Present a numbered list containing issue number, priority, title, and URL.
4. Call out any matching issue with no project priority as invalid backlog data.

Include closed issues only when the user asks for history or completed work.

## Review A Todo

1. Prefer an issue number or URL.
2. For a text description, search with `gh issue list --repo caneva20/factory-planner --state all --label todo --search "<terms> in:title,body" --limit 100`.
3. If more than one plausible issue matches, show them and ask the user to choose.
4. Read the selected issue with `gh issue view <number> --repo caneva20/factory-planner --json number,title,body,state,labels,comments,url`.
5. Match its URL to the selected project's item list and read priority from that project item. If it is absent, say that it is not in the selected project instead of inferring a priority from labels.
6. Summarize its objective, project priority, state, acceptance criteria, and relevant discussion without inventing progress.

## Complete A Todo

1. Identify one open todo issue confidently.
2. Confirm implementation evidence from the current work or ask the user for it. Intent alone is not evidence.
3. Post a concise evidence comment with `gh issue comment <number> --repo caneva20/factory-planner --body "<completion evidence>"`.
4. Close it with `gh issue close <number> --repo caneva20/factory-planner --reason completed`.
5. Re-read the issue and report its number, title, closed state, and URL.

Do not remove labels on closure; they preserve backlog history.

## Safety

- Never close an ambiguous issue.
- Never claim completion without evidence and a successful close command.
- Never close an issue as `not planned` when the work was completed.
- Never treat an issue label as priority, even if its name matches a project priority option.
- Never edit a local todo file or create a Git commit for an issue operation.

## Response Pattern

1. Show the relevant listing or selected issue first.
2. Ask for selection only when matching is ambiguous.
3. For completion, state the evidence that will be posted.
4. After completion, report the evidence comment and confirmed closed issue URL.
