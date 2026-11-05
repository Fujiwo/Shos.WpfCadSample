# Project Guidelines

## Project

- This repository creates a simple WPF CAD application using C# 14 and .NET 10 with MVC and MVVM.
- Follow [create-wpf-cad.prompt.md](prompts/create-wpf-cad.prompt.md) for the application requirements; do not duplicate its feature specification here.
- Use the existing solution `Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx`. Keep application and test projects under `Source\Shos.WpfCadSample\` and add them to that solution; do not create another solution.

## Repository layout

- `Plans\`: Markdown implementation plans.
- `Documents\`: dated Markdown specifications and design documents.
- `Reviews\`: review reports.
- `Source\Shos.WpfCadSample\`: solution and application/test projects.
- `.github\prompts\`: reusable task prompts.
- `.github\skills\`: skills available to GitHub Copilot.

## Prompt and review artifacts

- When creating a reusable prompt, create a new `.prompt.md` file under `.github\prompts\`. Do not replace or overwrite an existing prompt; choose a distinct name and update relevant references.
- When conducting a requested review, save the review result as a new Markdown file under `Reviews\`, named `YYYYMMDD-<subject>-review.md`. Do not overwrite an existing review; add a distinguishing suffix if needed.
- Include the review scope, findings (or explicitly state that no findings were found), and relevant file paths/locations in the report. Link the report when presenting the result.

## Plan before substantial implementation

- Before the initial CAD implementation, use [create-cad-plan.prompt.md](prompts/create-cad-plan.prompt.md) to create and obtain approval for these separate documents in order: a dated Markdown specification in `Documents\` (including features, UI design, and conceptual data structures), a dated Markdown architecture/class design in `Documents\` (including needed UML), then dated Markdown implementation and test plans in `Plans\`.
- Create and obtain approval for each stage before creating the next. Do not start implementation until the specification, architecture/class design, implementation plan, and test plan are all approved.
- Create `Documents\` or `Plans\` when needed. Mark each new document `状態：レビュー待ち`; change it to `状態：承認済み` only after the user explicitly approves it. Revert changed approved documents and affected downstream documents to review pending.
- For other multi-step changes affecting design or scope, create a Markdown plan in `Plans\`.
- Include the date and topic in each document filename, for example `Documents\YYYYMMDD-cad-specification.md`, `Documents\YYYYMMDD-cad-architecture.md`, and `Plans\YYYYMMDD-cad-implementation-plan.md`.
- Include the goal, scope and exclusions, requirements, design, ordered steps and completion criteria, test approach, risks, and open questions.
- Present plans and wait for user approval before implementing them. Do not change application code for the initial CAD implementation before all CAD documents are approved.
- Before running `create-wpf-cad.prompt.md`, confirm that the CAD specification, architecture/class design, implementation plan, and test plan are all approved; create and get approval for any missing document first.
- A simple, bounded maintenance fix does not require a plan or prior approval. If work needs a substantial change from an approved plan, update the plan and get approval before proceeding.

## Sustainable coding

- For code design, implementation, changes, tests, refactoring, code reviews, bug fixes, and performance work, use the [sustainable-code-jp skill](skills/sustainable-code-jp/SKILL.md).
- Use the skill for architecture/design document reviews when they involve design judgments. Do not use it for reviews limited to repository instructions, prompts, or other documentation; those are documentation-only work.
- Follow the skill's guidance on how much of its references to read for the task.
- Do not invoke the skill for work it excludes, such as documentation-only edits, Git-only operations, environment setup, configuration-value-only changes, build/CI configuration-only changes, or code explanations.
- Before relying on or updating mirrored `sustainable-code-jp` or `genshijin` skills, compare their relative file sets and contents. If they differ, report the discrepancy rather than assuming they match or silently synchronizing them; keep corresponding copies synchronized when an approved change applies to both.

## CAD conventions

- Keep types out of the root namespace; use role-based namespaces such as `Models`, `ViewModels`, `Views`, and `Controllers`.
- Keep XAML code-behind free of application logic other than `InitializeComponent()`.
- Prefer `record`, `struct`, or `record struct` for suitable value data, and use current C# syntax where it improves clarity.
- Use `Shos.UndoRedoList`'s `UndoRedoObservableCollection<T>` for Undo/Redo.
- Do not add features outside the agreed requirements.

## Build and test

- Once application and test projects are present in the solution, run these commands from the repository root:
  - `dotnet build Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx`
  - `dotnet test Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx`
- Until those projects exist, do not claim these commands were run or are passing; report why build/test verification was unavailable.
- Report the verification commands run and their results. Do not commit secrets.
