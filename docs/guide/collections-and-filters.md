# 7. Collections and Filters

## What it is

A **filter** is a predicate over elements. A **collection** walks the hierarchy and yields the elements a filter accepts.

```csharp
TypeFilter nozzles = new TypeFilter(DbElementTypeInstance.NOZZLE);
DBElementCollection collection = new DBElementCollection(equipment, nozzles);

foreach (DbElement nozzle in collection)
{
    Console.WriteLine(nozzle.GetString(DbAttributeInstance.NAME));
}
```

That replaces the recursive descent you would otherwise write by hand — and it is faster, because the filter is evaluated inside the database layer rather than by marshalling every element into .NET first.

Both types live in `Aveva.Core.Database.Filters` (`Aveva.Core.Database.Filters.dll`).

## Why it matters

Any real customisation ends up asking "give me all the X below Y where Z". Hand-rolled recursion for that is slower, longer, and gets the edge cases wrong — hidden elements, references crossing databases, deleted members.

## Collections

`DBElementCollection` has a constructor for each scope you might want:

```csharp
// Everything below one element
new DBElementCollection(root, filter);

// Everything in one database
new DBElementCollection(db, filter);

// Everything in several databases
new DBElementCollection(dbArray, filter);

// Everything of a database type in the current MDB
new DBElementCollection(DbType.Design, filter);

// Filter only - scope set later
new DBElementCollection(filter);
```

Two properties matter:

| Property | Effect |
| --- | --- |
| `IncludeRoot` | Whether the root element itself is tested. Default is `false`. |
| `Filter` | Swap the filter without rebuilding the collection. |

```csharp
DBElementCollection collection = new DBElementCollection(site);
collection.IncludeRoot = true;
collection.Filter = new TypeFilter(DbElementTypeInstance.BRANCH);
```

### Iterating

`foreach` is the normal way:

```csharp
foreach (DbElement element in collection)
{
    // ...
}
```

The explicit enumerator gives you progress information, which is worth having for a scan the user is waiting on:

```csharp
DBElementEnumerator iterator = (DBElementEnumerator)collection.GetEnumerator();
while (iterator.MoveNext())
{
    DbElement element = (DbElement)iterator.Current;
    // iterator.CountSoFar - how many have matched
    // iterator.Scanned    - how many have been examined
}
```

::: warning Do not modify while iterating
Creating or deleting elements inside the loop invalidates the enumerator. Collect into a `List<DbElement>` first, then make the changes.

```csharp
List<DbElement> found = new List<DbElement>();
foreach (DbElement e in collection) found.Add(e);
foreach (DbElement e in found) e.SetAttribute(att, value);
```
:::

## Filters without a collection

Every filter derives from `BaseFilter`, which is useful on its own:

```csharp
TypeFilter filter = new TypeFilter(DbElementTypeInstance.NOZZLE);

bool ok            = filter.Valid(element);        // does this element pass?
DbElement first    = filter.FirstMember(equi);     // first matching member
DbElement next     = filter.Next(first);           // next match at the same level
DbElement[] all    = filter.Members(equi);         // all matching members
DbElement ancestor = filter.Parent(nozzle);        // nearest matching ancestor
```

`filter.Parent(element)` is the clean way to answer "which SITE is this element in":

```csharp
TypeFilter siteFilter = new TypeFilter(DbElementTypeInstance.SITE);
DbElement site = siteFilter.Parent(anyElement);
```

## The filter catalogue

About fifty filter classes ship in the assembly. These are the ones that carry the weight.

### By element type

```csharp
new TypeFilter(DbElementTypeInstance.NOZZLE)
new TypeFilter(new[] { DbElementTypeInstance.BOX, DbElementTypeInstance.CYLINDER })

new ActualTypeFilter(DbElementTypeInstance.EQUIPMENT)   // exact type, ignores derived
new BelowOrAtType(DbElementTypeInstance.EQUIPMENT)      // this type or anything under it
```

`TypeFilter` matches the type and its derived types. `ActualTypeFilter` matches exactly. Both accept extra types later via `Add()`.

### By attribute value

