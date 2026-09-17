# 5. How to Create a PMLNetCallable Class

## What it is

A PMLNetCallable class is an ordinary C# class that PML can create, hold in a variable, and call methods on — exactly as if it were a PML object.

```
PML                                   .NET
────────────────────────────────────  ─────────────────────────────
import |C:\AVEVA\Custom\MyLib|        loads MyLib.dll
using namespace |MyCompany.Tools|     selects the namespace
!s = object NETSTRING(|abc|)    ───►  new NetString("abc")
q var !s.length()               ───►  s.Length()  →  3
```

## Why it matters

This is the cheapest way in. No addin registration, no XML, no toolbar. You write a class, PML imports the DLL and uses it. Most PML .NET customisation in the wild is this plus a PML form.

## The four required pieces

A class is only usable from PML if all four are present:

1. `[assembly: PMLNetCallable()]` in `AssemblyInfo.cs` — see [Chapter 3](./project-setup).
2. `[PMLNetCallable()]` on the **class**.
3. `[PMLNetCallable()]` on a **public parameterless constructor**.
4. `[PMLNetCallable()]` on a **public `Assign` method** taking one argument of the class's own type.

Miss any of them and PML reports that the object does not exist, or fails on assignment, with no clue which one is missing.

## A complete example

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

        // (1) Required: parameterless constructor
        [PMLNetCallable()]
        public NetString()
        {
            mValue = string.Empty;
        }

        // Optional: overloaded constructor, also callable from PML
        [PMLNetCallable()]
        public NetString(string value)
        {
            mValue = value;
        }

        // (2) Required: Assign, taking the class's own type.
        // PML calls this for  !a = !b  so both variables do not share state.
        [PMLNetCallable()]
        public void Assign(NetString that)
        {
            this.mValue = that.mValue;
        }

        // A property becomes two PML methods: VAL() to get, VAL(STRING) to set.
        [PMLNetCallable()]
        public string Val
        {
            get { return mValue; }
            set { mValue = value; }
        }

        [PMLNetCallable()]
        public string Append(string value)
        {
            mValue += value;
            return mValue;
        }

        // Return double, not int - PML REAL maps to System.Double.
        [PMLNetCallable()]
        public double Length()
        {
            return mValue.Length;
        }

        // Returns a PML ARRAY. Keys are the PML indices, so start at 1.
        [PMLNetCallable()]
        public Hashtable Split(string separator)
        {
            Hashtable result = new Hashtable();
            string[] parts = mValue.Split(new[] { separator }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                result.Add((double)(i + 1), parts[i]);
            }
            return result;
        }

        // Raises a PML error that  handle(1000, 1)  can catch.
        [PMLNetCallable()]
        public double Real()
        {
            try
            {
                return Convert.ToDouble(mValue);
            }
            catch (FormatException)
            {
                throw new PMLNetException(1000, 1, "String cannot be converted to a number");
            }
        }

        // No attribute: invisible to PML, usable from C#.
        public void Reset()
        {
            mValue = string.Empty;
        }
    }
}
```

## Using it from PML

```
import |C:\AVEVA\Custom\MyCompany.Tools|
handle ANY
endhandle

using namespace |MyCompany.Tools|

!s = object NETSTRING(|abcde|)

q var !s
<MYCOMPANY.TOOLS.NETSTRING> MyCompany.Tools.NetString

q var !s.methods()
<ARRAY>
   [1]  <STRING> 'ADDEVENTHANDLER(STRING, ANY, STRING)'
   [2]  <STRING> 'APPEND(STRING)'
   [3]  <STRING> 'ASSIGN(MYCOMPANY.TOOLS.NETSTRING)'
   [4]  <STRING> 'HANDLE()'
   [5]  <STRING> 'LENGTH()'
   [6]  <STRING> 'NETSTRING()'
   [7]  <STRING> 'NETSTRING(STRING)'
   [8]  <STRING> 'REAL()'
   [9]  <STRING> 'REMOVEEVENTHANDLER(STRING, REAL)'
  [10]  <STRING> 'SPLIT(STRING)'
  [11]  <STRING> 'STRING()'
  [12]  <STRING> 'VAL()'
  [13]  <STRING> 'VAL(STRING)'
  [14]  <STRING> 'VALID()'

q var !s.length()
<REAL> 5

q var !s.append(|fgh|)
<STRING> 'abcdefgh'

