# 4. The E3D PML .NET API and the DLLs You Need

An E3D installation folder holds several hundred assemblies. About a dozen of them are the public API. This chapter is the map.

Everything below was read out of the assemblies shipped with **E3D 3.1** (`Aveva.Core.*` version 1.3.0.0, `Aveva.ApplicationFramework` 4.0.0.0, `PMLNet` 2.0.0.0), targeting **.NET Framework 4.7.2**, all 32-bit.

## The public assemblies

| Assembly | Namespace(s) | What lives there |
| --- | --- | --- |
| `PMLNet.dll` | `Aveva.Core.PMLNet` | `PMLNetCallable`, `PMLNetException`, `PMLNetDelegate`, `PMLNetAny` |
| `Aveva.Core.Database.dll` | `Aveva.Core.Database` | `DbElement`, `DbAttribute`, `DbElementType`, `MDB`, `Db`, `Project`, `CurrentElement`, `DbPseudoAttribute`, `DbExpression`, `DbRule` |
| `Aveva.Core.Database.Filters.dll` | `Aveva.Core.Database.Filters` | `DBElementCollection`, `DBElementEnumerator`, and ~50 filter classes |
| `Aveva.Core.Geometry.dll` | `Aveva.Core.Geometry` | `Position`, `Direction`, `Orientation`, `LimitsBox`, `Plane`, `Line`, `Arc`, grids |
| `Aveva.Core.Utilities.dll` | `Aveva.Core.Utilities.*` | `PdmsMessage`, `PdmsException`, `UndoTransaction`, `PdmsTrace`, `Command` (command line), `Interrupt`, `Bore` |
| `Aveva.Core.Presentation.dll` | `Aveva.Core.Presentation` | `Selection`, `FilteredListView`, `ElementTypeSelector`, `PMLNetDruid`, clipboard |
| `Aveva.Core.Presentation.DataGrid.dll` | `Aveva.Core.Presentation.DataGrid` | The AVEVA grid control used by PML's `GridControl` |
| `Aveva.Core3D.Graphics.dll` | `Aveva.Core3D.Graphics` | Drawlist, colours, 3D view interaction |
| `Aveva.ApplicationFramework.dll` | `Aveva.ApplicationFramework` | `IAddin`, `IAddinInjected`, `IDependencyResolver`, `DependencyResolver`, `ServiceManager`, `AddinManager` |
| `Aveva.ApplicationFramework.Presentation.dll` | `Aveva.ApplicationFramework.Presentation` | `Command`, `ICommandManager`, `IWindowManager`, `DockedWindow`, tool types |
| `Aveva.E3D.Standalone.dll` | `Aveva.E3D.Standalone` | `Standalone` — the entry point for out-of-process applications |
| `PMLNetUtilities.dll` | `Aveva.Core.PMLNet` | Ready-made PML-callable helpers: `PDMSEvars`, `PMLClipboard`, `PMLImageViewerControl`, `StageBar`, `Wheel` |

::: danger Everything else is private
If an assembly is not on this list, treat it as internal to AVEVA. `*.Implementation.dll` in particular exists to be replaced between releases. Referencing it compiles today and breaks on the next service pack.
:::

## Which references for which project

Start from the smallest set and add as the compiler complains.

### PMLNetCallable class (no database access)

```
PMLNet
System, System.Core
```

### PMLNetCallable class (reads or writes the model)

```
PMLNet
Aveva.Core.Database
Aveva.Core.Geometry          (only if you touch Position/Direction/Orientation)
Aveva.Core.Utilities         (only if you catch PdmsException or use undo)
```

### PMLNetCallable user control

```
PMLNet
System.Windows.Forms, System.Drawing
+ whatever the class above needs
```

### .NET addin

```
Aveva.ApplicationFramework
Aveva.ApplicationFramework.Presentation
Aveva.Core.Database
Aveva.Core.Presentation      (for CurrentElement-driven UI, Selection)
System.Windows.Forms, System.Drawing
```

### Standalone application

```
Aveva.E3D.Standalone
Aveva.Core.Database
Aveva.Core.Utilities
```

Never reference the CAF assemblies from a standalone application. There is no application to attach to and the calls will fail at runtime.

## PDMS to E3D: the rename table

Most .NET customisation material on the internet targets PDMS 12.x. The types are broadly the same; the assemblies and namespaces are not.