```csharp
using Aveva.Core.Database.Filters;

// Boolean attributes
new AttributeTrueFilter(DbAttributeInstance.ISNAME)
new AttributeFalseFilter(DbAttributeInstance.BUIL)

// Unset
new AttributeUnsetFilter(DbAttributeInstance.DESC)

// Strings, with a rich operator set
new AttributeStringFilter(DbAttributeInstance.DESC,
                          FilterOperator.Contains, "pump")

// Wildcards, PML-style
new AttributeLikeFilter(DbAttributeInstance.NAME, "/E13*")

// Numbers
new AttributeDoubleFilter(DbAttributeInstance.XLEN,
                          FilterOperator.GreaterThan, 1000.0)
new AttributeDoubleRangeFilter(DbAttributeInstance.XLEN, 500.0, 1500.0)

// References
new AttributeRefFilter(DbAttributeInstance.SPRE, specElement)
```

`FilterOperator` covers `Equals`, `DoesNotEqual`, `LessThan`, `LessThanOrEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`, `Like`, `NotLike`, `MatchesRegularExpression`, `DoesNotMatch`, `StartsWith`, `Contains`, `EndsWith`, their negations, `InRange` and `InList`.

Every attribute filter also has a constructor taking a `DbQualifier`, for array attributes.

### Combining

```csharp
AndFilter all = new AndFilter();
all.Add(new TypeFilter(DbElementTypeInstance.EQUIPMENT));
all.Add(new AttributeLikeFilter(DbAttributeInstance.NAME, "/E13*"));
all.Add(new AttributeDoubleFilter(DbAttributeInstance.XLEN,
                                  FilterOperator.GreaterThan, 1000.0));

OrFilter either = new OrFilter(
    new TypeFilter(DbElementTypeInstance.BOX),
    new TypeFilter(DbElementTypeInstance.CYLINDER));

// Nest freely
AndFilter combined = new AndFilter(all, either);
```

`AndFilter` and `OrFilter` take two filters, an array, or nothing followed by `Add()` calls. `TrueFilter` and `FalseFilter` exist for the degenerate cases, which is handy when a filter is built from user input that might be empty.

### By geometry

```csharp
// Everything whose volume touches the volume of an element
new InVolumeFilter(zone, false);

// Only things completely inside it
new InVolumeFilter(zone, true);

// Restricted to specific types
new InVolumeFilter(zone,
                   new[] { DbElementTypeInstance.EQUIPMENT },
                   true);

// Against a limits box
new InLimitsBoxFilter(limitsBox, true);
```

### By discipline

Pre-built filters for the common model partitions: `PipingFilter`, `StructuralFilter`, `EquipmentFilter`, `HangerFilter`, `HullFilter`, `SchematicPipingFilter`, `SchematicElectricalFilter`, `SchematicHVACFilter`, `DraftFilter`, `CatalogueSpecWorldFilter`, `CataloguePartWorldFilter`.

These encode AVEVA's own definition of "what counts as piping", which is usually what a user means.

### By expression

When the filter is easier to say in PML than to build from classes:

```csharp
DbExpression expr = DbExpression.Parse("( XLEN GT 1000 AND ISNAME )");
ExpressionFilter filter = new ExpressionFilter(expr);
```

Slower than a native filter, because each element is evaluated through the expression engine. Excellent when the expression comes from user input or configuration.

### Structural filters

Two families here, and the difference in what they take matters.

Taking a **filter**:

| Filter | Meaning |
| --- | --- |
| `BelowFilter(f)` | Something below this element matches `f` |
| `BelowOrAtFilter(f)` | This element, or something below it, matches `f` |

Taking one or more **element types**:

| Filter | Meaning |
| --- | --- |
| `SkipFilter(type)` | Do not descend into elements of this type |
| `HideFilter(type)` | Exclude this type from results, keep scanning |
| `HideBelowFilter(type)` | Exclude everything below this type |
| `ShowFilter(type)` | Include this type explicitly |
| `ShowDescendants(type)` | Include everything below this type |
| `BelowType(type)` / `BelowOrAtType(type)` | Positional tests by type |

```csharp
SkipFilter skipStructure = new SkipFilter(DbElementTypeInstance.STRUCTURE);
HideFilter hideBoxes = new HideFilter(new[]
{
    DbElementTypeInstance.BOX,
    DbElementTypeInstance.CYLINDER
});
```

`SkipFilter` is the performance tool. Not descending into branches you do not care about can turn a minutes-long whole-model scan into a seconds-long one.

## Compound filters and tree navigation

`CompoundFilter` separates three concerns explicitly — what to show, what to hide, and what not to descend into:

