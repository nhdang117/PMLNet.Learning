# Database and Collections

`Aveva.Core.Database` and `Aveva.Core.Database.Filters`. Full signatures in `api-cheatsheet.md`.

## The two null rules

These cause most runtime failures in PML .NET code:

```csharp
// GetElement NEVER returns null - it returns an INVALID element
DbElement e = DbElement.GetElement("/E1301");
if (!e.IsValid) { /* handle here */ }

// GetDbAttribute DOES return null when the project lacks the attribute
DbAttribute uda = DbAttribute.GetDbAttribute(":MYUDA");
if (uda == null) { /* normal condition, not an error */ }
```

## Reading

```csharp
string s = e.GetString(ATT.NAME);
double d = e.GetDouble(ATT.XLEN);        // millimetres, always
int    i = e.GetInteger(ATT.REV);
bool   b = e.GetBool(ATT.BUIL);

Position    p = e.GetPosition(ATT.POS);
Direction   v = e.GetDirection(ATT.HDIR);
Orientation o = e.GetOrientation(ATT.ORI);
DbElement   r = e.GetElement(ATT.SPRE);

string any = e.GetAsString(ATT.POS);     // formatted, never throws on type
```

`GetAsString` is the right choice in generic code — attribute browsers, reports, exports.

Name attributes: `NAME` (full, leading `/`, empty if unnamed), `NAMN` (no slash), **`FLNM`** (full name falling back to the reference — use this in UI).

## Writing

```csharp
try
{
    e.Claim();
    e.SetAttribute(ATT.DESC, "text");
}
catch (PdmsException ex) { /* claimed elsewhere, read-only, extract-locked */ }
```

Non-throwing overloads report through `out PdmsMessage`, read with `error.MessageText()`.

Wrap multi-step changes so the user can undo them:

```csharp
UndoTransaction t = UndoTransaction.GetUndoTransaction();
t.StartTransaction("Rename equipment");
try { /* changes */ } finally { t.EndTransaction(); }
```

Commit with `MDB.CurrentMDB.SaveWork("comment")` — and **check the `bool` it returns**.

## Creating

```csharp
DbElement child = owner.Create(0, NOUN.EQUIPMENT);   // 0 = first member
child.SetAttribute(ATT.NAME, "/E1301");              // name it IMMEDIATELY
```

Also `CreateFirst`, `CreateLast`, `CreateAfter`, `CreateBefore`. Use `CreateLast` when order does not matter.

## Navigation

```csharp
e.Owner; e.Previous;            // properties
e.Next(); e.FirstMember(); e.Member(3); e.Members();
e.Members(NOUN.NOZZLE);
```

`Members()` materialises one level. For anything deeper, use a collection.

## Collections

```csharp
TypeFilter filter = new TypeFilter(NOUN.NOZZLE);
DBElementCollection c = new DBElementCollection(root, filter);
c.IncludeRoot = true;                       // default is false

foreach (DbElement el in c) { }
```

Constructors take `(root, filter)`, `(db, filter)`, `(dbArray, filter)`, `(DbType, filter)` or `(filter)`.

**Never create or delete inside the `foreach`.** Collect into a `List<DbElement>`, then act.

Progress and cancellation: cast `GetEnumerator()` to `DBElementEnumerator` for `CountSoFar` / `Scanned`; set `Interrupt` on the collection or filter to let the user cancel.

## Filters

Standalone use, no collection needed:

```csharp
filter.Valid(e);            // predicate
filter.FirstMember(owner);
filter.Next(el);
filter.Members(owner);
filter.Parent(el);          // nearest matching ancestor - the clean way to
                            // answer "which SITE is this in"
```

Common constructors:

```csharp
new TypeFilter(NOUN.NOZZLE)               // includes derived types
new ActualTypeFilter(NOUN.EQUIPMENT)      // exact type only
new AttributeTrueFilter(ATT.ISNAME)
new AttributeLikeFilter(ATT.NAME, "/E13*")
new AttributeStringFilter(ATT.DESC, FilterOperator.Contains, "pump")
new AttributeDoubleFilter(ATT.XLEN, FilterOperator.GreaterThan, 1000.0)
new AndFilter(f1, f2)                     // or AndFilter() + Add()
new OrFilter(f1, f2)
new InVolumeFilter(zone, true)
new ExpressionFilter(DbExpression.Parse("( XLEN GT 1000 )"))
```

Structural filters differ in what they take:

- **A filter**: `BelowFilter(f)`, `BelowOrAtFilter(f)`
- **Element types**: `SkipFilter(type)`, `HideFilter(type)`, `HideBelowFilter(type)`, `ShowFilter(type)`, `ShowDescendants(type)`, `BelowType(type)`, `BelowOrAtType(type)`

An empty `AndFilter` accepts everything; an empty `OrFilter` accepts nothing. Handle that when building filters from user input.

## Performance

1. Root the collection as tightly as possible.
2. Cheapest filters first in an `AndFilter` — type tests are cheap, string tests are not.
3. `SkipFilter` branches you do not need. Not descending beats filtering.
4. Avoid `ExpressionFilter` in hot paths.
5. Hoist attribute reads out of inner loops.

## Threading

The API is not thread-safe. UI thread only. Compute on a worker if you must, but marshal back before touching any `Db*` type.
