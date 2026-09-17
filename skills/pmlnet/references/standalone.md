# Standalone Application

Your own executable, running a headless E3D core. No E3D window, no user, no PML.

Template: `assets/templates/Standalone/`

## Entry point

`Aveva.E3D.Standalone.Standalone` in `Aveva.E3D.Standalone.dll`. All members are static.

| Member | Purpose |
| --- | --- |
| `Start()` | Start the core. Overloads take a module number and/or an environment `Hashtable`. |
| `Open(project, user, password, mdb)` | Log in and open. Overload with `out PdmsMessage`. |
| `Close()` | **Obsolete in 3.1** — close MDB and project explicitly |
| `Quit()`, `Finish()`, `ExitError(msg)` | Harder shutdowns |
| `MDB`, `Project` | The open MDB and project |

## Shape

```csharp
bool opened = false;
try
{
    if (!Standalone.Start()) return 1;

    PdmsMessage error;
    opened = Standalone.Open(project, user, password, mdb, out error);
    if (!opened)
    {
        Console.Error.WriteLine("Login failed: " + error.MessageText());
        return 2;
    }

    DoWork();
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.ToString());
    return 3;
}
finally
{
    if (opened)
    {
        MDB.CurrentMDB.CloseMDB();          // MDB.Close() is also obsolete
        Project.CurrentProject.Close();
    }
}
```

Both `Start()` and `Open()` return `bool`. Check both.

## Project settings

`OutputType` is `Exe`. Everything else from `project-setup.md` still applies: .NET Framework 4.7.2, x86, no copy-local.

**Never reference `Aveva.ApplicationFramework*`.** There is no application framework in this process.

## The environment

The core needs E3D's environment variables, including a `PATH` that reaches the executable folder for the native libraries. Without them `Start()` returns `false` and explains nothing.

**Wrapper batch (preferred):**

```bat
@echo off
call "C:\AVEVA\Plant\Everything3D3.1\evars.bat" "C:\AVEVA\Plant\Everything3D3.1\"
"%~dp0MyTool.exe" %*
```

**Or pass a `Hashtable`:**

```csharp
Hashtable env = new Hashtable();
env["AVEVA_DESIGN_EXE"] = @"C:\AVEVA\Plant\Everything3D3.1\";
env["PMLLIB"]           = @"C:\AVEVA\Plant\Everything3D3.1\pmllib\";
env["projects_dir"]     = @"C:\AVEVA\Plant\Everything3D3.1\Projects\E3D3.1\";
Standalone.Start(env);
```

Read the target installation's `evars.bat` and match it. Do not invent variables.

## Modules

Database read/write permission follows the module. Start as a specific one when the default cannot write what you need:

```csharp
Standalone.Start(moduleNumber);
Standalone.Start(moduleNumber, env);
```

Reads working and writes silently refused usually means the wrong module.

## Writing safely

Same API as in-process — `Claim()`, `SetAttribute`, `SaveWork`, `Release()`. But there is no user to catch a mistake:

- Implement a **dry-run** mode that logs intended changes and writes nothing.
- Scope queries tightly.
- `SaveWork` with a comment identifying the job and date.
- Release claims — a crashed job holding claims blocks users.
- Test against a copy of the project.

## Credentials

Never hard-code a password. Read it from an environment variable set by the scheduler, or from a credential store. Keep it out of the command line, the source and the log.

## Rules

- Environment before `Start()`.
- `Close` in a `finally`.
- One core per process. No threads, no two projects.
- `CurrentElement` does not exist here — navigate from `MDB.CurrentMDB.GetFirstWorld(DbType.Design)` or by name.
- A standalone run consumes a licence like any other session.
- Return meaningful exit codes.
