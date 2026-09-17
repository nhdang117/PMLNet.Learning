# Project Setup

Applies to every PML .NET project. Getting any of this wrong produces an assembly E3D will not load, usually with no error message.

## Mandatory settings

| Setting | Value |
| --- | --- |
| Output type | `Library` (`Exe` only for standalone) |
| `TargetFrameworkVersion` | `v4.7.2` |
| `PlatformTarget` | `x86`, on **every** configuration |
| AVEVA references | `Private=False`, `SpecificVersion=False` |
| Reference paths | `$(AVEVA_DESIGN_EXE)`, never hard-coded |

`des.exe.config` declares `<supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.7.2"/>`, and the `Aveva.Core.*` assemblies are marked 32-bit-required. Both constraints are absolute.

## Environment variables

| Variable | Value |
| --- | --- |
| `AVEVA_DESIGN_EXE` | `C:\AVEVA\Plant\Everything3D3.1\` — **trailing backslash** |
| `PDMSEXE` | legacy alias for the same folder |
| `PMLLIB` | `%AVEVA_DESIGN_EXE%pmllib\` |
| `PMLUI` | `%AVEVA_DESIGN_EXE%PMLUI\` |

Set by the installation's `evars.bat`.

## Project file skeleton

Prefer copying `assets/templates/*/`. If writing one by hand, the load-bearing parts are:

```xml
<PropertyGroup>
  <OutputType>Library</OutputType>
  <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  <PlatformTarget>x86</PlatformTarget>
  <LangVersion>7.3</LangVersion>
  <AvevaExe Condition=" '$(AVEVA_DESIGN_EXE)' != '' ">$(AVEVA_DESIGN_EXE)</AvevaExe>
  <AvevaExe Condition=" '$(AvevaExe)' == '' ">C:\AVEVA\Plant\Everything3D3.1\</AvevaExe>
  <ReferencePath>$(AvevaExe)</ReferencePath>
</PropertyGroup>

<ItemGroup>
  <Reference Include="Aveva.Core.Database">
    <HintPath>$(AvevaExe)Aveva.Core.Database.dll</HintPath>
    <Private>False</Private>
    <SpecificVersion>False</SpecificVersion>
  </Reference>
</ItemGroup>
```

`LangVersion` 7.3 is the highest .NET Framework 4.7.2 projects support by default. Do not use C# 8+ syntax.

## AssemblyInfo

Required for anything PML uses:

```csharp
using Aveva.Core.PMLNet;
[assembly: PMLNetCallable()]
```

Missing this is the single most common cause of "PML cannot find my class".

## References by project type

| Type | References |
| --- | --- |
| Callable class (compute only) | `PMLNet` |
| Callable class (model access) | `PMLNet`, `Aveva.Core.Database`, `Aveva.Core.Database.Filters`, `Aveva.Core.Geometry`, `Aveva.Core.Utilities` |
| User control | the above plus `System.Windows.Forms`, `System.Drawing` |
| Addin | `Aveva.ApplicationFramework`, `Aveva.ApplicationFramework.Presentation`, `Aveva.Core.Database`, `Aveva.Core.Presentation`, `System.Windows.Forms` |
| Pseudo UDA | `Aveva.ApplicationFramework`, `Aveva.Core.Database` |
| Standalone | `Aveva.E3D.Standalone`, `Aveva.Core.Database`, `Aveva.Core.Utilities` — **never** the CAF assemblies |

Never reference `*.Implementation.dll`, `*Internal.dll`, or any third-party DLL in the installation folder.

## Deployment

Build to a folder outside the installation:

```
C:\AVEVA\Custom\
    MyCompany.E3D.Addin.dll
    DesignAddins.xml          your copy
    DesignCustomization.xml   your copy
    MyTools.uic
```

Post-build:

```
xcopy /y /d "$(TargetPath)" "C:\AVEVA\Custom\"
```

This copy **fails while E3D is running** — the assembly is locked for the life of the process. Close E3D, rebuild, restart. There is no workaround.

## Debugging

```xml
<StartAction>Program</StartAction>
<StartProgram>$(AvevaExe)des.exe</StartProgram>
<StartArguments>-PROJ=SAM -USER=SYSTEM -PASS=XXXXXX -MDB/SAMPLE</StartArguments>
<StartWorkingDirectory>C:\AVEVA\Custom</StartWorkingDirectory>
```

Launch Visual Studio from a shell that has run `evars.bat`, so E3D inherits its environment.

## Build verification

```bat
set AVEVA_DESIGN_EXE=C:\AVEVA\Plant\Everything3D3.1\
msbuild MyProject.csproj /t:Rebuild /p:Configuration=Release /warnaserror
```

Use `/warnaserror`. Several APIs are obsolete in 3.1 (`Standalone.Close()`, `MDB.Close()`) and the warning names the replacement.
