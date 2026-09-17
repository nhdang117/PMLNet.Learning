# 11. How to Create a Standalone Interface

## What it is

A standalone application is your own `.exe` — console, WinForms, WPF, a Windows service — that starts a headless E3D core, logs into a project, opens an MDB, and then uses exactly the same `Aveva.Core.Database` API as everything else in this guide.

No E3D window. No user logged in. No PML.

```
your-tool.exe
   │
   ├─ Standalone.Start()                 starts the headless core
   ├─ Standalone.Open(proj,user,pass,mdb) logs in and opens the MDB
   │
   ├─ MDB.CurrentMDB ...                  the normal API from here on
   ├─ DbElement.GetElement("/E1301")
   │
   └─ CloseMDB() + Project.Close()        log out cleanly
```

## Why it matters

Everything in the earlier chapters needs a person sitting in front of E3D. This does not. Nightly reports, scheduled extracts, data migrations, a web service answering questions about the model, a validation job in a build pipeline — all of them need the model without the application.

## The entry point

`Aveva.E3D.Standalone.Standalone`, in `Aveva.E3D.Standalone.dll`. All members are static.

| Member | Purpose |
| --- | --- |
| `Start()` | Start the headless core. Overloads take a module number, an environment `Hashtable`, or both. |
| `Open(project, user, password, mdb)` | Log in and open an MDB. An overload returns a `PdmsMessage`. |
| `Close()` | Close the MDB and log out. **Obsolete in 3.1** — close the MDB and project explicitly instead. |
| `Quit()` | Shut the core down |
| `Finish()` | Terminate |
| `ExitError(message)` | Report and exit |
| `MDB` | The open MDB |
| `Project` | The open project |

## A minimal console application

```csharp
using System;
using Aveva.Core.Database;
using Aveva.Core.Utilities.Messaging;
using Aveva.E3D.Standalone;

namespace MyCompany.E3D.Reports
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (!Standalone.Start())
                {
                    Console.Error.WriteLine("Failed to start the E3D core.");
                    return 1;
                }

                PdmsMessage error;
                if (!Standalone.Open("SAM", "SYSTEM", "XXXXXX", "SAMPLE", out error))
                {
                    Console.Error.WriteLine("Login failed: " + error.MessageText());
                    return 2;
                }

                Report();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 3;
            }
            finally
            {
                MDB.CurrentMDB.CloseMDB();
                Project.CurrentProject.Close();
            }
        }

        private static void Report()
        {
            MDB mdb = MDB.CurrentMDB;
            Console.WriteLine("MDB: " + mdb.Name);

            foreach (Db db in mdb.GetDBArray(DbType.Design))
            {
                Console.WriteLine("{0}\t#{1}\t{2}\t{3}",
                    db.Name,
                    db.Number,
                    db.Type,
                    db.LatestSessionOnDb.User);
            }
        }
    }
}
```

The project is an **executable**, not a library — but everything else from [Chapter 3](./project-setup) still applies: .NET Framework 4.7.2, x86, no copy-local on AVEVA references.

## The environment problem

The headless core needs E3D's environment variables — `AVEVA_DESIGN_EXE`, `PMLLIB`, the projects directory, and the rest. Without them `Start()` returns `false` and tells you nothing useful.

Two ways to solve it.

### Inherit the environment

Run your executable from a shell that has already run E3D's `evars.bat`. The simplest form is a wrapper batch file next to the installation's own:

```bat
@echo off
call "C:\AVEVA\Plant\Everything3D3.1\evars.bat" "C:\AVEVA\Plant\Everything3D3.1\"
"C:\AVEVA\Custom\MyCompany.E3D.Reports.exe" %*
```

Reliable, and it keeps the environment definition in one place. This is what scheduled tasks should call.

### Pass it in

`Start` accepts a `Hashtable` of variables, which removes the batch file:

```csharp
Hashtable environment = new Hashtable();
environment["AVEVA_DESIGN_EXE"] = @"C:\AVEVA\Plant\Everything3D3.1\";
environment["PMLLIB"]           = @"C:\AVEVA\Plant\Everything3D3.1\pmllib\";
environment["PMLUI"]            = @"C:\AVEVA\Plant\Everything3D3.1\PMLUI\";
environment["projects_dir"]     = @"C:\AVEVA\Plant\Everything3D3.1\Projects\E3D3.1\";
environment["SAM000"]           = @"C:\AVEVA\Projects\SAM\";

if (!Standalone.Start(environment))
{
    // ...
}
```

Cleaner to deploy, but you are now maintaining a copy of what `evars.bat` says. Read `evars.bat` on the target installation and match it — do not guess.

::: warning The native DLLs must be findable
The E3D core is not pure .NET. Its native libraries are found through `PATH`, which `evars.bat` extends with the installation folder. If `Start()` fails with a `DllNotFoundException` or returns `false` with a clean environment otherwise, `PATH` is what is missing.
:::

## Modules and what you may write

E3D's modules have different database permissions. Design can write 3D data and read catalogue data, but cannot read Draft. Those definitions are set in Admin under **Project → Module Definitions**.

A standalone application inherits the permissions of the module it starts as:

```csharp
// Start as a specific module number
Standalone.Start(moduleNumber);
Standalone.Start(moduleNumber, environment);
```

If reads succeed and writes are silently refused, you are almost certainly in the wrong module.

## Writing from a standalone application

The API is identical to the in-process case, including claims and save-work:

