# 3. How to Set Up a PML .NET Project

This chapter produces a project that E3D will load. Most "my addin does nothing and there is no error" problems are one of the settings on this page.

## The four settings that decide everything

| Setting | Value | Why |
| --- | --- | --- |
| Output type | **Class Library** | E3D loads DLLs. Only standalone applications are executables. |
| Target framework | **.NET Framework 4.7.2** | `des.exe.config` declares `<supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.7.2"/>`. .NET 5+ and .NET Core will not load at all. |
| Platform target | **x86** | The E3D executables and `Aveva.Core.*` assemblies are marked 32-bit-required. An AnyCPU or x64 build throws `BadImageFormatException` when the CLR tries to bind it. |
| Copy Local on AVEVA references | **False** | Your DLL must bind to the assemblies already loaded in the process. Shipping your own copies causes version conflicts and duplicate type identities. |

::: danger Platform target is not optional
"AnyCPU" looks harmless and is the Visual Studio default. In a 32-bit host it usually still works — until the day it silently does not. Set x86 explicitly on every configuration, including Debug, Release and any custom ones.
:::

## Environment variables E3D sets

`evars.bat` in the installation folder sets these. They are available to Visual Studio if you launch it from an E3D-configured shell, and useful in your project file either way:

| Variable | Typical value | Use |
| --- | --- | --- |
| `AVEVA_DESIGN_EXE` | `C:\AVEVA\Plant\Everything3D3.1\` | The executable folder. Note the **trailing backslash**. |
| `PDMSEXE` | Same as above | Legacy alias, still set by `set_aveva_design.bat` |
| `PMLLIB` | `%AVEVA_DESIGN_EXE%pmllib\` | Where PML macros and forms are searched for |
| `PMLUI` | `%AVEVA_DESIGN_EXE%PMLUI\` | PML user-interface files |
| `AVEVA_DESIGN_USER` | `...\USERDATA\` | Per-user data |

If you are building on a machine without E3D installed, set `AVEVA_DESIGN_EXE` yourself to a folder holding a copy of the reference assemblies.

## A project file that works

This is a classic (non-SDK) `.csproj`, which is what the AVEVA samples use and what the WinForms designer is happiest with. References resolve through `AVEVA_DESIGN_EXE`, so nothing is hard-coded to one machine and no AVEVA DLL ever enters source control.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"
          Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />

  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">x86</Platform>
    <ProjectGuid>{PUT-A-NEW-GUID-HERE}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>MyCompany.E3D.Training</RootNamespace>
    <AssemblyName>MyCompany.E3D.Training</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <PlatformTarget>x86</PlatformTarget>
    <LangVersion>7.3</LangVersion>
    <!-- Fall back to the default install if the variable is not set -->
    <AvevaExe Condition=" '$(AVEVA_DESIGN_EXE)' != '' ">$(AVEVA_DESIGN_EXE)</AvevaExe>
    <AvevaExe Condition=" '$(AvevaExe)' == '' ">C:\AVEVA\Plant\Everything3D3.1\</AvevaExe>
    <ReferencePath>$(AvevaExe)</ReferencePath>
  </PropertyGroup>

  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|x86' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
  </PropertyGroup>

  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|x86' ">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
  </PropertyGroup>

  <ItemGroup>
    <!-- AVEVA assemblies: never copied locally -->
    <Reference Include="PMLNet">
      <HintPath>$(AvevaExe)PMLNet.dll</HintPath>
      <Private>False</Private>
      <SpecificVersion>False</SpecificVersion>
    </Reference>
    <Reference Include="Aveva.Core.Database">
      <HintPath>$(AvevaExe)Aveva.Core.Database.dll</HintPath>
      <Private>False</Private>
      <SpecificVersion>False</SpecificVersion>
    </Reference>
    <Reference Include="Aveva.Core.Database.Filters">
      <HintPath>$(AvevaExe)Aveva.Core.Database.Filters.dll</HintPath>
      <Private>False</Private>
      <SpecificVersion>False</SpecificVersion>
    </Reference>
    <Reference Include="Aveva.Core.Geometry">
      <HintPath>$(AvevaExe)Aveva.Core.Geometry.dll</HintPath>
      <Private>False</Private>
      <SpecificVersion>False</SpecificVersion>
    </Reference>
    <Reference Include="Aveva.Core.Utilities">
      <HintPath>$(AvevaExe)Aveva.Core.Utilities.dll</HintPath>
      <Private>False</Private>
      <SpecificVersion>False</SpecificVersion>
    </Reference>

    <!-- Framework assemblies -->
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <Reference Include="System.Drawing" />
    <Reference Include="System.Windows.Forms" />
    <Reference Include="System.Xml" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="Properties\AssemblyInfo.cs" />
    <!-- your sources here -->
  </ItemGroup>

  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

Add `Aveva.ApplicationFramework` and `Aveva.ApplicationFramework.Presentation` when you write an addin, and `Aveva.Core.Presentation` when you need `CurrentElement`. [Chapter 4](./api-and-dlls) has the full list.

::: tip SDK-style projects
A modern `<Project Sdk="Microsoft.NET.Sdk">` file targeting `net472` with `<PlatformTarget>x86</PlatformTarget>` also works, and is shorter. The trade-off is that WinForms designer support for SDK-style .NET Framework projects is less reliable in older Visual Studio versions. If you are hand-writing your controls, use SDK-style. If you want the designer, stay classic.
:::

## AssemblyInfo: the line people forget

For `[PMLNetCallable]` to be honoured on your types, the **assembly itself** must carry the attribute:

```csharp
using System.Reflection;
using System.Runtime.InteropServices;
using Aveva.Core.PMLNet;

