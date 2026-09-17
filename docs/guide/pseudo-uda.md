# 10. How to Create a Pseudo UDA

## What it is

A normal UDA stores a value. A **pseudo UDA** stores nothing — when anything asks for its value, E3D calls your C# and uses what you return.

```
Anything that reads an attribute
   PML:      q var !ce.:VOLUME
   Explorer: the attribute grid
   Report:   a Draft report column
   .NET:     element.GetDouble(uda)
        │
        ▼
   E3D sees the UDA is pseudo
        │
        ▼
   Your delegate:  double VolumeCalculation(DbElement ele,
                                            DbAttribute att,
                                            DbQualifier qualifier)
        │
        ▼
   The value, computed fresh
```

The value is always current, is never saved, and takes up no space in the database.

## Why it matters

The alternative is a stored UDA plus something that keeps it up to date — a macro someone remembers to run, a batch job, a save-work hook. Every one of those drifts. A pseudo UDA cannot drift, because there is nothing to drift from.

Typical uses: derived quantities (volume, weight, surface area), values pulled from an external system, codes assembled from other attributes, status flags computed from a rule.

## What you need first

The UDA must exist and be **declared as pseudo** in the Lexicon module. That is a data-model change, not a code change:

1. In Lexicon, create the UDA (`:VOLUME`).
2. Set its type (real, text, logical, reference, and array variants).
3. Set the element types it applies to.
4. Mark it as **pseudo**.

Without the pseudo flag, your handler is never called and the UDA behaves as ordinary storage.

## Registering a handler

Three steps, in `Aveva.Core.Database`:

```csharp
using Aveva.Core.Database;
using Ps = Aveva.Core.Database.DbPseudoAttribute;
using NOUN = Aveva.Core.Database.DbElementTypeInstance;
using ATT = Aveva.Core.Database.DbAttributeInstance;

public static class VolumeUda
{
    public static void Register()
    {
        // 1. Find the UDA. Returns null if the project does not define it.
        DbAttribute uda = DbAttribute.GetDbAttribute(":VOLUME");
        if (uda == null) return;

        // 2. Wrap the method in the matching delegate type.
        Ps.GetDoubleDelegate handler = new Ps.GetDoubleDelegate(Calculate);

        // 3. Register it, optionally for one element type only.
        Ps.AddGetDoubleAttribute(uda, NOUN.BOX, handler);
    }

    private static double Calculate(DbElement element,
                                    DbAttribute attribute,
                                    DbQualifier qualifier)
    {
        double x = element.GetDouble(ATT.XLEN);
        double y = element.GetDouble(ATT.YLEN);
        double z = element.GetDouble(ATT.ZLEN);
        return x * y * z;
    }
}
```

Every getter delegate has the same signature: `(DbElement, DbAttribute, DbQualifier)` returning the value's type.

## Where to register

**In an addin's `Start` method.** The handler must be in place before anything reads the attribute, which in practice means before the user can do anything.

```csharp
public class PseudoUdaAddin : IAddinInjected
{
    public string Name { get { return "PseudoUdaAddin"; } }
    public string Description { get { return "Registers calculated UDAs"; } }

    public void Start(IDependencyResolver resolver)
    {
        try
        {
            VolumeUda.Register();
            SupplierCodeUda.Register();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Pseudo UDA registration failed: " + ex.Message);
        }
    }

    public void Start(ServiceManager serviceManager) { }
    public void Stop() { }
}
```

A PMLNetCallable class can register handlers too, but then the behaviour depends on someone having imported the assembly — which makes the attribute's value depend on session history. Use an addin.

## The full delegate set

`DbPseudoAttribute` has a pair of delegates and a family of registration methods for each supported type.

| Value type | Getter delegate | Registration |
| --- | --- | --- |
| `int` | `GetIntDelegate` | `AddGetIntAttribute` |
| `double` | `GetDoubleDelegate` | `AddGetDoubleAttribute` |
| `bool` | `GetBoolDelegate` | `AddGetBoolAttribute` |
| `string` | `GetStringDelegate` | `AddGetStringAttribute` |
| `DbElement` | `GetRefDelegate` | `AddGetRefAttribute` |
| `Position` | `GetPositionDelegate` | `AddGetPositionAttribute` |
| `Direction` | `GetDirectionDelegate` | `AddGetDirectionAttribute` |
| `Orientation` | `GetOrientationDelegate` | `AddGetOrientationAttribute` |
| `int[]` | `GetIntArrayDelegate` | `AddGetIntArrayAttribute` |
| `double[]` | `GetDoubleArrayDelegate` | `AddGetDoubleArrayAttribute` |
| `string[]` | `GetStringArrayDelegate` | `AddGetStringArrayAttribute` |
| `DbElement[]` | `GetRefArrayDelegate` | `AddGetRefArrayAttribute` |

Each registration method has three overloads:

```csharp
// Every element type the UDA is valid for
Ps.AddGetDoubleAttribute(uda, handler);

// This element type and its derived types
Ps.AddGetDoubleAttribute(uda, NOUN.BOX, handler);

// This element type exactly, nothing derived
Ps.AddGetDoubleAttributeForOneType(uda, NOUN.BOX, handler);
```

Register different handlers for different element types when the calculation differs — a volume for a `BOX` is not a volume for a `CYLINDER`.

