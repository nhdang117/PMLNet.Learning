# Glossary

## AVEVA terms

**Addin** — A .NET assembly E3D loads at startup because its name appears in the module's addins XML file. Implements `IAddin` or `IAddinInjected`.

**Attribute** — A named value on an element. Standard attributes are defined by the data model; UDAs are added per project.

**CAF** — Common Application Framework. The part of E3D that owns the user interface and component plumbing: commands, windows, addins, dependency resolution.

**Catalogue** — The database type holding standard components — the parts a design references rather than defines.

**CE** — Current Element. The element the user is "at". `CurrentElement.Element` in .NET, `!ce` in PML.

**Claim** — Exclusive write access to an element in a multi-write database. Taken with `Claim()`, given up with `Release()`, usually after `SaveWork`.

**Command** — In the CAF, a named unit of behaviour derived from `Aveva.ApplicationFramework.Presentation.Command`, triggered by tools that reference its `Key`.

**DB / Database** — One file in a project. Each has a type — Design, Catalogue, Draft, Dictionary, Engineering, Schematic, and others.

**Draft** — The drawing production module and its database type.

**Element** — One node in the model hierarchy. `DbElement` in .NET.

**Element type** — What an element *is*: `EQUIPMENT`, `NOZZLE`, `BRANCH`. `DbElementType` in .NET, with a static field per type on `DbElementTypeInstance`.

**Extract** — A working copy of a database for a user or group, merged back later. Part of AVEVA's multi-user model.

**E3D** — Everything3D. AVEVA's 3D design product, successor to PDMS.

**Global** — AVEVA's multi-site replication feature.

**Lexicon** — The module where UDAs and UDETs are defined. Where an attribute is marked pseudo.

**MDB** — Multiple Database. The set of databases a session works with. `MDB.CurrentMDB`.

**Module** — A functional area of E3D — Design, Draft, Paragon, Lexicon, Admin — each with its own addins XML, customization XML and database permissions.

**Paragon** — The catalogue and specification authoring module.

**PDMS** — Plant Design Management System. E3D's predecessor. Most .NET material on the internet targets PDMS 12.x and uses `Aveva.Pdms.*` namespaces.

**PML** — Programmable Macro Language. AVEVA's interpreted customisation language. PML1 is the older expression syntax; PML2 added objects and forms.

**PMLNETCONTROL** — The PML keyword on a `container` gadget that marks it as a host for a .NET user control.

**Pseudo attribute** — An attribute whose value is computed on demand rather than stored. Pseudo UDAs are the user-defined kind.

**Reference / Ref** — An element's database address, a pair of integers. Stable across renames.

**SaveWork** — Commit changes to the database. `MDB.CurrentMDB.SaveWork(comment)`.

**Site / Zone** — The top levels of the design hierarchy. `SITE` owns `ZONE`, which owns equipment, pipes and structures.

**Spec** — Specification. The rules constraining which catalogue components may be used where.

**UDA** — User Defined Attribute. A project-specific attribute, defined in Lexicon. Names start with `:`.

**UDET** — User Defined Element Type. A project-specific element type.

**.uic file** — User Interface Customization file. Holds toolbar and menu definitions produced by E3D's Customize dialog.

## .NET and platform terms

**AnyCPU** — A build that adapts to the host process bitness. **Wrong for E3D** — set x86 explicitly.

**Assembly** — A compiled .NET DLL or EXE.

**Copy Local / `Private`** — Whether MSBuild copies a referenced assembly into your output folder. **Must be `False`** for AVEVA references.

**Delegate** — A typed reference to a method. Pseudo UDA handlers and PML events are both delegate-based.

**Dependency resolution** — The CAF's service location. `resolver.GetImplementationOf<IWindowManager>()`.

**.NET Framework 4.7.2** — The runtime E3D 3.1 targets. Not .NET Core, not .NET 5 or later.

**Marshalling** — Converting values between PML and .NET. Only `REAL`/`double`, `STRING`/`string`, `BOOLEAN`/`bool` and `ARRAY`/`Hashtable` cross.

**x86** — 32-bit. What every assembly loaded into E3D 3.1 must be.

## This guide's terms

**The four models** — The four ways .NET code attaches to E3D: PMLNetCallable class, PMLNetCallable user control, addin, standalone application.

**Deployment folder** — A folder outside the E3D installation holding your DLL, your copies of the registration XML files, and your `.uic`. `C:\AVEVA\Custom\` throughout this guide.

**Ground truth** — In this guide, a statement verified by reading the assemblies in an E3D 3.1 installation rather than taken from older documentation.
