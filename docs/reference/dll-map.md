# DLL Map

Which assembly holds what, for **AVEVA E3D 3.1**. All paths are relative to `%AVEVA_DESIGN_EXE%`, typically `C:\AVEVA\Plant\Everything3D3.1\`.

All of these are **.NET Framework 4.7.2** and **32-bit**. Your project must match.

## The public set

| Assembly | Version | Namespace | Reference it when |
| --- | --- | --- | --- |
| `PMLNet.dll` | 2.0.0.0 | `Aveva.Core.PMLNet` | PML will use your class |
| `PMLNetUtilities.dll` | 1.3.0.0 | `Aveva.Core.PMLNet` | You want AVEVA's ready-made PML-callable helpers |
| `Aveva.Core.Database.dll` | 1.3.0.0 | `Aveva.Core.Database` | You touch the model at all |
| `Aveva.Core.Database.Filters.dll` | 1.3.0.0 | `Aveva.Core.Database.Filters` | You collect or filter elements |
| `Aveva.Core.Geometry.dll` | 1.3.0.0 | `Aveva.Core.Geometry` | You use `Position`, `Direction`, `Orientation` |
| `Aveva.Core.Utilities.dll` | 1.3.0.0 | `Aveva.Core.Utilities.*` | Messaging, undo, tracing, command line |
| `Aveva.Core.Presentation.dll` | 1.3.0.0 | `Aveva.Core.Presentation` | Selection, list views, presentation services |
| `Aveva.Core.Presentation.DataGrid.dll` | 1.3.0.0 | `Aveva.Core.Presentation.DataGrid` | You want AVEVA's grid |
| `Aveva.Core3D.Graphics.dll` | 1.3.0.0 | `Aveva.Core3D.Graphics` | Drawlist and colours |
| `Aveva.ApplicationFramework.dll` | 4.0.0.0 | `Aveva.ApplicationFramework` | You are writing an addin |
| `Aveva.ApplicationFramework.Presentation.dll` | 4.0.0.0 | `Aveva.ApplicationFramework.Presentation` | Commands, windows, tools |
| `Aveva.E3D.Standalone.dll` | 1.3.0.0 | `Aveva.E3D.Standalone` | Your code runs outside E3D |
| `Aveva.Core3D.Standalone.dll` | 1.3.0.0 | `Aveva.Core3D.Standalone` | Standalone with 3D graphics |
| `Aveva.Core.PMLPseudos.dll` | 1.3.0.0 | `Aveva.Core.PMLPseudos` | AVEVA's own pseudo-attribute addin — reference, not a dependency |

All are signed with public key token `17c64733a9775004`.

## Type index

Looking for a type and not sure where it lives:

| Type | Assembly |
| --- | --- |
| `DbElement`, `DbAttribute`, `DbElementType` | `Aveva.Core.Database` |
| `DbAttributeInstance`, `DbElementTypeInstance` | `Aveva.Core.Database` |
| `MDB`, `Db`, `Project`, `MDBSetup` | `Aveva.Core.Database` |
| `CurrentElement`, `CurrentElementChangedEventArgs` | `Aveva.Core.Database` |
| `DbQualifier`, `DbExpression`, `DbRule` | `Aveva.Core.Database` |
| `DbPseudoAttribute` | `Aveva.Core.Database` |
| `DbType`, `DbAttributeUnit`, `DbDimension` | `Aveva.Core.Database` |
| `DBElementCollection`, `DBElementEnumerator` | `Aveva.Core.Database.Filters` |
| `BaseFilter` and every `*Filter` | `Aveva.Core.Database.Filters` |
| `ElementTreeNavigator`, `CompoundFilter` | `Aveva.Core.Database.Filters` |
| `Position`, `Direction`, `Orientation`, `LimitsBox` | `Aveva.Core.Geometry` |
| `PdmsMessage`, `PdmsException`, `PdmsOutput` | `Aveva.Core.Utilities` |
| `UndoTransaction`, `UndoSubscriber`, `UndoCaretaker` | `Aveva.Core.Utilities` |
| `PdmsTrace`, `Interrupt` | `Aveva.Core.Utilities` |
| `Command` (the PML command line one) | `Aveva.Core.Utilities` |
| `Selection`, `FilteredListView`, `ElementTypeSelector` | `Aveva.Core.Presentation` |
| `DataGridControl`, `PMLDataGridControl` | `Aveva.Core.Presentation.DataGrid` |
| `IAddin`, `IAddinInjected`, `IDependencyResolver` | `Aveva.ApplicationFramework` |
| `ServiceManager`, `DependencyResolver`, `AddinManager` | `Aveva.ApplicationFramework` |
| `Command` (the CAF one), `ICommandManager` | `Aveva.ApplicationFramework.Presentation` |
| `IWindowManager`, `DockedWindow`, `DockedPosition` | `Aveva.ApplicationFramework.Presentation` |
| `PMLNetCallable`, `PMLNetException`, `PMLNetDelegate` | `PMLNet` |
| `PMLNetAny`, `PMLNetTrace` | `PMLNet` |
| `Standalone` | `Aveva.E3D.Standalone` |

::: warning Two classes called `Command`
`Aveva.ApplicationFramework.Presentation.Command` is the CAF command you derive from for toolbar behaviour. `Aveva.Core.Utilities.CommandLine.Command` runs a PML command string. If both namespaces are in scope you must qualify.
:::

## Minimum references by project type

**PMLNetCallable class, computation only**

```
PMLNet
```

**PMLNetCallable class, reads the model**

```
PMLNet
Aveva.Core.Database
Aveva.Core.Geometry      (if Position/Direction/Orientation)
Aveva.Core.Utilities     (if PdmsException or undo)
```

**PMLNetCallable user control**

```
PMLNet
System.Windows.Forms
System.Drawing
+ the above as needed
```

**Addin**

```
Aveva.ApplicationFramework
Aveva.ApplicationFramework.Presentation
Aveva.Core.Database
Aveva.Core.Presentation
System.Windows.Forms
System.Drawing
```

**Pseudo UDA addin**

```
Aveva.ApplicationFramework
Aveva.Core.Database
```

**Standalone**

```
Aveva.E3D.Standalone
Aveva.Core.Database
Aveva.Core.Utilities
```

## What not to reference

| Pattern | Why |
| --- | --- |
| `*.Implementation.dll` | Internal implementations behind the public interfaces. Replaced without notice. |
| `*Internal.dll` | The name is the warning. |
| `DevExpress.*` | Third-party UI libraries shipped for AVEVA's own UI. Not licensed to you through E3D. |
| `Teigha.*`, `AWSSDK.*`, `libifcore*` | Third-party dependencies of AVEVA features. |
| Anything not in the public set above | Unsupported. It may work today; it is not a contract. |

## Never commit these

AVEVA assemblies are licensed software. Reference them from the installation folder through `AVEVA_DESIGN_EXE` and keep `*.dll` in `.gitignore`.

```xml
<Reference Include="Aveva.Core.Database">
  <HintPath>$(AVEVA_DESIGN_EXE)Aveva.Core.Database.dll</HintPath>
  <Private>False</Private>
  <SpecificVersion>False</SpecificVersion>
</Reference>
```

`Private=False` (Copy Local = False) is what stops the build dropping copies into your output folder — and from there into a commit.

## Documentation on your machine

| What | Where |
| --- | --- |
| Per-assembly API reference (`.chm`) | `%AVEVA_DESIGN_EXE%\Documentation\` |
| Sample projects | `%AVEVA_DESIGN_EXE%\Samples.zip` |
| .NET Customisation User Guide | The E3D help suite |

Windows blocks `.chm` files copied from a network share — right-click → Properties → Unblock, or every page renders empty.
