# PML .NET Templates

Five starter projects for AVEVA Everything3D 3.1, one per integration model. All five compile clean against a real E3D 3.1 installation with warnings treated as errors.

No AVEVA DLL is committed here. References resolve from your installation through `AVEVA_DESIGN_EXE`.

| Template | Output | Use it for |
| --- | --- | --- |
| `CallableClass/` | `MyCompany.E3D.Tools.dll` | A C# class PML creates and calls |
| `UserControl/` | `MyCompany.E3D.Controls.dll` | A WinForms control hosted on a PML form |
| `Addin/` | `MyCompany.E3D.Addin.dll` | Code loaded at E3D startup, with a command and docked window |
| `PseudoUda/` | `MyCompany.E3D.PseudoUdas.dll` | UDAs whose value is computed in C# |
| `Standalone/` | `MyCompany.E3D.Reports.exe` | A batch tool that runs outside E3D |

## Building

```bat
set AVEVA_DESIGN_EXE=C:\AVEVA\Plant\Everything3D3.1\
msbuild CallableClass\CallableClass.csproj /t:Rebuild /p:Configuration=Release
```

The trailing backslash matters — that is how E3D's own `evars.bat` defines the variable, and the project files concatenate paths onto it.

If `AVEVA_DESIGN_EXE` is not set, `AVEVA.props` falls back to `C:\AVEVA\Plant\Everything3D3.1\`.

## Shared settings

`AVEVA.props` is imported by every project and fixes the things that must not vary:

- `TargetFrameworkVersion` = `v4.7.2`
- `PlatformTarget` = `x86`
- `ReferencePath` = the E3D executable folder
- `DeployDir` = `C:\AVEVA\Custom\` — where the post-build step copies output

Override the deploy folder per build:

```bat
msbuild Addin\Addin.csproj /p:DeployDir=D:\MyDeploy\
```

The copy is skipped silently if the folder does not exist, and fails harmlessly if E3D has the DLL locked.

> **Both settings are mandatory.** E3D 3.1 hosts a **32-bit** CLR running **.NET Framework 4.7.2**. An AnyCPU or x64 build throws `BadImageFormatException`. .NET 5 or later will not load at all.

## Making it yours

1. Rename the folder and the `.csproj`.
2. Change `RootNamespace` and `AssemblyName` in the `.csproj`.
3. Generate a new `ProjectGuid` in the `.csproj` and a new `Guid` in `AssemblyInfo.cs`.
4. Replace `MyCompany` throughout — namespaces, addin names, and every command `Key`.
5. Keep `[assembly: PMLNetCallable()]` in `AssemblyInfo.cs` for anything PML will use, and drop it from anything PML will not.

Command keys must be unique across every addin loaded in the session. Namespace them with your company and assembly name.

## Deploying

Do not build into the E3D installation folder. Use a deployment folder:

```
C:\AVEVA\Custom\
    MyCompany.E3D.Addin.dll
    DesignAddins.xml          your copy, with your entry appended
    DesignCustomization.xml   your copy, registering your .uic
    MyTools.uic               produced from inside E3D
    pmllib\
        forms\
            csharpdatepicker.pmlfrm
```

`Addin/config/` holds annotated starting points for the two XML files. Copy the real ones out of `%AVEVA_DESIGN_EXE%`, keep every AVEVA entry, and append yours — never edit AVEVA's files in place.

## Testing each template

**CallableClass**

```
import |C:\AVEVA\Custom\MyCompany.E3D.Tools|
handle ANY
endhandle
using namespace |MyCompany.E3D.Tools|
!s = object NETSTRING(|abcde|)
q var !s.methods()
q var !s.length()
```

**UserControl**

Copy `UserControl/pml/csharpdatepicker.pmlfrm` onto `%PMLLIB%`, then:

```
pml rehash all
show !!cSharpDatePicker
```

**Addin**

Add your entry to `DesignAddins.xml`, restart E3D, and look for the **Element Info** docked window. Bind `MyCompany.E3D.Addin.ShowElementInfo` to a toolbar button to toggle it.

**PseudoUda**

Define `:VOLUME` (real) and `:SITECODE` (text) in Lexicon, **mark both as pseudo**, register the addin, restart, then:

```
q var !ce.:VOLUME
q var !ce.:SITECODE
```

**Standalone**

```bat
set E3D_PASSWORD=yourpassword
run.bat --project SAM --user SYSTEM --mdb SAMPLE --root /ZONE-01
```

## The development loop

Once E3D loads an assembly it locks the file for the life of the process. Close E3D — not minimise — before rebuilding, or the deploy copy fails with "the process cannot access the file".

## Full guide

<https://nhdang117.github.io/PMLNet.Learning/>
