using System.Reflection;
using System.Runtime.InteropServices;
using Aveva.Core.PMLNet;

[assembly: AssemblyTitle("MyCompany.E3D.Tools")]
[assembly: AssemblyDescription("PML .NET callable classes for AVEVA E3D")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("MyCompany")]
[assembly: AssemblyProduct("MyCompany.E3D.Tools")]
[assembly: AssemblyCopyright("Copyright (c) MyCompany")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("7a1f3c42-0c4b-4e35-9e2a-1b5d0c7a1001")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// REQUIRED. Without this, PML imports the assembly without error and then
// reports that your classes do not exist.
[assembly: PMLNetCallable()]
