---
name: todo-migrate
description: Use when a file-backed TODO.md backlog must be migrated into GitHub Issues, resumed after an interrupted import, verified, or retired safely.
---

# Todo Migrate

Move legacy backlog history to GitHub Issues without losing state or provenance.

## Issue Repository

All issue operations target `caneva20/factory-planner`. The current repository contains the implementation, but does not host its own backlog.

## Project And Priority

Use a project explicitly requested for the migration. Otherwise read `git config --local --get todo.project-owner` and `git config --local --get todo.project-number`. If either is missing, run `gh project list --owner caneva20 --format json`, ask the user to choose once, and save both values in local Git config. Do not change the saved default unless the user asks.

Fetch the selected project and fields with `gh project view` and `gh project field-list`. Require one single-select field named `Priority`. Its current options are the only valid priorities; never create or apply priority labels.

Old priorities are not authoritative. Confirm active priorities against the project options. For completed history, reuse an exact matching project option when justified by the source; otherwise ask once which project priority option should be the migration default and reuse that answer.

## Preflight And Setup

1. Run `gh auth status`.
2. Run `gh repo view caneva20/factory-planner --json nameWithOwner,url,owner`.
3. Resolve the selected/default project and its `Priority` field. If Projects access is missing, stop with the reported remediation, normally `gh auth refresh --hostname github.com --scopes project`.
4. Read `TODO.md`; confirm 12 active and 39 completed entries.
5. Run `gh label list --repo caneva20/factory-planner --limit 100`. If missing, create only the backlog label with `gh label create todo --repo caneva20/factory-planner --color 0E8A16 --description "Tracked backlog work"`:

| Name | Color | Description |
|---|---|---|
| `todo` | `0E8A16` | Tracked backlog work |
Stop on authentication, repository, parsing, or label failures. Do not modify `TODO.md`.

## Provenance And Idempotency

Every migrated issue body ends with a deterministic marker:

```html
<!-- housing-game-todo-legacy:T### -->
```

Fetch bodies with `gh issue list --repo caneva20/factory-planner --state all --label todo --limit 200 --json number,body,state,labels,url`. Reuse one exact marker match; stop on multiple matches; create only when none exists.

## Active Item Workflow

For each active item, in source order:

1. Summarize the apparent objective and affected area.
2. Ask one focused question at a time only until the objective is clear.
3. Confirm one priority from the selected project's current `Priority` options.
4. Draft a title and body with `Objective`, `Context`, unchecked `Acceptance Criteria`, `Legacy ID`, `Created`, and the marker.
5. Show the full draft, `todo` label, destination project, and project priority; require explicit approval.
6. Create the issue with `gh issue create --repo caneva20/factory-planner`, add it to the selected project with `gh project item-add`, and set its `Priority` with `gh project item-edit`.
7. Verify the project item, then report its number and URL. If refinement proves the item obsolete, require approval before closing it as `not planned`.

Do not invoke a full feature-design workflow.

## Completed Item Workflow

For each completed item, in source order:

1. Select an exact current project priority option from source evidence or use the user-approved migration default.
2. Preserve the outcome as `Objective`.
3. Record `Legacy ID`, `Created`, `Completed`, the selected-priority note, a checked acceptance criterion, and the marker.
4. Create with `gh issue create --repo caneva20/factory-planner`, add it to the selected project, set its project `Priority`, then run `gh issue close <number> --repo caneva20/factory-planner --reason completed`.

Completed history does not require individual user questions or draft approval.

## Verification And Source Removal

Before deleting `TODO.md`, query all migrated issues and verify:

- There are 51 unique legacy markers.
- All 12 source-active markers have their individually approved final state.
- All 39 source-completed markers identify issues closed as `completed`.
- Every migrated issue has the `todo` label, belongs to the selected project, and has one current `Priority` option set.
- No migrated issue receives a priority label.
- Every migrated issue contains the required provenance dates.
- No legacy ID maps to multiple issues.

On any failure, report the mismatch and keep `TODO.md`. On success, show the results and require approval before deletion.

After deletion, run `git diff --check` and `git status --short`. Never commit unless the user explicitly requests it.