| PDMS 12.x | E3D 3.1 |
| --- | --- |
| `Aveva.Pdms.Database.dll` / `Aveva.Pdms.Database` | `Aveva.Core.Database.dll` / `Aveva.Core.Database` |
| `PDMSFilters.dll` / `Aveva.Pdms.Database.Filters` | `Aveva.Core.Database.Filters.dll` / `Aveva.Core.Database.Filters` |
| `Aveva.Pdms.Geometry.dll` / `Aveva.Pdms.Geometry` | `Aveva.Core.Geometry.dll` / `Aveva.Core.Geometry` |
| `Aveva.Pdms.Utilities.dll` / `Aveva.Pdms.Utilities.*` | `Aveva.Core.Utilities.dll` / `Aveva.Core.Utilities.*` |
| `Aveva.Pdms.Shared.dll` | Split across `Aveva.Core.Database` and `Aveva.Core.Presentation` |
| `Aveva.Pdms.Graphics.dll` | `Aveva.Core3D.Graphics.dll` |
| `Aveva.Pdms.Standalone.dll` / `PdmsStandalone` | `Aveva.E3D.Standalone.dll` / `Aveva.E3D.Standalone.Standalone` |
| `Aveva.PDMS.PMLNet` (namespace) | `Aveva.Core.PMLNet` (namespace, still in `PMLNet.dll`) |

Two behavioural differences worth knowing:

- **`PdmsStandalone` became `Aveva.E3D.Standalone.Standalone`**, with the same static `Start()` / `Open()` / `Close()` shape, plus static `MDB` and `Project` properties.
- **`IAddin` gained a sibling, `IAddinInjected`**, which receives an `IDependencyResolver` instead of a `ServiceManager`. New addins should implement `IAddinInjected`. See [Chapter 9](./dotnet-addin).

## The types you will use every day

### From `Aveva.Core.Database`

| Type | Role |
| --- | --- |
| `DbElement` | One element. Struct-like value; check `.IsValid` before use. |
| `DbAttribute` | An attribute *definition*, not a value. Get them from `DbAttributeInstance`. |
| `DbAttributeInstance` | Static fields for every standard attribute: `NAME`, `DESC`, `POS`, `XLEN`… |
| `DbElementType` | An element type definition. |
| `DbElementTypeInstance` | Static fields for every element type: `EQUIPMENT`, `NOZZLE`, `BRANCH`… |
| `MDB` | `MDB.CurrentMDB` — databases, worlds, save/get/quit work, claims |
| `Db` | A single database within the MDB |
| `Project` | `Project.CurrentProject` — open, close, project-level information |
| `CurrentElement` | `CurrentElement.Element` plus the `CurrentElementChanged` event |
| `DbQualifier` | Array index or type qualifier for attribute access |
| `DbExpression` | A parsed PML1 expression, evaluated against an element |
| `DbPseudoAttribute` | Registration point for pseudo UDA handlers ([Chapter 10](./pseudo-uda)) |

### From `Aveva.Core.Geometry`

`Position`, `Direction` and `Orientation` are the three you need constantly. They are classes, not structs, and they carry units — do not try to round-trip them through `double` unless you know which unit you are in.

### From `Aveva.Core.Utilities`

| Type | Namespace | Role |
| --- | --- | --- |
| `PdmsMessage` | `.Messaging` | Structured error returned by `out` parameters |
| `PdmsException` | `.Messaging` | What the API throws when an operation is refused |
| `UndoTransaction` | `.Undo` | Wrap database changes so the user can undo them |
| `UndoSubscriber` | `.Undo` | Put your own in-memory state on the undo stack |
| `PdmsTrace` | `.Tracing` | Diagnostic tracing that goes where E3D's own traces go |
| `Command` | `.CommandLine` | Run a PML/PDMS command line string from .NET |

### From `PMLNet`

| Type | Role |
| --- | --- |
| `PMLNetCallable` | The attribute. Goes on the assembly, the class, constructors, and each exposed member. |
| `PMLNetException` | Throw this to raise a catchable PML error with a module and error number. |
| `PMLNetDelegate.PMLNetEventHandler` | The one event signature PML can subscribe to. |
| `PMLNetAny` | The other direction: call *into* PML from .NET. |

## Finding a member you cannot remember

Three options, in order of speed:

1. **Visual Studio Object Browser** — add the reference, then browse the assembly. Signatures, but no prose.
2. **The `.chm` files** in `%AVEVA_DESIGN_EXE%\Documentation\` — one per public assembly, with AVEVA's own descriptions. Unblock the file in Windows properties or the pages render empty.
3. **`ExamplesAddin`** from `Samples.zip` — working code for most of the database API, one topic per file.

The [API Cheatsheet](../reference/api-cheatsheet) in this guide covers the calls that make up the great majority of real customisation code.

## Thread safety

**The AVEVA API is not thread-safe.** Call it only from the thread that E3D calls you on — which in practice means the UI thread.

If you need background work, do the computation on a worker thread and marshal back to the UI thread (`Control.Invoke`) before touching any `Db*` type. A background thread reading `DbElement` while the user edits the model is a crash waiting for a busy day.

## Checklist

- [ ] You know which assemblies your project type needs.
- [ ] You are referencing no `*.Implementation.dll`.
- [ ] Any code you copied from a PDMS-era source has been translated through the rename table.
- [ ] All API calls happen on the UI thread.

## Next

[Chapter 5](./pmlnetcallable-class) writes the first real thing: a C# class PML can create and call.
