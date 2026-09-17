# 6. Database Interface

## What it is

`Aveva.Core.Database` is the .NET view of the AVEVA data model. Every element, attribute, hierarchy walk, creation, deletion and save goes through it.

The mental model maps onto PML one-for-one:

| PML | .NET |
| --- | --- |
| `!ce` | `CurrentElement.Element` |
| `/SITE-01` | `DbElement.GetElement("/SITE-01")` |
| `NAME OF OWNER` | `element.Owner.GetString(DbAttributeInstance.NAME)` |
| `MEMBERS` | `element.Members()` |
| `NEW EQUI` | `owner.Create(0, DbElementTypeInstance.EQUIPMENT)` |
| `SAVEWORK` | `MDB.CurrentMDB.SaveWork("comment")` |

## Getting an element

```csharp
using Aveva.Core.Database;

// By name
DbElement equi = DbElement.GetElement("/E1301");

// By database reference
DbElement byRef = DbElement.GetElement(new int[] { 7205, 1462 });

// The current element
DbElement ce = CurrentElement.Element;

// From an attribute that holds a reference
DbElement head = branch.GetElement(DbAttributeInstance.HREF);
```

::: warning Always check `IsValid`
`GetElement` for a name that does not exist does **not** throw and does **not** return null. It returns an invalid `DbElement`. Calling `GetString` on it throws later, somewhere less convenient.

```csharp
DbElement e = DbElement.GetElement("/NOPE");
if (!e.IsValid)
{
    // handle it here, not three methods down the stack
    return;
}
```
:::

`DbElement` also exposes `IsNull`, `IsDeleted` and `IsDeleteable` for the less common cases.

## The current element

```csharp
using Aveva.Core.Database;

// Read
DbElement ce = CurrentElement.Element;

// Write - this moves the user's CE, and everything watching it reacts
CurrentElement.Element = DbElement.GetElement("/E1301");

// React to the user navigating
CurrentElement.CurrentElementChanged += OnCeChanged;

private void OnCeChanged(object sender, CurrentElementChangedEventArgs e)
{
    string name = e.Element.GetAsString(DbAttributeInstance.FLNM);
    Console.WriteLine("CE is now " + name);
}
```

**Rule.** Never cache the current element across an operation that might move it. Read `CurrentElement.Element` when you need it.

## Element types

```csharp
DbElementType type = element.ElementType;          // property
bool isEqui = element.ElementType == DbElementTypeInstance.EQUIPMENT;

// By name, when the type is not known at compile time
DbElementType nozz = DbElementType.GetElementType("NOZZ");

string typeName = type.Name;            // "EQUIPMENT"
string shortName = type.ShortName;      // "EQUI"
```

`DbElementTypeInstance` has a static field for every element type in the data model — `EQUIPMENT`, `NOZZLE`, `BRANCH`, `SITE`, `ZONE`, `BOX`, `CYLINDER`, and hundreds more. Prefer it over `GetElementType("...")`: it is compile-time checked and faster.

Useful members on `DbElementType`:

| Member | Returns |
| --- | --- |
| `SystemAttributes()` | Every standard attribute valid for this type |
| `GetAllUdas()` | Every UDA valid for this type |
| `MemberTypes()` | The types allowed as members |
| `OwnerTypes()` | The types allowed as owner |
| `IsAttributeValid(att)` | Whether an attribute applies to this type |

## Navigating the hierarchy

```csharp
DbElement owner  = element.Owner;
DbElement first  = element.FirstMember();
DbElement next   = element.Next();
DbElement prev   = element.Previous;              // property, not a method
DbElement third  = element.Member(3);

DbElement[] all      = element.Members();
DbElement[] nozzles  = element.Members(DbElementTypeInstance.NOZZLE);

// First/next of a given type
DbElement n1 = equi.FirstMember(DbElementTypeInstance.NOZZLE);
DbElement n2 = n1.Next(DbElementTypeInstance.NOZZLE);

// Ancestry test
bool inside = site.IsDescendant(zone);
```

`Members()` on a large owner materialises the whole array. For anything wider than one level, use a collection with a filter instead — [Chapter 7](./collections-and-filters).

## Reading attributes

An attribute is identified by a `DbAttribute`, which is a *definition*, not a value. Get them from `DbAttributeInstance`:

```csharp
using Aveva.Core.Database;
using Aveva.Core.Geometry;

string name  = element.GetString(DbAttributeInstance.NAME);
string desc  = element.GetString(DbAttributeInstance.DESC);
double xlen  = element.GetDouble(DbAttributeInstance.XLEN);
int    rev   = element.GetInteger(DbAttributeInstance.REV);
bool   built = element.GetBool(DbAttributeInstance.BUIL);

Position    pos = element.GetPosition(DbAttributeInstance.POS);
Direction   dir = element.GetDirection(DbAttributeInstance.HDIR);
Orientation ori = element.GetOrientation(DbAttributeInstance.ORI);

DbElement spref = element.GetElement(DbAttributeInstance.SPRE);

// Arrays
string[] words   = element.GetAsStringArray(DbAttributeInstance.ALIST);
double[] numbers = element.GetDoubleArray(DbAttributeInstance.PARA);
```

