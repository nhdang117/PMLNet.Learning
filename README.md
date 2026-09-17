# PML .NET for AVEVA E3D

A practical guide to .NET customisation for **AVEVA Everything3D 3.1**, plus five compilable starter projects and a Claude Code skill built from the same material.

**📖 [Read the guide](https://nhdang117.github.io/PMLNet.Learning/)**

Every namespace, class and signature was read out of the assemblies shipped with an E3D 3.1 installation — not copied from an older PDMS manual, where the names are different and no longer compile.

## What is here

| Path | What it is |
| --- | --- |
| `docs/` | The guide — eleven chapters plus reference pages. Published to GitHub Pages. |
| `templates/` | Five starter projects, one per integration model. All compile clean. |
| `skills/pmlnet/` | A Claude Code skill: the same rules, packaged for an AI assistant. |
| `tools/` | Repository maintenance scripts. |

## The eleven chapters

1. Introduction
2. .NET Customisation Overview — the four integration models
3. How to Set Up a PML .NET Project
4. The E3D PML .NET API and the DLLs You Need
5. How to Create a PMLNetCallable Class
6. Database Interface
7. Collections and Filters
8. How to Create a PMLNetCallable User Control
9. How to Create a .NET Addin for E3D
10. How to Create a Pseudo UDA
11. How to Create a Standalone Interface

Plus an API cheatsheet, a DLL map, a type-mapping table, symptom-first troubleshooting and a glossary.

## The four things that matter most

1. **Target .NET Framework 4.7.2 and platform x86.** E3D 3.1 is a 32-bit CLR host. AnyCPU or x64 assemblies do not load.
2. **Reference AVEVA DLLs in place** from your installation, with Copy Local off. Never commit them.
3. **Mark everything you expose** — the assembly, the class, every constructor, `Assign`, and each member PML calls.
4. **Restart E3D after rebuilding.** An imported assembly is locked for the life of the process.

## Templates

```bat
set AVEVA_DESIGN_EXE=C:\AVEVA\Plant\Everything3D3.1\
msbuild templates\CallableClass\CallableClass.csproj /t:Rebuild /p:Configuration=Release
```

| Template | For |
| --- | --- |
| `CallableClass/` | A C# class PML creates and calls |
| `UserControl/` | A WinForms control on a PML form |
| `Addin/` | Code loaded at E3D startup, with a command and docked window |
| `PseudoUda/` | UDAs computed in C# on every read |
| `Standalone/` | A batch tool that runs outside E3D |

See [`templates/README.md`](templates/README.md).

## The Claude skill

```bash
cp -r skills/pmlnet ~/.claude/skills/
```

Then ask for what you need — a callable class, an addin, a pseudo UDA — and Claude scaffolds it against the same rules this guide teaches. Details in [`docs/skill.md`](docs/skill.md).

## Running the site locally

```bash
npm install
npm run docs:dev      # http://localhost:5173
npm run docs:build
```

Pushes to `main` publish through GitHub Actions.

## Contributing

Corrections are welcome, particularly from other E3D versions. Two ground rules:

- **No AVEVA DLLs, PDFs or training material in the repository.** That content is licensed to its owners. Reference it, do not redistribute it.
- **Verify before you write.** If a signature came from an assembly or from code you ran, say which. If it came from memory, do not add it.

After editing `templates/`, `docs/reference/api-cheatsheet.md` or `docs/reference/troubleshooting.md`, re-sync the skill's copies:

```powershell
pwsh tools/sync-skill-assets.ps1
```

## Not an AVEVA publication

An independent community guide, not produced, reviewed or endorsed by AVEVA. AVEVA's own documentation and the API help files in `%AVEVA_DESIGN_EXE%\Documentation\` are the authority for your installation.

AVEVA, Everything3D, E3D and PDMS are trademarks of AVEVA Group plc.

## Licence

[MIT](LICENSE) — the guide, the templates and the skill. Not the AVEVA software they describe.
