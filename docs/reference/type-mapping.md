# Type Mapping

## PML ↔ .NET

Only four PML types cross the boundary. Everything else must be converted on the .NET side.

| PML | .NET | Notes |
| --- | --- | --- |
| `REAL` | `System.Double` | Not `int`, not `float`, not `decimal` |
| `STRING` | `System.String` | |
| `BOOLEAN` | `System.Boolean` | |
| `ARRAY` | `System.Collections.Hashtable` | Keys are `double`, PML indices start at 1 |

### Returning an array

```csharp
[PMLNetCallable()]
public Hashtable GetNames()
{
    Hashtable result = new Hashtable();
    double index = 1;                       // PML arrays are 1-based
    foreach (string name in names)
    {
        result.Add(index++, name);
    }
    return result;
}
```

### Receiving an array

```csharp
[PMLNetCallable()]
public double SumValues(Hashtable values)
{
    double total = 0;
    foreach (DictionaryEntry entry in values)
    {
        // entry.Key is double, entry.Value is one of the four types
        total += Convert.ToDouble(entry.Value);
    }
    return total;
}
```

### Events

An event's `ArrayList` follows the same rules — each element must be one of the four types, and PML sees it as a 1-based array.

```csharp
ArrayList args = new ArrayList();
args.Add((double)index);     // !data[1]
args.Add(text);              // !data[2]
args.Add(flag);              // !data[3]
OnSomething(args);
```

### Things that do not cross

| .NET type | Do this instead |
| --- | --- |
| `int`, `long`, `float`, `decimal` | Return `double` |
| `DateTime` | Return a formatted `string`, or a `double` of ticks/epoch |
| `enum` | Return the name as `string` or the value as `double` |
| `List<T>`, `T[]`, `Dictionary<,>` | Copy into a `Hashtable` |
| `DbElement` | Return the element name as `string`, re-acquire in PML |
| Your own class | Make it PMLNetCallable and return it |
| `null` | Return an empty string, `0`, or throw `PMLNetException` |

## Database types ↔ .NET

What each attribute kind gives you from `DbElement`:

| Attribute type | Getter | Setter | .NET type |
| --- | --- | --- | --- |
| Text | `GetString` | `SetAttribute(att, string)` | `string` |
| Word | `GetString` | `SetAttribute(att, string)` | `string` |
| Real | `GetDouble` | `SetAttribute(att, double)` | `double` |
| Integer | `GetInteger` | `SetAttribute(att, int)` | `int` |
| Logical | `GetBool` | `SetAttribute(att, bool)` | `bool` |
| Reference | `GetElement` | `SetAttribute(att, DbElement)` | `DbElement` |
| Position | `GetPosition` | `SetAttribute(att, Position)` | `Position` |
| Direction | `GetDirection` | `SetAttribute(att, Direction)` | `Direction` |
| Orientation | `GetOrientation` | `SetAttribute(att, Orientation)` | `Orientation` |
| Date | `GetDateTime` | `SetAttribute(att, DateTime)` | `DateTime` |
| Text array | `GetAsStringArray` | `SetAttribute(att, string[])` | `string[]` |
| Real array | `GetDoubleArray` | `SetAttribute(att, double[])` | `double[]` |
| Integer array | `GetIntegerArray` | `SetAttribute(att, int[])` | `int[]` |
| Logical array | `GetBoolArray` | `SetAttribute(att, bool[])` | `bool[]` |
| Reference array | `GetElementArray` | `SetAttribute(att, DbElement[])` | `DbElement[]` |
| Anything | `GetAsString` | — | `string`, formatted |

`GetAsString` is the safe generic accessor. It formats the way E3D would display the value and does not throw over a type mismatch, which makes it right for attribute browsers, reports and exports.

## Units

`GetDouble` on a distance attribute returns **millimetres** — the database's internal unit — regardless of the user's display units.

| Concern | What to do |
| --- | --- |
| Displaying a length | Format through the presentation layer, or convert explicitly |
| Evaluating an expression | Pass the right `DbAttributeUnit`: `NONE`, `DIST`, `BORE`, `PTYP` |
| Bores | `Aveva.Core.Utilities.Units.Bore` understands bore conventions |
| Angles | Degrees |

```csharp
double mm = element.GetDouble(DbAttributeInstance.XLEN);      // millimetres
double d  = element.EvaluateDouble(expr, DbAttributeUnit.DIST);
```

## Pseudo UDA delegate signatures

Getters all look the same:

```csharp
T Handler(DbElement element, DbAttribute attribute, DbQualifier qualifier)
```

Setters take the new value instead:

```csharp
void Handler(DbElement element, DbAttribute attribute, T newValue)
```

| UDA type | Getter delegate | Setter delegate |
| --- | --- | --- |
| Integer | `GetIntDelegate` | `SetIntDelegate` |
| Real | `GetDoubleDelegate` | `SetDoubleDelegate` |
| Logical | `GetBoolDelegate` | `SetBoolDelegate` |
| Text | `GetStringDelegate` | `SetStringDelegate`, `SetStringQualDelegate` |
| Reference | `GetRefDelegate` | `SetRefDelegate` |
| Position | `GetPositionDelegate` | `SetPositionDelegate` |
| Direction | `GetDirectionDelegate` | `SetDirectionDelegate` |
| Orientation | `GetOrientationDelegate` | `SetOrientationDelegate` |
| Integer array | `GetIntArrayDelegate` | `SetIntArrayDelegate` |
| Real array | `GetDoubleArrayDelegate` | `SetDoubleArrayDelegate` |
| Text array | `GetStringArrayDelegate` | `SetStringArrayDelegate`, `SetStringArrayQualDelegate` |
| Reference array | `GetRefArrayDelegate` | `SetRefArrayDelegate` |

The delegate type must match the UDA's declared type in Lexicon. A mismatch registers without error and never fires.

## Exceptions

| Layer | Exception | Where it comes from |
| --- | --- | --- |
| PML boundary | `PMLNetException(module, number, text)` | You throw it; PML catches with `handle(module, number)` |
| Database | `PdmsException` | The API throws it when an operation is refused |
| Structured error | `PdmsMessage` | Returned by `out` parameters on the non-throwing overloads |

```csharp
try
{
    element.Claim();
}
catch (PdmsException ex)
{
    throw new PMLNetException(1000, 2, "Cannot claim: " + ex.Message);
}
```

Translate at the boundary. PML cannot usefully handle a `PdmsException`; it can handle your `PMLNetException`.