When you do not care about the type, or want it formatted the way E3D would display it:

```csharp
string any = element.GetAsString(DbAttributeInstance.POS);
```

`GetAsString` is the safe fallback in generic code — an attribute browser, a report, an export. It never throws over a type mismatch.

Attributes not known at compile time, including UDAs:

```csharp
DbAttribute uda = DbAttribute.GetDbAttribute(":MYUDA");
if (uda != null)
{
    string value = element.GetString(uda);
}
```

**Rule.** UDA names start with `:`. `GetDbAttribute` returns `null` for an attribute that is not defined in this project — always null-check before use, because a project without your UDA is a normal condition, not an error.

### Qualifiers

Some attributes need an index or a type to make sense — array members, `POS WRT`, per-type defaults:

```csharp
// Third element of an array attribute
double third = element.GetDouble(DbAttributeInstance.PARA, 3);

// Position with respect to another element
DbQualifier wrt = new DbQualifier();
wrt.wrtQualifier = DbElement.GetElement("/SITE-01");
Position relative = element.GetPosition(DbAttributeInstance.POS, wrt);
```

`DbQualifier` accumulates qualifiers with `Add()` overloads for each type. The `int` overloads on the getters are the shorthand for the common array-index case.

## Writing attributes

```csharp
element.SetAttribute(DbAttributeInstance.DESC, "Pump suction");
element.SetAttribute(DbAttributeInstance.BUIL, true);
element.SetAttribute(DbAttributeInstance.REV, 3);
element.SetAttribute(DbAttributeInstance.XLEN, 1500.0);
element.SetAttribute(DbAttributeInstance.POS, new Position(1000, 2000, 3000));
element.SetAttribute(DbAttributeInstance.NAME, "/E1301-A");
```

There is an overload for every marshallable type, plus `out PdmsMessage` variants that report the failure instead of throwing:

```csharp
PdmsMessage error;
if (!element.SetAttribute(DbAttributeInstance.DESC, value, out error))
{
    Console.WriteLine(error.MessageText());
}
```

### Claims come first

A write to an element in a multi-write database requires a claim:

```csharp
using Aveva.Core.Utilities.Messaging;

try
{
    element.Claim();
    element.SetAttribute(DbAttributeInstance.DESC, "Updated");
}
catch (PdmsException ex)
{
    // Claimed by another user, database is read-only, element is extract-locked...
    Console.WriteLine(ex.Message);
}
```

| Call | Effect |
| --- | --- |
| `element.Claim()` | Claim this element |
| `element.ClaimHierarchy()` | Claim it and everything below |
| `element.Release()` | Release the claim (after `SaveWork`) |
| `element.ReleaseHierarchy()` | Release the whole branch |
| `MDB.CurrentMDB.Claim(elements)` | Claim a batch in one call |

Attributes `LCLM` and `LCLMH` tell you the current claim state.

## Creating and deleting

```csharp
DbElement zone = DbElement.GetElement("/ZONE-01");

// Create as the first member (position 0 = first)
DbElement equi = zone.Create(0, DbElementTypeInstance.EQUIPMENT);
equi.SetAttribute(DbAttributeInstance.NAME, "/E1301");

// Relative creation
DbElement after  = equi.CreateAfter(DbElementTypeInstance.EQUIPMENT);
DbElement before = equi.CreateBefore(DbElementTypeInstance.EQUIPMENT);
DbElement last   = zone.CreateLast(DbElementTypeInstance.EQUIPMENT);
DbElement firstM = zone.CreateFirst(DbElementTypeInstance.EQUIPMENT);

// Copy
target.Copy(source);                       // attributes only
DbCopyOption options = new DbCopyOption
{
    FromName = "E1301",
    ToName   = "E1302",
    Rename   = true
};
target.CopyHierarchy(source, options);     // attributes and members

// Move
element.InsertAfterLast(newOwner);

// Delete
element.Delete();

PdmsMessage error;
if (!element.Delete(out error))
{
    Console.WriteLine(error.MessageText());
}
```

**Rule — name immediately after creating.** A newly created element is unnamed. Anything that looks it up by name before you set `NAME` will not find it.

**Rule — creation is ordered.** The first argument to `Create` is the position in the owner's member list. `0` means first. Use `CreateLast` when order does not matter; it avoids re-indexing.

## Saving and discarding

```csharp
MDB.CurrentMDB.SaveWork("Created equipment for area 13");   // commit
MDB.CurrentMDB.GetWork();                                    // refresh from disk
MDB.CurrentMDB.QuitWork();                                   // discard since last save
```