## Writable pseudo UDAs

Setters exist for most types, so a pseudo UDA can accept a value and do something with it:

```csharp
Ps.SetStringDelegate setter = new Ps.SetStringDelegate(ApplyCode);
Ps.AddSetStringAttribute(uda, NOUN.EQUIPMENT, setter);

private static void ApplyCode(DbElement element,
                              DbAttribute attribute,
                              string newValue)
{
    // Store it somewhere real - another attribute, an external system
    element.SetAttribute(ATT.DESC, "Code: " + newValue);
}
```

Setter delegates take the new value instead of a qualifier; the string and string-array variants also have `...QualDelegate` forms that receive a `DbQualifier` as well.

Register a setter only when writing genuinely means something. A UDA that accepts a value and silently discards it is worse than a read-only one.

## A second example: text from another element

```csharp
private static string SiteCode(DbElement element,
                               DbAttribute attribute,
                               DbQualifier qualifier)
{
    TypeFilter siteFilter = new TypeFilter(DbElementTypeInstance.SITE);
    DbElement site = siteFilter.Parent(element);

    if (!site.IsValid) return string.Empty;

    string name = site.GetString(DbAttributeInstance.NAMN);
    if (string.IsNullOrEmpty(name)) return string.Empty;

    return name.Length <= 3 ? name.ToUpper()
                            : name.Substring(0, 3).ToUpper();
}
```

Register with `Ps.AddGetStringAttribute(uda, handler)` and every element with the UDA reports its site prefix, always current, even after the element is moved.

## Performance: the part that bites

**Your handler runs on every read.** Not once per session — every read. Open the attribute grid on a selection of two hundred elements and it runs two hundred times. Run a report over a site and it runs for every row.

Consequences:

**Rule — no I/O in a handler.** No file reads, no HTTP, no database queries against another system. If the value comes from outside, cache it and refresh on a schedule or an explicit user action.

```csharp
private static readonly Dictionary<string, string> Cache =
    new Dictionary<string, string>();

private static string SupplierCode(DbElement element,
                                   DbAttribute attribute,
                                   DbQualifier qualifier)
{
    string key = element.GetString(DbAttributeInstance.NAME);

    string value;
    if (Cache.TryGetValue(key, out value)) return value;

    value = LookUpSlowly(key);       // called once per element per session
    Cache[key] = value;
    return value;
}
```

**Rule — no `MessageBox`, no dialogs, no UI.** A handler that shows a dialog while the explorer is painting hangs E3D.

**Rule — never throw.** An exception escaping a handler crosses back into native code. Catch everything and return a sensible default:

```csharp
private static double Calculate(DbElement element,
                                DbAttribute attribute,
                                DbQualifier qualifier)
{
    try
    {
        return element.GetDouble(ATT.XLEN)
             * element.GetDouble(ATT.YLEN)
             * element.GetDouble(ATT.ZLEN);
    }
    catch (Exception)
    {
        return 0.0;
    }
}
```

**Rule — do not write to the database from a getter.** You are inside a read. Writing re-enters the database layer and can deadlock or corrupt the read in progress.

**Rule — do not read the same pseudo UDA from inside its own handler.** Infinite recursion, and the stack overflow takes E3D with it.

## Testing

```
q var !ce.:VOLUME
```

From C#:

```csharp
DbAttribute uda = DbAttribute.GetDbAttribute(":VOLUME");
double v = element.GetDouble(uda);
```

If you get zero, an empty string, or the stored value instead of yours, work through this list:

1. Is the UDA declared pseudo in Lexicon?
2. Is it valid for this element type?
3. Did `GetDbAttribute` return null — is the name right, with the leading `:`?
4. Did the addin actually start?
5. Does the delegate type match the UDA's declared type? A `GetDoubleDelegate` on a text UDA registers without complaint and never fires.
6. Did you register for a different element type than the one you are testing?

## Rules and gotchas

**Gotcha — registration is per-process and not persisted.** Every E3D session must register again. That is what the addin is for.

**Gotcha — registering twice.** Calling `AddGet...` twice for the same UDA and type is not guaranteed to replace the first handler. Register once, at startup.

**Gotcha — the qualifier argument is usually ignored**, and should be handled for array UDAs. For a scalar UDA it arrives with nothing meaningful in it.

**Gotcha — pseudo UDAs do not appear in extracts or Global propagation** as data, because there is no data. Downstream systems reading the database file directly will not see them. Only code going through the E3D API sees the computed value.

**Gotcha — the Lexicon definition is project data.** Your DLL works, the project has not been updated, and nothing happens. Ship the UDA definition alongside the assembly.

## Checklist

- [ ] The UDA exists in Lexicon and is marked pseudo.
- [ ] The UDA is valid for the element types you register against.
- [ ] `GetDbAttribute` is null-checked.
- [ ] The delegate type matches the UDA's declared type.
- [ ] Registration happens once, in an addin's `Start`.
- [ ] The handler does no I/O, no UI and no database writes.
- [ ] The handler cannot throw.
- [ ] Anything expensive is cached.
- [ ] `q var !ce.:YOURUDA` returns the computed value in a fresh session.

## Next

[Chapter 11](./standalone-interface) leaves E3D behind entirely and reads the same databases from your own executable.
