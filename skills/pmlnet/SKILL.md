---
name: pmlnet
description: Write, scaffold and review C# .NET customisation for AVEVA Everything3D (E3D) 3.1 — PMLNetCallable classes, PML .NET user controls, CAF addins, pseudo UDAs and standalone applications. Use when the task mentions AVEVA, E3D, PDMS, PML, PMLNetCallable, DbElement, an addin for E3D, a pseudo UDA, or a .csproj that references Aveva.Core.* or PMLNet.dll.
---

# PML .NET for AVEVA E3D

Targets **AVEVA Everything3D 3.1**. Every rule here was verified against the shipped assemblies.

## Before writing any code

Decide which of the four integration models applies. Getting this wrong is expensive to undo.

| The task says | Model | Reference |
| --- | --- | --- |
| PML needs a capability it lacks | PMLNetCallable class | `references/callable-class.md` |
| A PML form needs a gadget PML lacks | PMLNetCallable user control | `references/user-control.md` |
| Present at E3D startup, toolbar or menu, no PML | CAF addin | `references/addin.md` |
| An attribute's value should be computed, not stored | Pseudo UDA (registered from an addin) | `references/pseudo-uda.md` |
| Runs with nobody logged in — batch, report, service | Standalone application | `references/standalone.md` |

Setup applies to all five: `references/project-setup.md`.
Reading or writing the model, and collecting elements: `references/database.md`.
API lookups: `references/api-cheatsheet.md`. Symptoms: `references/troubleshooting.md`.

Working templates that compile clean: `assets/templates/`.

## Non-negotiable rules

These are platform constraints, not style preferences. Violating any of them produces code that does not load, or fails silently.

1. **Target .NET Framework 4.7.2. Never .NET Core, .NET 5+, or a later Framework version.**
2. **Platform target x86 on every configuration.** E3D 3.1 is a 32-bit CLR host. AnyCPU is the Visual Studio default and is wrong here.
3. **`Private=False` (Copy Local = False) on every AVEVA reference.** Bind to what is already loaded.
4. **Never commit an AVEVA DLL.** Reference from `$(AVEVA_DESIGN_EXE)` and gitignore `*.dll`.
5. **Use `Aveva.Core.*` namespaces, not `Aveva.Pdms.*`.** The PDMS-era names in most online material do not exist in E3D. See the rename table in `references/api-cheatsheet.md`.
6. **`[assembly: PMLNetCallable()]` in `AssemblyInfo.cs`** for any assembly PML will use. Without it, PML imports the DLL and reports that the classes do not exist.
7. **Only four types cross the PML boundary**: `REAL`↔`double`, `STRING`↔`string`, `BOOLEAN`↔`bool`, `ARRAY`↔`Hashtable` (keys are `double`, indices start at **1**). Never return `int`.
8. **A PMLNetCallable class needs all of**: `[PMLNetCallable()]` on the public class, on a parameterless constructor, and on `Assign(OwnType that)`.
9. **`DbElement.GetElement` never returns null** — it returns an invalid element. Always check `.IsValid`.
10. **`DbAttribute.GetDbAttribute` *does* return null** for an attribute the project does not define. Always null-check.
11. **Claim before writing.** `element.Claim()` inside `try`/`catch (PdmsException)`.
12. **The AVEVA API is not thread-safe.** UI thread only.
13. **A pseudo UDA handler must never throw, do I/O, show UI, or write to the database.** It runs on every read.
14. **`Command.Key` must be globally unique.** Namespace it with company and assembly.
15. **Addin `Start` must not throw.** Wrap the body.

## Scaffolding workflow

1. **Confirm the model** from the table above. If the request is ambiguous, ask — do not guess between an addin and a callable class.
2. **Copy the matching template** from `assets/templates/`. Do not write a `.csproj` from scratch; the templates already encode rules 1–4.
3. **Rename**: folder, `.csproj`, `RootNamespace`, `AssemblyName`, new `ProjectGuid`, new `Guid` in `AssemblyInfo.cs`, and every `MyCompany` occurrence including command keys.
4. **Read the model's reference file** before writing the C#.
5. **Write the code**, keeping the template's shape.
6. **Build** with `AVEVA_DESIGN_EXE` set, and treat warnings as errors — several AVEVA APIs are obsolete in 3.1 and the compiler names the replacement.
7. **State the manual steps** the user must perform: XML registration, `.uic` creation in E3D, Lexicon UDA definitions, `pml rehash all`.

```bat
set AVEVA_DESIGN_EXE=C:\AVEVA\Plant\Everything3D3.1\
msbuild MyProject.csproj /t:Rebuild /p:Configuration=Release /warnaserror
```

## Review checklist

When reviewing existing PML .NET code, check in this order — earlier items cause the failures people report:

- [ ] `.csproj`: `v4.7.2`, `x86` on every configuration, `Private=False` on AVEVA references
- [ ] No AVEVA DLL committed; no reference to `*.Implementation.dll` or `*Internal.dll`
- [ ] No `Aveva.Pdms.*` namespace anywhere
- [ ] `[assembly: PMLNetCallable()]` present if PML uses the assembly
- [ ] Exposed methods return only `double`/`string`/`bool`/`Hashtable`/`void`
- [ ] `Hashtable` keys are `double` starting at 1
- [ ] `Assign` present and copying real state
- [ ] `.IsValid` checked after every `GetElement`
- [ ] Null checked after every `GetDbAttribute`
- [ ] `Claim()` before writes, inside `try`/`catch (PdmsException)`
- [ ] Multi-step changes wrapped in `UndoTransaction`
- [ ] `SaveWork` return value checked
- [ ] Addin: `Stop()` unsubscribes everything `Start` subscribed
- [ ] Addin: `Start` cannot throw
- [ ] Pseudo UDA: handler cannot throw, does no I/O, no UI, no writes
- [ ] No API call off the UI thread
- [ ] Collections rooted as tightly as possible; cheap filters first in `AndFilter`

## What to tell the user

Some steps cannot be done in code. Say so explicitly rather than leaving them implied:

- **Assembly locking.** Once E3D imports or loads the DLL, the file is locked until that process exits. Close E3D before rebuilding — there is no workaround.
- **Addin registration.** Add an entry to *your own copy* of `<Module>Addins.xml`, without the `.dll` extension. Never edit AVEVA's file in place.
- **Toolbar buttons.** `.uic` files are produced from inside E3D (right-click a toolbar → Customize…), then registered in your copy of `<Module>Customization.xml`. The button's command key must match `Command.Key` exactly.
- **PML forms.** A new `.pmlfrm` needs `pml rehash all` before `show` can find it.
- **Pseudo UDAs.** The UDA must exist in Lexicon **and be marked pseudo**. Code alone does nothing.
- **Standalone.** Needs E3D's environment, normally by running through a wrapper batch that calls `evars.bat`.

## Honesty about verification

You can write code that compiles without an E3D installation being present, but you cannot verify that it *works* — that needs a running E3D and a project. Say which of the two you did. Do not describe untested code as working.