`SaveWork` returns `bool`. Check it. A failed save with an ignored return value is how a day of work disappears.

Save a subset when you need to:

```csharp
MDB.CurrentMDB.SaveWork(elements, "Partial save");
MDB.CurrentMDB.SaveWork(databases, "Partial save");
```

## Databases and the MDB

```csharp
MDB mdb = MDB.CurrentMDB;

string mdbName = mdb.Name;
Db[] designDbs = mdb.GetDBArray(DbType.Design);
Db[] everything = mdb.GetDBArray();

DbElement world = mdb.GetFirstWorld(DbType.Design);

// Find by name across a database type
DbElement found = mdb.FindElement(DbType.Design, "/E1301");
DbElement[] all = mdb.FindElements(DbType.Design, "/E1301");

foreach (Db db in designDbs)
{
    Console.WriteLine(db.Name + " #" + db.Number + " " + db.Type);
}
```

`DbType` covers `Design`, `Catalogue`, `Draft`, `Isodraft`, `Dictionary`, `Engineering`, `Schematic`, `System`, `Global` and the rest.

Project-level information:

```csharp
Project project = Project.CurrentProject;
string code = project.Code;       // "SAM"
string user = project.UserName;
bool open   = project.IsOpen();
```

## Undo

Database changes made from .NET should be undoable by the user, like any other change:

```csharp
using Aveva.Core.Utilities.Undo;

UndoTransaction trans = UndoTransaction.GetUndoTransaction();
trans.StartTransaction("Rename equipment");
try
{
    element.SetAttribute(DbAttributeInstance.NAME, "/E1301-NEW");
}
finally
{
    trans.EndTransaction();
}
```

Everything between `StartTransaction` and `EndTransaction` becomes one entry on the undo stack. `UndoTransaction.PerformUndo()` and `PerformRedo()` drive it programmatically.

To put your own in-memory state on the same stack, derive from `UndoSubscriber` and register it with `UndoCaretaker.RegisterUndoSubscriber`. The four overrides (`GetBeginState`, `GetEndState`, `RestoreBeginState`, `RestoreEndState`) capture and restore whatever you decide your state is.

## Expressions and rules

PML1 expressions are available from .NET, which is often the shortest route to something the API has no direct call for:

```csharp
DbExpression expr = DbExpression.Parse("HEIGHT OF PREV * 2");
double value = element.EvaluateDouble(expr, DbAttributeUnit.DIST);

DbExpression nameExpr = DbExpression.Parse("( SUBSTR( NAMN OF SITE, 1, 3 ))");
string prefix = element.EvaluateString(nameExpr);
```

Design rules:

```csharp
DbExpression expr = DbExpression.Parse("HEIGHT * 2.0");
DbRule rule = DbRule.CreateDbRule(expr, DbRuleStatus.DYNAMIC, DbExpressionType.REAL);

DbAttribute diam = DbAttribute.GetDbAttribute("DIAM");
element.SetRule(diam, rule);

bool exists   = element.ExistRule(diam);
bool outOfDate = element.VerifyRule(diam);
element.ExecuteRule(diam);
element.ExecuteAllRules();          // everything below this element
element.DeleteRule(diam);
```

## Rules and gotchas

**Rule — `GetElement` never returns null.** It returns an invalid element. Check `IsValid`.

**Rule — `DbAttribute.GetDbAttribute` *does* return null** for an undefined attribute. Check for null.

**Rule — the API is single-threaded.** UI thread only.

**Gotcha — units.** `GetDouble` on a distance attribute returns millimetres, the database's internal unit, regardless of what the user's display units are set to. Convert at the boundary, and use `DbAttributeUnit` when evaluating expressions.

**Gotcha — `NAME` versus `NAMN` versus `FLNM`.** `NAME` is the full name with leading `/`. `NAMN` is the name without it. `FLNM` is the full name, falling back to the reference for unnamed elements — which is why UI code should display `FLNM`.

**Gotcha — an element with no name.** `GetString(NAME)` on an unnamed element returns an empty string, not the reference. Use `GetAsString(FLNM)` for anything a user reads.

**Gotcha — setting `NAME` to a name already in use** throws. Test with `MDB.CurrentMDB.FindElement` first if the name comes from user input.

## Checklist

- [ ] Every `GetElement` result is checked with `IsValid`.
- [ ] Every `GetDbAttribute` result is checked for null.
- [ ] Writes are preceded by `Claim()` and wrapped in a `try`/`catch (PdmsException)`.
- [ ] New elements get their `NAME` set immediately.
- [ ] Multi-step changes are wrapped in an `UndoTransaction`.
- [ ] `SaveWork` return values are checked.
- [ ] Nothing touches the API off the UI thread.

## Next

[Chapter 7](./collections-and-filters) replaces hand-written recursion with the collection and filter API.