```csharp
DbElement equi = DbElement.GetElement("/E1301");
if (!equi.IsValid)
{
    Console.Error.WriteLine("/E1301 not found");
    return;
}

try
{
    equi.Claim();
    equi.SetAttribute(DbAttributeInstance.DESC, "Updated by batch job");

    if (!MDB.CurrentMDB.SaveWork("Nightly description update"))
    {
        Console.Error.WriteLine("SaveWork failed");
        return;
    }

    equi.Release();
}
catch (PdmsException ex)
{
    Console.Error.WriteLine("Refused: " + ex.Message);
}
```

::: danger A batch job writing to a live project
There is no user to notice a mistake and no undo dialog. Before you run anything that writes:

- **Dry-run first.** Log every intended change, change nothing, and read the log.
- **Scope the query tightly.** A filter that is slightly too broad rewrites the wrong site at 2 a.m.
- **Save with a comment that identifies the job.** `SaveWork("nightly-sync 2026-09-17")` is how someone reconstructs what happened.
- **Release your claims.** A crashed job holding claims blocks users the next morning.
- **Test against a copy of the project.** Every time.
:::

## Shutting down

`Standalone.Close()` still exists but is marked obsolete in E3D 3.1. The compiler tells you so:

```
warning CS0618: 'Standalone.Close()' is obsolete:
'please use MDB.CurrentMDB.Close() and Project.CurrentProject.Close()'
```

Close the two explicitly instead. Note that `MDB.Close()` is *also* obsolete in favour of the `bool`-returning `CloseMDB()`:

```csharp
MDB.CurrentMDB.CloseMDB();       // not MDB.Close()
Project.CurrentProject.Close();
```

Always in a `finally`, so an exception does not leave the project locked:

```csharp
bool opened = false;
try
{
    if (!Standalone.Start()) return 1;

    PdmsMessage error;
    opened = Standalone.Open(project, user, password, mdb, out error);
    if (!opened)
    {
        Console.Error.WriteLine(error.MessageText());
        return 2;
    }

    DoWork();
    return 0;
}
finally
{
    if (opened)
    {
        MDB.CurrentMDB.CloseMDB();
        Project.CurrentProject.Close();
    }
}
```

`Quit()` and `Finish()` shut the core down harder, and `ExitError(message)` reports and terminates in one call. For a console tool, closing the MDB and project plus a normal exit is what you want — returning an exit code lets the scheduler know what happened.

## Credentials

**Do not hard-code the password.** Options, in increasing order of respectability:

1. Command-line arguments — fine for interactive use, visible in the process list.
2. Environment variables set by the scheduled task — decent.
3. Windows Credential Manager or a secret store — right for anything unattended.

Whichever you choose, keep it out of source control and out of the log file.

## A useful shape for real jobs

```csharp
internal static int Main(string[] args)
{
    Options options = Options.Parse(args);        // project, mdb, dry-run, output
    using (Log log = new Log(options.LogPath))
    {
        bool opened = false;
        try
        {
            if (!Standalone.Start(BuildEnvironment(options)))
            {
                log.Error("Core failed to start");
                return 1;
            }

            PdmsMessage error;
            opened = Standalone.Open(options.Project, options.User,
                                     options.Password, options.Mdb, out error);
            if (!opened)
            {
                log.Error("Login failed: " + error.MessageText());
                return 2;
            }

            int changed = Run(options, log);
            log.Info(options.DryRun
                ? changed + " elements would change"
                : changed + " elements changed");
            return 0;
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            return 3;
        }
        finally
        {
            if (opened)
            {
                MDB.CurrentMDB.CloseMDB();
                Project.CurrentProject.Close();
            }
        }
    }
}
```

Exit codes, a log file, and a dry-run switch. Every unattended job that survives contact with production has all three.

## Rules and gotchas

**Rule — the environment must be set before `Start()`.** Setting variables after the core starts changes nothing.

**Rule — still x86, still .NET Framework 4.7.2.** The core is the same 32-bit code.

**Rule — do not reference the CAF assemblies.** There is no application framework in this process.

**Gotcha — `Start()` returns `false` rather than explaining.** When it does, check `PATH`, then `AVEVA_DESIGN_EXE`, then whether another E3D process on the same machine holds a licence.

**Gotcha — licensing.** A standalone application consumes a licence like any other session. A scheduled job that runs while the team is working takes a seat from someone.

**Gotcha — one core per process.** Do not try to open two projects at once, and do not parallelise across threads. Run several processes if you need concurrency.

**Gotcha — the current element does not exist.** `CurrentElement` is a presentation concept. In a standalone application, navigate explicitly from `MDB.CurrentMDB.GetFirstWorld(DbType.Design)` or by name.

## Checklist

- [ ] Output type is Console or Windows Application, targeting .NET Framework 4.7.2, x86.
- [ ] The environment is set before `Start()` — batch wrapper or `Hashtable`.
- [ ] `Start()` and `Open()` return values are both checked.
- [ ] The MDB and project are closed in a `finally` (`CloseMDB()`, not the obsolete `Close()`).
- [ ] Credentials do not appear in source or logs.
- [ ] Writing jobs have a dry-run mode and were tested against a copy.
- [ ] Claims are released; `SaveWork` comments identify the job.
- [ ] The process returns a meaningful exit code.
- [ ] No reference to `Aveva.ApplicationFramework*`.

## Where to go next

You have covered all four integration models. Worth having open from here on:

- [API Cheatsheet](../reference/api-cheatsheet) — the calls you will look up repeatedly
- [DLL Map](../reference/dll-map) — which assembly holds what
- [Troubleshooting](../reference/troubleshooting) — symptom-first diagnosis
- [Claude Skill](/skill) — the same rules, packaged so an AI assistant applies them
