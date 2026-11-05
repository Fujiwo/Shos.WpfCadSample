# Project Guidelines

## Project

- This repository creates a simple WPF CAD application using C# 14 and .NET 10 with MVC and MVVM.
- Follow [create-wpf-cad.prompt.md](prompts/create-wpf-cad.prompt.md) for the application requirements; do not duplicate its feature specification here.
- Use the existing solution `Source\Shos.WpfCadSample\Shos.WpfCadSample.slnx`. Keep application and test projects under `Source\Shos.WpfCadSample\` and add them to that solution; do not create another solution.

## Repository layout

- `Plans\`: Markdown implementation plans.
- `Reviews\`: review results.
- `Source\Shos.WpfCadSample\`: solution and application/test projects.
- `.github\prompts\`: reusable task prompts.
- `.github\skills\`: skills available to GitHub Copilot.

## Plan before substantial implementation

- Before the initial CAD implementation or a multi-step change affecting design or scope, create a Markdown plan in `Plans\`.
- Name plans with the date and topic, for example `Plans\YYYYMMDD-topic.md`.
- Include the goal, scope and exclusions, requirements, design, ordered steps and completion criteria, test approach, risks, and open questions.
- Present the plan and wait for user approval before implementing it. Do not change application code for the initial CAD implementation before approval.
- Before running `create-wpf-cad.prompt.md`, confirm that its corresponding plan is approved; create and get approval for one first if needed.
- A simple, bounded maintenance fix does not require a plan or prior approval. If work needs a substantial change from an approved plan, update the plan and get approval before proceeding.

## Sustainable coding

- For code design, implementation, changes, tests, refactoring, reviews, bug fixes, and performance work, use the [sustainable-code-jp skill](skills/sustainable-code-jp/SKILL.md).
- Follow the skill's guidance on how much of its references to read for the task.
- Do not invoke the skill for work it excludes, such as documentation-only edits, Git-only operations, environment setup, configuration-value-only changes, build/CI configuration-only changes, or code explanations.
- The `.github\skills\` and `.claude\skills\` copies of `sustainable-code-jp` and `genshijin` currently match; keep corresponding files in sync.

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
