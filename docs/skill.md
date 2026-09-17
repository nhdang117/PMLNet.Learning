# The Claude Skill

The same material as this guide, packaged so an AI coding assistant applies the same rules you do.

It is a [Claude Code skill](https://claude.com/claude-code): a folder of Markdown that Claude loads when a task looks like AVEVA .NET customisation, plus the five compilable templates.

## What it knows

- **The fifteen non-negotiable rules** — x86, .NET Framework 4.7.2, `Private=False`, the `[assembly: PMLNetCallable()]` line, the four marshallable types, `IsValid` versus null, claim-before-write, thread confinement, and the rest.
- **Which model to use** — it is asked to choose between callable class, user control, addin, pseudo UDA and standalone *before* writing code, rather than defaulting to whatever the prompt hints at.
- **E3D namespaces, not PDMS ones** — it knows `Aveva.Core.*` and knows why most material online says `Aveva.Pdms.*`.
- **The manual steps** — that XML registration, `.uic` creation, Lexicon definitions and `pml rehash all` cannot be done in code, and must be handed back to you explicitly.

## Layout

```
skills/pmlnet/
    SKILL.md                       rules, model selection, workflows
    references/
        project-setup.md           csproj, framework, platform, deployment
        callable-class.md          PMLNetCallable classes
        user-control.md            controls on PML forms
        database.md                DbElement, attributes, collections, filters
        addin.md                   IAddinInjected, commands, windows
        pseudo-uda.md              DbPseudoAttribute registration
        standalone.md              headless applications
        api-cheatsheet.md          the calls, verified
        troubleshooting.md         symptom-first diagnosis
    assets/
        templates/                 the five starter projects
```

Claude reads `SKILL.md` first and pulls in a reference file only when the task needs it.

## Installing

Copy the skill folder into your Claude Code skills directory:

```bash
# Available in every project
cp -r skills/pmlnet ~/.claude/skills/

# Or just this project
cp -r skills/pmlnet .claude/skills/
```

Then ask for what you want:

> Create a PMLNetCallable class that reads an Excel file and returns the rows to PML as an array.

> Review this addin for E3D 3.1 compliance.

> Add a pseudo UDA that computes total weight from the members below an element.

## What it will and will not claim

The skill is explicit that code compiling is not the same as code working. Compiling needs the assemblies; working needs a running E3D and a project. It is instructed to say which of the two it actually did, and not to describe untested code as working.

## Keeping it in sync

`templates/` at the repository root is the canonical copy, along with `docs/reference/api-cheatsheet.md` and `docs/reference/troubleshooting.md`. The skill keeps its own copies so it stays self-contained when installed on its own.

After editing any of those, re-sync:

```powershell
pwsh tools/sync-skill-assets.ps1
```

`-Check` reports drift and exits non-zero instead of copying.
