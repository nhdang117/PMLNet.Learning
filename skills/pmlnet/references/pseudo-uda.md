# Pseudo UDA

A UDA whose value is computed by your C# on every read, rather than stored.

Template: `assets/templates/PseudoUda/`

## Prerequisite the code cannot supply

The UDA must exist in **Lexicon** and be **marked as pseudo**. Without that flag the handler is never called. Always state this to the user — a working DLL with an unflagged UDA looks like a code bug and is not one.

## Registration

```csharp
using Aveva.Core.Database;
using Ps   = Aveva.Core.Database.DbPseudoAttribute;
using NOUN = Aveva.Core.Database.DbElementTypeInstance;
using ATT  = Aveva.Core.Database.DbAttributeInstance;

DbAttribute uda = DbAttribute.GetDbAttribute(":VOLUME");
if (uda == null) return;                                   // not defined here

Ps.GetDoubleDelegate handler = new Ps.GetDoubleDelegate(Calculate);
Ps.AddGetDoubleAttribute(uda, NOUN.BOX, handler);
```

Three overloads per type:

```csharp
Ps.AddGetDoubleAttribute(uda, handler);                    // all valid types
Ps.AddGetDoubleAttribute(uda, NOUN.BOX, handler);          // type + derived
Ps.AddGetDoubleAttributeForOneType(uda, NOUN.BOX, handler);// exact type only
```

Setter registration always requires an element type:

```csharp
Ps.AddSetStringAttribute(uda, NOUN.EQUIPMENT, setterDelegate);
```

## Handler signature

Getters:

```csharp
T Handler(DbElement element, DbAttribute attribute, DbQualifier qualifier)
```

Setters:

```csharp
void Handler(DbElement element, DbAttribute attribute, T newValue)
```

## Delegate types

| UDA type | Getter | Setter |
| --- | --- | --- |
| Integer | `GetIntDelegate` | `SetIntDelegate` |
| Real | `GetDoubleDelegate` | `SetDoubleDelegate` |
| Logical | `GetBoolDelegate` | `SetBoolDelegate` |
| Text | `GetStringDelegate` | `SetStringDelegate`, `SetStringQualDelegate` |
| Reference | `GetRefDelegate` | `SetRefDelegate` |
| Position | `GetPositionDelegate` | `SetPositionDelegate` |
| Direction | `GetDirectionDelegate` | `SetDirectionDelegate` |
| Orientation | `GetOrientationDelegate` | `SetOrientationDelegate` |
| …Array | `Get*ArrayDelegate` | `Set*ArrayDelegate` |

The delegate type must match the UDA's declared type. A mismatch registers without complaint and never fires.

## Where to register

**In an addin's `Start`.** Registration is per-process and not persisted, so it must happen every session. Registering from a PMLNetCallable class makes the attribute's value depend on whether someone imported the assembly, which is worse than useless.

## The handler runs on every read

Not once per session — once per read, per element, per grid refresh, per report row. Therefore:

- **Never throw.** An exception crosses back into native code. Catch everything, return a default.
- **No I/O.** No files, no HTTP, no external databases. Cache if the value comes from outside.
- **No UI.** A dialog from a handler hangs E3D.
- **No database writes.** You are inside a read; writing can deadlock.
- **No recursion.** Reading the same pseudo UDA inside its own handler never returns.

```csharp
private static double Calculate(DbElement e, DbAttribute a, DbQualifier q)
{
    try
    {
        return e.GetDouble(ATT.XLEN) * e.GetDouble(ATT.YLEN) * e.GetDouble(ATT.ZLEN);
    }
    catch (Exception)
    {
        return 0.0;
    }
}
```

Caching pattern:

```csharp
private static readonly Dictionary<string, string> Cache =
    new Dictionary<string, string>();

string key = element.GetString(ATT.NAME);
string value;
if (Cache.TryGetValue(key, out value)) return value;
value = ExpensiveLookup(key);
Cache[key] = value;
return value;
```

## Testing

```
q var !ce.:VOLUME
```

Returning nothing, in diagnosis order: not flagged pseudo in Lexicon → `GetDbAttribute` returned null → delegate type mismatch → registered for a different element type → addin did not start → handler threw.

## Other notes

- Pseudo UDAs hold no data, so they do not appear in extracts or in anything reading the database files directly. Only code going through the E3D API sees the value.
- Register once. Registering twice for the same UDA and type is not guaranteed to replace the first handler.
- The `qualifier` argument is meaningful only for array UDAs.
