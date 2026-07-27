---
name: todo
description: Use when the user asks to add a todo, capture follow-up work, note an idea for later, or turn feedback into a GitHub backlog issue.
---

# Todo

Treat every requested todo as a draft. GitHub Issues are the only backlog source; never write to a local todo file.

## Issue Repository

All issue operations target `caneva20/factory-planner`. The current repository contains the implementation, but does not host its own backlog.

## Project And Priority

Every new todo goes to the requested GitHub Project. When the user does not specify one, use the repository's saved default:

1. Read `git config --local --get todo.project-owner` and `git config --local --get todo.project-number`.
2. If either value is missing, run `gh project list --owner caneva20 --format json`, ask the user to choose once, and save the answer with `git config --local todo.project-owner <owner>` and `git config --local todo.project-number <number>`.
3. An explicitly requested project, including a request for no project, overrides the default for that todo only. Change the saved default only when the user asks.
4. Validate the selected project with `gh project view <number> --owner <owner> --format json`.
5. Read its fields with `gh project field-list <number> --owner <owner> --format json`. Require one single-select field named `Priority` and use only its current options. Never represent priority with an issue label.

If Projects access is missing, stop with the remediation reported by `gh` (normally `gh auth refresh --hostname github.com --scopes project`).

## Labels

Every todo has the `todo` label. Add contextual labels when applicable:

- `engine`: The work relates to the shared game engine.
- `game:<game-id>`: The work relates to a specific game, for example `game:classic`.

Contextual labels are independent. An engine-related todo for a specific game has both labels.

Priority labels are forbidden. The project's `Priority` field is authoritative.

## Workflow

1. Run `gh auth status` and stop with the reported remediation if authentication fails.
2. Run `gh repo view caneva20/factory-planner --json nameWithOwner,url,owner` to confirm the target repository.
3. Resolve and validate the destination project as described above unless the user explicitly requested no project.
4. Review the request and search open and closed todo issues for likely duplicates with `gh issue list --repo caneva20/factory-planner --state all --label todo --search "<key terms> in:title,body" --limit 100`.
5. State the apparent objective, affected area, destination project, applicable contextual labels, and selected project priority.
6. Ask one short question at a time only if a material ambiguity would make the objective, labels, project, or priority unclear. Priority must exactly match a current option from the selected project's `Priority` field. Do not run a full feature-design workflow.
7. Draft one issue using the required format below.
8. Show the complete title, body, non-priority labels, destination project, and project priority. Require explicit user approval before creation.
9. Create it with `gh issue create --repo caneva20/factory-planner --title "<title>" --body "<body>" --label todo`, adding `--label engine` and/or `--label "game:<game-id>"` when applicable. Capture the returned issue URL.
10. Add the issue with `gh project item-add <number> --owner <owner> --url <issue-url> --format json` and capture the project item ID.
11. Set `Priority` with `gh project item-edit --id <item-id> --project-id <project-id> --field-id <priority-field-id> --single-select-option-id <priority-option-id>`.
12. Re-read the project items, verify the issue is present with the selected priority, and report its issue number and URL.

When the user explicitly requests no project, skip project priority selection and steps 10-11.

## Required Issue Format

```markdown
## Objective

<One concise outcome statement.>

## Context

<Only the context and affected area needed to understand the work.>

## Acceptance Criteria

- [ ] <A short, observable completion condition.>
```

Use a concise outcome-oriented title. Keep the body specific enough for a later implementation pass, but do not prematurely design the solution.

## Duplicate Handling

- Show plausible duplicates before drafting a new issue.
- Ask whether to update or use the existing issue when similarity is meaningful.
- Never create a duplicate automatically.

## Safety

- Never fall back to a local todo file or another local backlog.
- If `todo` or an applicable contextual label is missing, report the configuration error and stop. Label creation belongs to migration or setup.
- Never create or apply a priority label, even when a project priority option has the same name.
- Never create an issue before draft approval.
- If issue creation succeeds but adding it to the project or setting priority fails, report the partial result and issue URL. Do not create a replacement issue.
- Never create a Git commit for an issue operation.

## Response Pattern

1. Give the short objective review and ask only necessary clarification questions.
2. Show any duplicate candidate.
3. Show the complete issue draft, destination project, and project priority, then ask for approval.
4. After creation, report the issue number, title, non-priority labels, project, priority, and URL.