!s.val(|finished|)
q var !s.val()
<STRING> 'finished'
```

Things worth noticing in that output:

- `Reset()` is absent — no attribute, no exposure.
- `Val` appears twice, once as a getter and once as a setter.
- `ADDEVENTHANDLER`, `HANDLE`, `STRING`, `VALID` and `REMOVEEVENTHANDLER` are added by the PML .NET engine for free.
- Both constructors are listed, one per signature.

`.methods()` is the fastest way to check what actually got exposed. If a method is missing from that list, the attribute is missing in the source.

## What can cross the boundary

Only four PML types have .NET equivalents:

| PML | .NET |
| --- | --- |
| `REAL` | `System.Double` |
| `STRING` | `System.String` |
| `BOOLEAN` | `System.Boolean` |
| `ARRAY` | `System.Collections.Hashtable` |

**Rule.** Anything else must be converted on the .NET side before it is returned.

Practical consequences:

- **Return `double`, never `int`.** An `int` return type produces a method PML cannot bind.
- **`Hashtable` keys are `double`, and PML arrays start at 1.** Adding key `0` produces an entry PML can see but not index naturally.
- **`DateTime`, enums, `DbElement` and your own classes do not cross.** Convert to string, or expose another PMLNetCallable class.
- **A `Hashtable` you return is copied into a PML array.** Mutating it afterwards on the .NET side changes nothing in PML.

::: tip Passing an element to PML
There is no direct `DbElement` marshalling. Pass the element name (`GetString(DbAttributeInstance.NAME)`) or its database reference as a string, and let PML re-acquire it. On the way back in, take a `string` and call `DbElement.GetElement(name)`.
:::

## Errors: `PMLNetException`

Throwing any other exception type surfaces in PML as an unhelpful generic failure. `PMLNetException` carries a module number and an error number, which PML can catch selectively:

```csharp
throw new PMLNetException(1000, 1, "String cannot be converted to a number");
```

```
!input = |abc|
!s = object NETSTRING(!input)
!r = !s.real()
handle (1000, 1)
   $p Not a number: $!!error.text
endhandle
```

Pick a module number for your own code and keep it consistent — it is how PML tells your errors apart from AVEVA's.

## Events back into PML

`PMLNetDelegate.PMLNetEventHandler` is the only event signature PML can subscribe to. It takes one `ArrayList`.

```csharp
[PMLNetCallable()]
public class Watcher
{
    // The event itself is not marked - PMLNetEventHandler is already known to PML.
    public event PMLNetDelegate.PMLNetEventHandler OnProgress;

    [PMLNetCallable()]
    public Watcher() { }

    [PMLNetCallable()]
    public void Assign(Watcher that) { }

    [PMLNetCallable()]
    public void Run()
    {
        for (int i = 1; i <= 10; i++)
        {
            if (OnProgress != null)
            {
                ArrayList args = new ArrayList();
                args.Add((double)i);
                args.Add("step " + i);
                OnProgress(args);
            }
        }
    }
}
```

From PML, the handler is a form method taking one `ARRAY`:

```
!this.watcher = object WATCHER()
!this.watcher.addeventhandler(|OnProgress|, !this, |progressed|)

define method .progressed(!data is ARRAY)
   $p Step $!data[1] : $!data[2]
endmethod
```

The `ArrayList` arrives as a PML array indexed from 1, whatever you put in it. Only the four marshallable types survive the trip.

## Rules and gotchas

**Rule — the class must be public.** The AVEVA training material shows `class NetString` without a modifier. That worked in older versions; make it `public` and avoid the question.

**Rule — the assembly is locked once imported.** After `import`, the DLL cannot be overwritten until that E3D process exits. Your post-build copy will fail. Close E3D first.

**Rule — `import` twice is an error.** Always wrap it:

```
import |C:\AVEVA\Custom\MyCompany.Tools|
handle ANY
endhandle
```

**Rule — the PML type name is the class name, uppercased.** `NetString` becomes `NETSTRING`. Two classes whose names differ only by case collide. The fully qualified PML type is `<NAMESPACE.CLASSNAME>`, which is why `using namespace` matters when two assemblies define the same class name.

**Gotcha — `Assign` is not optional even when there is no state.** PML calls it on every `!a = !b`. An empty body is fine:

```csharp
[PMLNetCallable()]
public void Assign(MyClass that) { }
```

**Gotcha — overloads must differ in a way PML can see.** PML matches on argument count and on the four marshallable types. `Foo(double)` and `Foo(int)` look identical from PML and one of them wins unpredictably. Overload by arity, or by genuinely different PML types.

**Gotcha — static methods are not exposed.** PML needs an instance. Make it an instance method.

**Gotcha — a long-running method freezes E3D.** There is no async story here. If it takes more than a second, report progress through an event so the user can see something is happening.

## Testing without a form

The command window is the fastest test harness you have:

```
import |C:\AVEVA\Custom\MyCompany.Tools|
handle ANY
endhandle
using namespace |MyCompany.Tools|
!t = object NETSTRING(|hello|)
q var !t.methods()
q var !t.length()
```

Get the object right there before you build any UI around it.

## Checklist

- [ ] `[assembly: PMLNetCallable()]` is present in `AssemblyInfo.cs`.
- [ ] The class is `public` and carries `[PMLNetCallable()]`.
- [ ] There is a parameterless constructor with the attribute.
- [ ] There is an `Assign(YourType that)` with the attribute.
- [ ] Every exposed method returns `double`, `string`, `bool`, `Hashtable` or `void`.
- [ ] `Hashtable` keys are `double` and start at 1.
- [ ] Failures throw `PMLNetException` with your own module number.
- [ ] `.methods()` in PML lists everything you expected, and nothing you did not.

## Next

[Chapter 6](./database-interface) moves from computation to the model: reading and writing elements and attributes.