```csharp
CompoundFilter compound = new CompoundFilter();
compound.AddShow(new TypeFilter(DbElementTypeInstance.EQUIPMENT));
compound.AddHide(new AttributeTrueFilter(DbAttributeInstance.BUIL));
compound.AddSkip(new SkipFilter(DbElementTypeInstance.STRUCTURE));
```

`ElementTreeNavigator` drives a tree view from one of these, so an explorer-style control shows the same thing the filter says:

```csharp
ElementTreeNavigator navigator = new ElementTreeNavigator(world, compound);

bool hasChildren  = navigator.HasMembers(element);
DbElement[] kids  = navigator.MembersInScan(element);
DbElement parent  = navigator.Parent(element);
DbElement next    = navigator.NextInScan(element);
```

It is the right tool for lazy-loading a large hierarchy into a `TreeView` — ask for members only when a node expands.

## Performance

Scans over a real project are the slowest thing most customisation does. In rough order of effect:

1. **Scope tightly.** A collection rooted at a zone beats one rooted at the world, every time.
2. **Put the cheapest test first in an `AndFilter`.** Type tests are very cheap. String comparisons are not.
3. **`SkipFilter` the branches you do not care about.** Not descending beats filtering.
4. **Prefer native filters over `ExpressionFilter`** in hot paths.
5. **Read attributes once.** `GetString` crosses into the database layer each call; hoist it out of inner loops.
6. **Let the user cancel.** Both `DBElementCollection` and `BaseFilter` expose an `Interrupt` property for exactly this.

```csharp
foreach (DbElement element in collection)
{
    string name = element.GetString(DbAttributeInstance.NAME);   // once
    if (name.StartsWith("/E13")) { /* ... */ }
}
```

## A worked example

Find every nozzle in the design databases whose owning equipment is named `/E13*`, and report its bore:

```csharp
using System;
using System.Collections.Generic;
using Aveva.Core.Database;
using Aveva.Core.Database.Filters;

public static List<string> ReportNozzles()
{
    List<string> lines = new List<string>();

    AndFilter filter = new AndFilter();
    filter.Add(new TypeFilter(DbElementTypeInstance.NOZZLE));
    filter.Add(new AttributeTrueFilter(DbAttributeInstance.ISNAME));

    TypeFilter equiFilter = new TypeFilter(DbElementTypeInstance.EQUIPMENT);

    DBElementCollection collection =
        new DBElementCollection(DbType.Design, filter);

    foreach (DbElement nozzle in collection)
    {
        DbElement equi = equiFilter.Parent(nozzle);
        if (!equi.IsValid) continue;

        string equiName = equi.GetString(DbAttributeInstance.NAME);
        if (!equiName.StartsWith("/E13")) continue;

        double bore = nozzle.GetDouble(DbAttributeInstance.PBOR);
        lines.Add(string.Format("{0}\t{1}\t{2:F1}",
                  equiName,
                  nozzle.GetString(DbAttributeInstance.NAME),
                  bore));
    }

    return lines;
}
```

For a real project you would push the `/E13*` test into the filter with `AttributeLikeFilter` on the owner rather than testing in C#. The version above shows both halves of the pattern — filter what you can, test what you must.

## Rules and gotchas

**Rule — `IncludeRoot` defaults to `false`.** If the root itself should be considered, say so.

**Rule — do not mutate during iteration.** Collect, then act.

**Gotcha — `TypeFilter` includes derived types.** For an exact match, use `ActualTypeFilter`.

**Gotcha — filters are stateful and reusable, but not thread-safe.** Same rule as the rest of the API: one thread.

**Gotcha — an empty `AndFilter` accepts everything**, and an empty `OrFilter` accepts nothing. When building filters from user input, handle the empty case deliberately rather than discovering it in production.

**Gotcha — `Members()` on a filter is one level deep.** For a recursive result you need a collection.

## Checklist

- [ ] The collection is rooted as tightly as the task allows.
- [ ] `IncludeRoot` is set deliberately.
- [ ] Cheap filters come first in `AndFilter`.
- [ ] Long scans set `Interrupt` so the user can cancel.
- [ ] No creation or deletion happens inside a `foreach` over a collection.
- [ ] Attribute reads are hoisted out of inner loops.

## Next

[Chapter 8](./pmlnetcallable-user-control) puts a real WinForms control on a PML form.