[assembly: AssemblyTitle("MyCompany.E3D.Training")]
[assembly: AssemblyProduct("MyCompany.E3D.Training")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: ComVisible(false)]
[assembly: Guid("PUT-A-NEW-GUID-HERE")]

// Required for PML to discover [PMLNetCallable] types in this assembly.
[assembly: PMLNetCallable()]
```

Without the assembly-level attribute, PML imports the DLL without complaint and then reports that your class does not exist. It is the single most common first-day mistake.

An addin that exposes nothing to PML does not need this line — but adding it costs nothing and saves the puzzle later.

## Where to put the built DLL

Do **not** build straight into the E3D installation folder. It is shared, it may be read-only, and mixing your output with AVEVA's makes upgrades painful.

Use a dedicated deployment folder:

```
C:\AVEVA\Custom\
    MyCompany.E3D.Training.dll
    DesignAddins.xml          (your copy, if you write addins)
    DesignCustomization.xml   (your copy, if you add toolbars)
    MyTools.uic
    pmllib\
        forms\
            mytool.pmlfrm
```

Then add a post-build step:

```
xcopy /y /d "$(TargetPath)" "C:\AVEVA\Custom\"
xcopy /y /d "$(TargetDir)$(TargetName).pdb" "C:\AVEVA\Custom\"
```

::: warning The post-build copy fails while E3D is running
Once E3D has imported or loaded your assembly, the file is locked for the lifetime of that process. The copy fails with "the process cannot access the file". Close E3D, rebuild, restart. There is no way around this — the CLR does not unload assemblies from a running AppDomain. See [Troubleshooting](../reference/troubleshooting) for the details.
:::

## Making E3D find your code

How you register depends on which of the four models you chose:

| Model | Registration |
| --- | --- |
| PMLNetCallable class or user control | `import` the DLL by full path from PML — no registration file needed |
| Addin | Add a `<string>` entry to `<Module>Addins.xml` |
| Toolbar or menu item | Add a `<CustomizationFile>` entry to `<Module>Customization.xml` pointing at your `.uic` |
| Standalone | Nothing — but the process needs E3D's environment variables |

For addins, copy the module's `DesignAddins.xml` out of the installation folder into your own folder first, add your entry, and point E3D at your copy. Editing AVEVA's file in place means a service pack silently reverts your customisation.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ArrayOfString xmlns:xsd="http://www.w3.org/2001/XMLSchema"
               xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <!-- ... the AVEVA entries, unchanged ... -->
  <string>C:\AVEVA\Custom\MyCompany.E3D.Training</string>
</ArrayOfString>
```

Entries are assembly names **without** the `.dll` extension. A bare name is resolved against the executable folder; a full path is used as-is.

## Debugging inside E3D

Attaching to a running process works, but starting E3D from Visual Studio is better because you catch exceptions thrown during addin startup.

In **Project Properties → Debug**, set:

- **Start external program**: `C:\AVEVA\Plant\Everything3D3.1\des.exe`
- **Command line arguments**: `-PROJ=SAM -USER=SYSTEM -PASS=XXXXXX -MDB/SAMPLE`
- **Working directory**: `C:\AVEVA\Custom`

Or put it directly in the `.csproj`, per configuration, so it survives a fresh clone:

```xml
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|x86' ">
  <StartAction>Program</StartAction>
  <StartProgram>$(AvevaExe)des.exe</StartProgram>
  <StartArguments>-PROJ=SAM -USER=SYSTEM -PASS=XXXXXX -MDB/SAMPLE</StartArguments>
  <StartWorkingDirectory>C:\AVEVA\Custom</StartWorkingDirectory>
</PropertyGroup>
```

E3D needs its environment variables set. The reliable way is to launch Visual Studio from a shell that has already run E3D's `evars.bat`, so the whole environment is inherited.

::: tip Console output
`Console.WriteLine` from addin code appears in the E3D console window when one is attached. The AVEVA samples use it heavily for tracing. For anything user-facing, use the messaging API in `Aveva.Core.Utilities.Messaging` instead.
:::

## The development loop

The loop is slower than PML. Plan for it:

1. Edit C#.
2. Close E3D. (Not minimise — close.)
3. Build.
4. Start E3D.
5. Test.

Two things shorten it:

- **Keep logic out of the E3D-hosted assembly** where you can. Put the parts that do not touch the AVEVA API in a separate library with unit tests, and iterate there.
- **Drive from PML while prototyping.** A PMLNetCallable class can be re-imported after an E3D restart in seconds, and you can call it from the command line without building any UI.

## Checklist

- [ ] Output type is Class Library (except standalone).
- [ ] Target framework is .NET Framework 4.7.2.
- [ ] Platform target is x86 on **every** configuration.
- [ ] Every AVEVA reference has `Private=False` (Copy Local = False).
- [ ] References resolve through `AVEVA_DESIGN_EXE`, not a hard-coded path.
- [ ] `[assembly: PMLNetCallable()]` is in `AssemblyInfo.cs` if PML will use the assembly.
- [ ] No AVEVA DLL is committed to source control.
- [ ] Post-build copies to a deployment folder outside the installation.
- [ ] Debug profile starts `des.exe` with project arguments.

## Next

[Chapter 4](./api-and-dlls) maps the API: which assembly holds which types, what changed between PDMS and E3D, and which references each kind of project needs.
