# 2. .NET Customisation Overview

## The four models

E3D can host your C# in four different ways. Choosing wrongly is the most expensive mistake in this whole guide, because it is the one you discover last.

```
                    ┌──────────────────────────────────────────┐
                    │        AVEVA E3D (32-bit process)         │
                    │                                           │
   PML code ──────► │  PML Engine ──► PML .NET Engine ──┐       │
                    │                                    ▼      │
                    │  Common Application Framework   Your DLL  │
                    │  (commands, windows, addins) ──►  (CAF)   │
                    │                                    │      │
                    │  .NET API (Database, Filters, ─────┘      │
                    │   Geometry, Graphics, Utilities)          │
                    │                  │                        │
                    └──────────────────┼────────────────────────┘
                                       ▼
                              Project databases
                                       ▲
                                       │
              Standalone .exe ─────────┘  (own process, no E3D UI)
```

### 1. PMLNetCallable class

A C# class marked with `[PMLNetCallable]`. PML imports the assembly and creates instances of it as if it were a native PML object.

- **PML required**: yes — something in PML has to create and call it.
- **Use when**: PML needs a capability it does not have. Random numbers, time zones, regular expressions, HTTP calls, reading a spreadsheet.
- **Chapter**: [5](./pmlnetcallable-class)

### 2. PMLNetCallable user control

The same thing, but the class derives from `System.Windows.Forms.UserControl`. A PML form declares a `container` gadget with the `PMLNETCONTROL` keyword, and the control is dropped into it.

- **PML required**: yes — the form is still PML.
- **Use when**: you need a gadget PML does not have. Date pickers, tree views, check-list boxes, web browsers, grids.
- **Chapter**: [8](./pmlnetcallable-user-control)

### 3. .NET addin

An assembly implementing `IAddinInjected` (or the older `IAddin`), listed in the module's addins XML file. E3D loads it at startup. It can register commands, create docked windows, subscribe to events, and register pseudo attribute handlers.

- **PML required**: no.
- **Use when**: you want your functionality present from the moment E3D starts, on a toolbar or menu, with no PML at all.
- **Chapter**: [9](./dotnet-addin)

### 4. Standalone application

Your own executable. It uses `Aveva.E3D.Standalone.Standalone` to start a headless E3D core, log into a project and open an MDB, then works with the databases through the same API.

- **PML required**: no. E3D does not even have to be running.
- **Use when**: batch jobs, reports, scheduled extracts, data migration, integration services.
- **Chapter**: [11](./standalone-interface)

## Choosing between them

| Question | Answer points to |
| --- | --- |
| Does a user click something inside E3D to start it? | Addin (toolbar) or user control (PML form) |
| Does PML already exist and just need one new capability? | PMLNetCallable class |
| Does it need to run with nobody logged in? | Standalone |
| Does it need to be visible the moment E3D starts? | Addin |
| Does it need to change how an attribute is calculated? | Pseudo UDA, registered from an addin ([Chapter 10](./pseudo-uda)) |

These combine. A common shape is one addin that registers commands, hosts a docked window, and registers pseudo attribute handlers — with a couple of PMLNetCallable classes alongside it for PML forms that already exist.

## The Common Application Framework

The CAF is the part of E3D that owns the user interface and the plumbing between components — commands, windows, addins, dependency injection.

Two assemblies:

| Assembly | Holds |
| --- | --- |
| `Aveva.ApplicationFramework.dll` | `IAddin`, `IAddinInjected`, `IDependencyResolver`, `ServiceManager`, `AddinManager` |
| `Aveva.ApplicationFramework.Presentation.dll` | `Command`, `ICommandManager`, `IWindowManager`, `DockedWindow`, tool types |

You need the CAF for anything that appears in the E3D user interface. You do not need it for a PMLNetCallable class that only does computation, and you must not use it from a standalone application — there is no UI to attach to.

## The .NET API assemblies

| Assembly | Provides |
| --- | --- |
| `Aveva.Core.Database.dll` | Elements, attributes, element types, databases, MDB, claims, rules, expressions |
| `Aveva.Core.Database.Filters.dll` | Collections and the filter classes that drive them |
| `Aveva.Core.Geometry.dll` | `Position`, `Direction`, `Orientation`, and the rest of the geometric primitives |
| `Aveva.Core.Utilities.dll` | Messaging, undo transactions, command-line access, tracing |
| `Aveva.Core.Presentation.dll` | `CurrentElement`, selection, presentation-side services |
| `Aveva.Core3D.Graphics.dll` | Drawlist and colour |
| `Aveva.E3D.Standalone.dll` | The standalone entry point |
| `PMLNet.dll` | `[PMLNetCallable]`, `PMLNetException`, the event delegate |

[Chapter 4](./api-and-dlls) covers these in detail, including which ones you actually reference for each kind of project.

::: danger Treat undocumented assemblies as private
An E3D installation folder holds several hundred DLLs. The ones listed above and in Chapter 4 are the public API. Anything else is internal: it can change or disappear between service packs without notice, and referencing it is unsupported. If you find yourself referencing `*.Implementation.dll`, stop and find the public interface.
:::

## Customising the user interface

Commands are the unit of behaviour in the CAF. A command is a class deriving from `Aveva.ApplicationFramework.Presentation.Command` with an `Execute()` override and a unique `Key`.

Tools are the unit of presentation — buttons, menu items, combo boxes, and about ten more types. A tool is bound to a command by key, and several tools can drive the same command.

Tool definitions live in **`.uic` files**, which you produce from inside E3D (right-click a toolbar → **Customize…**) rather than by hand. A `.uic` file is registered by listing it in `<ModuleName>Customization.xml`. [Chapter 9](./dotnet-addin) walks through the whole loop.

## Where the official material lives

Your installation ships the authority:

| What | Where |
| --- | --- |
| .NET Customisation User Guide | The E3D help suite |
| Per-assembly API reference (`.chm`) | `%AVEVA_DESIGN_EXE%\Documentation\` |
| Sample projects | `%AVEVA_DESIGN_EXE%\Samples.zip` |

The samples are worth unpacking before you write anything. `ExamplesAddin` in particular is a tour of the database API — one small class per topic, each with a `Run()` method.

## Checklist

- [ ] You can name which of the four models your task needs, and why.
- [ ] You know whether PML is in the picture at all.
- [ ] You have unpacked `Samples.zip` and opened `ExamplesAddin`.
- [ ] You have found the `.chm` files in your `Documentation` folder.

## Next

[Chapter 3](./project-setup) sets up a project with the right target framework, platform and references — the settings that decide whether E3D loads your DLL or ignores it.
