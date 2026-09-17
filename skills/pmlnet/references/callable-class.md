# PMLNetCallable Class

A C# class PML creates and calls as if it were a native PML object.

Template: `assets/templates/CallableClass/`

## The four mandatory pieces

Miss any one and PML reports the class does not exist, or fails on assignment:

1. `[assembly: PMLNetCallable()]` in `AssemblyInfo.cs`
2. `[PMLNetCallable()]` on the **public** class
3. `[PMLNetCallable()]` on a **parameterless** constructor
4. `[PMLNetCallable()]` on `Assign(OwnType that)`

## Shape

```csharp
using System;
using System.Collections;
using Aveva.Core.PMLNet;

namespace MyCompany.Tools
{
    [PMLNetCallable()]
    public class NetString
    {
        private string mValue;

        [PMLNetCallable()]
        public NetString() { mValue = string.Empty; }

        [PMLNetCallable()]
        public NetString(string value) { mValue = value ?? string.Empty; }

        [PMLNetCallable()]
        public void Assign(NetString that) { this.mValue = that.mValue; }

        // Property -> VAL() and VAL(STRING) in PML
        [PMLNetCallable()]
        public string Val
        {
            get { return mValue; }
            set { mValue = value ?? string.Empty; }
        }

        [PMLNetCallable()]
        public double Length() { return mValue.Length; }   // double, never int

        [PMLNetCallable()]
        public Hashtable Split(string separator)
        {
            Hashtable result = new Hashtable();
            string[] parts = mValue.Split(new[] { separator }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
                result.Add((double)(i + 1), parts[i]);     // PML indices start at 1
            return result;
        }

        [PMLNetCallable()]
        public double Real()
        {
            double parsed;
            if (!double.TryParse(mValue, out parsed))
                throw new PMLNetException(1000, 1, "not a number");
            return parsed;
        }

        public void Reset() { mValue = string.Empty; }     // no attribute = hidden from PML
    }
}
```

## Type marshalling

| PML | .NET |
| --- | --- |
| `REAL` | `double` |
| `STRING` | `string` |
| `BOOLEAN` | `bool` |
| `ARRAY` | `Hashtable`, keys `double` from 1 |

Nothing else crosses. Convert on the .NET side:

- `int`, `float`, `decimal` → return `double`
- `DateTime`, enums → return a formatted `string`
- `List<T>`, arrays → copy into a `Hashtable`
- `DbElement` → pass the element name as `string`, re-acquire with `DbElement.GetElement`
- your own class → make it PMLNetCallable

## Errors

```csharp
throw new PMLNetException(moduleNumber, errorNumber, "message");
```

PML catches it with `handle(1000, 1)`. Pick one module number for your codebase. Any other exception type surfaces as an unhelpful generic failure.

Translate database exceptions at the boundary:

```csharp
catch (PdmsException ex)
{
    throw new PMLNetException(1000, 2, ex.Message);
}
```

## Events

`PMLNetDelegate.PMLNetEventHandler` is the only signature PML understands. The event itself needs **no** attribute.

```csharp
public event PMLNetDelegate.PMLNetEventHandler OnProgress;

ArrayList args = new ArrayList();
args.Add((double)step);
args.Add(message);
if (OnProgress != null) OnProgress(args);
```

PML side:

```
!obj.addeventhandler(|OnProgress|, !this, |progressed|)

define method .progressed(!data is ARRAY)
   $p $!data[1] $!data[2]
endmethod
```

The event name is matched as a **string** — renaming the C# event silently breaks every PML caller.

## Usage from PML

```
import |C:\AVEVA\Custom\MyCompany.Tools|
handle ANY
endhandle
using namespace |MyCompany.Tools|
!s = object NETSTRING(|abcde|)
q var !s.methods()
```

`.methods()` lists exactly what got exposed. Use it to verify. `ADDEVENTHANDLER`, `HANDLE`, `STRING`, `VALID` and `REMOVEEVENTHANDLER` are added by the engine.

## Rules

- The class must be `public`.
- `Assign` is required even with no state — an empty body is fine.
- Static methods are not exposed. Instance only.
- Overloads must differ in arity or in one of the four PML types. `Foo(double)` and `Foo(int)` collide.
- The PML type name is the class name uppercased; the qualified name is `<NAMESPACE.CLASSNAME>`.
- `import` twice is an error — always wrap in `handle ANY` / `endhandle`.
- The DLL is locked once imported. E3D must restart before it can be replaced.
- A long-running method freezes E3D. Report progress via an event.
