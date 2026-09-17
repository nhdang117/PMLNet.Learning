# Troubleshooting

Symptom first. Work down the list — they are ordered by how often each cause is the real one.

## PML cannot find my class

```
!o = object MYCLASS()
(61,64)  Unknown object type
```

1. **`[assembly: PMLNetCallable()]` is missing** from `AssemblyInfo.cs`. This is the most common cause by a wide margin.
2. **The class is missing `[PMLNetCallable()]`**, or is not `public`.
3. **The `using namespace` is wrong.** Check the namespace exactly: `q var !o` on a working object prints it.
4. **The `import` silently failed.** Run it without the `handle` block and read the error.
5. **You imported an old copy.** A post-build `xcopy` that failed while E3D was running leaves the previous DLL in place.

## `import` fails

```
import |C:\AVEVA\Custom\MyCompany.Tools|
(61,2)  Failed to load assembly
```

1. **Path wrong**, or the `.dll` extension included (it must be omitted).
2. **Wrong platform.** An AnyCPU or x64 build cannot load into 32-bit E3D.
3. **Wrong framework.** Anything above .NET Framework 4.8, or .NET Core/5+, will not load.
4. **A dependency is missing.** Your DLL references something not in the executable folder or next to it.
5. **Already imported.** Harmless — this is what the `handle ANY` block is for.

## A method is missing from `.methods()`

1. **No `[PMLNetCallable()]` on that member.**
2. **It returns a type PML cannot marshal** — `int`, `DateTime`, `List<T>`, an enum. Only `double`, `string`, `bool`, `Hashtable` and `void`.
3. **It is `static`.** PML needs instance members.
4. **An overload collides.** Two signatures that look identical to PML resolve to one.

## The post-build copy fails

```
The process cannot access the file because it is being used by another process
```

E3D has the assembly loaded and will not release it until the process exits. Close E3D — not minimise — then rebuild.

There is no workaround inside a running session. The CLR cannot unload an assembly from a live AppDomain, and E3D does not shadow-copy addins.

## My addin does not load

1. **Not listed** in `<Module>Addins.xml`, or listed **with** the `.dll` extension (it must be omitted).
2. **E3D is reading AVEVA's XML, not yours.** Check which file the running session actually loads.
3. **Wrong platform or framework.** x86, .NET Framework 4.7.2.
4. **The class is not `public`**, or does not implement `IAddinInjected`.
5. **`Start` threw.** Watch the console window, or launch E3D from Visual Studio with the debugger attached.
6. **Two addin classes in one assembly.** Ambiguous; the host picks one.

## My toolbar button does nothing

1. **The command key in the `.uic` does not match `Command.Key`** exactly. Case and all.
2. **The `.uic` is not registered** in `<Module>Customization.xml`.
3. **Registered, but E3D is reading AVEVA's copy** of the customization XML.
4. **The command was never added** to `ICommandManager.Commands`.
5. **`Execute()` throws immediately** and the exception is swallowed. Put a breakpoint on the first line.

## `BadImageFormatException`

The platform target is wrong. E3D 3.1 is a 32-bit process; your assembly is AnyCPU or x64.

Set `<PlatformTarget>x86</PlatformTarget>` on **every** configuration, not just the one you build most.

## `TypeLoadException` or `FileNotFoundException` at runtime

1. **Copy Local was left on** for an AVEVA reference, so your output folder holds a second copy of an assembly already loaded. Set `Private=False` on all of them.
2. **You referenced an assembly that is not in the executable folder** at runtime.
3. **Version mismatch** — built against a different E3D version than the one running. Set `SpecificVersion=False`.

## `GetElement` returned something broken

`DbElement.GetElement` never returns null and never throws for a missing name. It returns an invalid element. Check `IsValid` immediately:

```csharp
DbElement e = DbElement.GetElement(name);
if (!e.IsValid) { /* handle here */ }
```

## `NullReferenceException` on a `DbAttribute`

`DbAttribute.GetDbAttribute(":MYUDA")` **does** return null when the attribute is not defined in this project. A project without your UDA is a normal condition:

```csharp
DbAttribute uda = DbAttribute.GetDbAttribute(":MYUDA");
if (uda == null) return;
```

## Writes are refused

1. **No claim.** Call `element.Claim()` first.
2. **Claimed by another user.** `PdmsException` says so; there is nothing to do but wait.
3. **The database is read-only** in this MDB, or the module has no write permission for that database type.
4. **In a standalone application, the wrong module.** Pass the module number to `Standalone.Start`.

## `SaveWork` returns false

It returns `bool`. If you never checked it, start there.

1. Claims were lost between the change and the save.
2. Another user saved a conflicting change.
3. The database is read-only or the extract is not writable.

## My pseudo UDA returns nothing

1. **The UDA is not marked pseudo** in Lexicon.
2. **`GetDbAttribute` returned null** — wrong name, or missing the leading `:`.
3. **The delegate type does not match the UDA's declared type.** Registers silently, never fires.
4. **Registered for a different element type** than the one you are testing.
5. **The addin did not start.**
6. **The handler threw.** Wrap it in `try`/`catch` and return a default.

## Everything is slow

1. **The collection is rooted too high.** Root at a zone, not the world.
2. **Expensive filters are first in the `AndFilter`.** Type tests are cheap; put them first.
3. **No `SkipFilter`.** Not descending beats filtering.
4. **Attribute reads inside inner loops.** Hoist them.
5. **A pseudo UDA doing I/O.** It runs on every read. Cache.
6. **`ExpressionFilter` in a hot path.** Use native filters where you can.

## E3D freezes during my code

1. **Long work on the UI thread.** There is no async story — show progress through an event, or split the work.
2. **A modal dialog from a background context.**
3. **A pseudo UDA showing UI.** Never do this.
4. **Recursion.** A pseudo UDA that reads its own attribute never returns.

## My user control is the wrong size

1. **Child controls are not docked or anchored.** Set `Dock = DockStyle.Fill` on the main child.
2. **The PML container is too small.** PML units are not pixels; adjust `width` and `height`.
3. **`anchor all` missing** on a resizeable form.
4. **`InitializeComponent()` was never called** in the constructor — the control is empty, not small.

## My PML form will not show

```
show !!myForm
(61,19)  Form not found
```

Run `pml rehash all`. PML indexes forms at startup; a new `.pmlfrm` is invisible until the index is rebuilt.

While editing an existing form, `pml reload form !!myForm` is faster. It reloads PML only — the DLL still needs an E3D restart.

## An event handler never fires

1. **The event name string does not match the C# event name.** There is no compile-time link — rename the event and PML keeps calling the old name forever, silently.
2. **`addeventhandler` was never called**, or was called on a different instance.
3. **The PML handler does not take exactly one `ARRAY`.**
4. **The event was raised while it had no subscribers** — check for null before invoking.

## Standalone `Start()` returns false

1. **The environment is not set.** Run through a wrapper batch that calls `evars.bat`, or pass a `Hashtable`.
2. **`PATH` does not include the executable folder**, so the native libraries are not found.
3. **No licence available.**
4. **Wrong platform or framework.** Still x86, still 4.7.2.

## Nothing above matches

Narrow it down in this order:

1. Can PML import the assembly at all? If not, it is a project settings problem, not a code problem.
2. Does `.methods()` show what you expect? If not, it is an attribute problem.
3. Does the same code work against a known-good element, like the CE? If so, it is a data problem.
4. Does it work in a fresh session? If so, something in the session is stale — re-import, restart.

And read the console window. Most of what E3D knows about a failing addin, it prints there.
