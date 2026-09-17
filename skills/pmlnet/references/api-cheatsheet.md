# API Cheatsheet

Everything here was verified against AVEVA E3D 3.1 assemblies. `using` lines are omitted after the first block in each section.

## Usings you will always need

```csharp
using Aveva.Core.Database;                  // DbElement, DbAttribute, MDB, CurrentElement
using Aveva.Core.Database.Filters;          // collections and filters
using Aveva.Core.Geometry;                  // Position, Direction, Orientation
using Aveva.Core.Utilities.Messaging;       // PdmsMessage, PdmsException
using Aveva.Core.PMLNet;                    // PMLNetCallable, PMLNetException
using Aveva.ApplicationFramework;           // IAddinInjected, IDependencyResolver
using Aveva.ApplicationFramework.Presentation;  // Command, IWindowManager, DockedWindow
```

Common aliases, as used in AVEVA's own samples:

```csharp
using ATT  = Aveva.Core.Database.DbAttributeInstance;
using NOUN = Aveva.Core.Database.DbElementTypeInstance;
using Ps   = Aveva.Core.Database.DbPseudoAttribute;
```

## Elements

```csharp
DbElement e = DbElement.GetElement("/E1301");           // by name
DbElement e = DbElement.GetElement(new int[] {7205, 1});// by ref
DbElement e = CurrentElement.Element;                   // the CE

bool ok  = e.IsValid;          // ALWAYS check this
bool nul = e.IsNull;
bool del = e.IsDeleted;

DbElementType t = e.ElementType;
int[] reference = e.Ref;
Db database     = e.Db;
```

## Hierarchy

```csharp
DbElement owner = e.Owner;
DbElement prev  = e.Previous;              // property
DbElement next  = e.Next();                // method
DbElement next  = e.Next(NOUN.NOZZLE);

DbElement first = e.FirstMember();
DbElement first = e.FirstMember(NOUN.NOZZLE);
DbElement nth   = e.Member(3);

DbElement[] members = e.Members();
DbElement[] nozzles = e.Members(NOUN.NOZZLE);

bool below = ancestor.IsDescendant(child);
```

## Reading attributes

```csharp
string s  = e.GetString(ATT.NAME);
double d  = e.GetDouble(ATT.XLEN);
int    i  = e.GetInteger(ATT.REV);
bool   b  = e.GetBool(ATT.BUIL);

Position    p = e.GetPosition(ATT.POS);
Direction   v = e.GetDirection(ATT.HDIR);
Orientation o = e.GetOrientation(ATT.ORI);
DbElement   r = e.GetElement(ATT.SPRE);

string[] sa = e.GetAsStringArray(ATT.ALIST);
double[] da = e.GetDoubleArray(ATT.PARA);

string any = e.GetAsString(ATT.POS);       // formatted, never throws on type
double idx = e.GetDouble(ATT.PARA, 3);     // array index qualifier

DbAttribute[] all = e.GetAttributes();
```

## Writing attributes

```csharp
e.Claim();                                  // first, always

e.SetAttribute(ATT.DESC, "text");
e.SetAttribute(ATT.BUIL, true);
e.SetAttribute(ATT.REV, 3);
e.SetAttribute(ATT.XLEN, 1500.0);
e.SetAttribute(ATT.POS, new Position(1000, 2000, 3000));

PdmsMessage error;
if (!e.SetAttribute(ATT.DESC, "text", out error))
    Console.WriteLine(error.MessageText());

e.SetAttributeDefault(uda);
bool isDefault = e.AtDefault(uda);
```

## UDAs

```csharp
DbAttribute uda = DbAttribute.GetDbAttribute(":MYUDA");   // null if undefined
if (uda != null)
{
    string v = e.GetString(uda);
    e.SetAttribute(uda, "new value");
}

DbAttribute[] udas = e.ElementType.GetAllUdas();
```

## Creating, copying, deleting

```csharp
DbElement child = owner.Create(0, NOUN.EQUIPMENT);   // 0 = first member
child.SetAttribute(ATT.NAME, "/E1301");              // name it immediately

DbElement a = e.CreateAfter(NOUN.EQUIPMENT);
DbElement b = e.CreateBefore(NOUN.EQUIPMENT);
DbElement f = owner.CreateFirst(NOUN.EQUIPMENT);
DbElement l = owner.CreateLast(NOUN.EQUIPMENT);

target.Copy(source);                                  // attributes only

DbCopyOption opt = new DbCopyOption
{
    FromName = "E1301", ToName = "E1302", Rename = true
};
target.CopyHierarchy(source, opt);

e.InsertAfterLast(newOwner);                          // move
e.ChangeType(DbElementType.GetElementType("OLET"));

e.Delete();
PdmsMessage msg;
bool okDelete = e.Delete(out msg);
```

## MDB, databases, project

```csharp
MDB mdb = MDB.CurrentMDB;

string name    = mdb.Name;
Db[] all       = mdb.GetDBArray();
Db[] design    = mdb.GetDBArray(DbType.Design);
Db one         = mdb.GetDB(number);
DbElement world = mdb.GetFirstWorld(DbType.Design);

DbElement found   = mdb.FindElement(DbType.Design, "/E1301");
DbElement[] many  = mdb.FindElements(DbType.Design, "/E1301");

bool saved = mdb.SaveWork("comment");
bool got   = mdb.GetWork();
bool quit  = mdb.QuitWork();

mdb.Claim(elements);
mdb.Release(elements);
mdb.ReleaseAll();

Project p = Project.CurrentProject;
string code = p.Code;
string user = p.UserName;
bool open   = p.IsOpen();
```

## Element types

```csharp
DbElementType t = NOUN.EQUIPMENT;
DbElementType t = DbElementType.GetElementType("EQUI");

string n  = t.Name;
string sn = t.ShortName;

DbAttribute[] sys   = t.SystemAttributes();
DbAttribute[] udas  = t.GetAllUdas();
DbElementType[] mem = t.MemberTypes();
DbElementType[] own = t.OwnerTypes();
bool valid = t.IsAttributeValid(ATT.XLEN);
```

## Collections and filters

```csharp
TypeFilter filter = new TypeFilter(NOUN.NOZZLE);

DBElementCollection c = new DBElementCollection(root, filter);
DBElementCollection c = new DBElementCollection(DbType.Design, filter);
DBElementCollection c = new DBElementCollection(db, filter);
c.IncludeRoot = true;

foreach (DbElement el in c) { }

DBElementEnumerator it = (DBElementEnumerator)c.GetEnumerator();
while (it.MoveNext())
{
    DbElement el = (DbElement)it.Current;
    int matched = it.CountSoFar;
    int seen    = it.Scanned;
}

// Filters standalone
bool pass         = filter.Valid(e);
DbElement first   = filter.FirstMember(owner);
DbElement next    = filter.Next(first);
DbElement[] all   = filter.Members(owner);
DbElement parent  = filter.Parent(e);      // nearest matching ancestor
```

### Filter constructors

```csharp
new TypeFilter(NOUN.NOZZLE)
new TypeFilter(new[] { NOUN.BOX, NOUN.CYLINDER })
new ActualTypeFilter(NOUN.EQUIPMENT)
new BelowOrAtType(NOUN.EQUIPMENT)

new AttributeTrueFilter(ATT.ISNAME)
new AttributeFalseFilter(ATT.BUIL)
new AttributeUnsetFilter(ATT.DESC)
new AttributeStringFilter(ATT.DESC, FilterOperator.Contains, "pump")
new AttributeLikeFilter(ATT.NAME, "/E13*")
new AttributeDoubleFilter(ATT.XLEN, FilterOperator.GreaterThan, 1000.0)
new AttributeDoubleRangeFilter(ATT.XLEN, 500.0, 1500.0)
new AttributeRefFilter(ATT.SPRE, specElement)

new AndFilter(f1, f2)         // also AndFilter() + Add()
new OrFilter(f1, f2)
new TrueFilter()
new FalseFilter()

new InVolumeFilter(zone, true)                  // completelyWithin
new InLimitsBoxFilter(limitsBox, true)

new BelowFilter(f)            // takes a filter
new SkipFilter(NOUN.STRUCTURE)// takes element types
new HideFilter(NOUN.BOX)
new ShowFilter(NOUN.EQUIPMENT)

new ExpressionFilter(DbExpression.Parse("( XLEN GT 1000 )"))
```

`FilterOperator`: `Equals`, `DoesNotEqual`, `LessThan`, `LessThanOrEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`, `Like`, `NotLike`, `MatchesRegularExpression`, `DoesNotMatch`, `StartsWith`, `Contains`, `EndsWith`, `DoesNotStartWith`, `DoesNotContain`, `DoesNotEndWith`, `InRange`, `InList`.

## Expressions and rules

```csharp
DbExpression expr = DbExpression.Parse("HEIGHT OF PREV * 2");

double d  = e.EvaluateDouble(expr, DbAttributeUnit.DIST);
string s  = e.EvaluateString(expr);
bool   b  = e.EvaluateBool(expr);
DbElement r = e.EvaluateElement(expr);
Position p  = e.EvaluatePosition(expr);

DbRule rule = DbRule.CreateDbRule(expr, DbRuleStatus.DYNAMIC, DbExpressionType.REAL);
e.SetRule(ATT.XLEN, rule);
bool exists    = e.ExistRule(ATT.XLEN);
bool outOfDate = e.VerifyRule(ATT.XLEN);
e.ExecuteRule(ATT.XLEN);
e.ExecuteAllRules();
e.DeleteRule(ATT.XLEN);
```

## Undo

```csharp
using Aveva.Core.Utilities.Undo;

UndoTransaction t = UndoTransaction.GetUndoTransaction();
t.StartTransaction("Description shown in the undo list");
try   { /* database changes */ }
finally { t.EndTransaction(); }

UndoTransaction.PerformUndo();
UndoTransaction.PerformRedo();
```

## PML .NET

```csharp
[assembly: PMLNetCallable()]        // in AssemblyInfo.cs

[PMLNetCallable()]
public class MyClass
{
    [PMLNetCallable()] public MyClass() { }
    [PMLNetCallable()] public void Assign(MyClass that) { }
    [PMLNetCallable()] public double Compute() { return 1.0; }

    public event PMLNetDelegate.PMLNetEventHandler OnDone;   // no attribute needed

    private void Raise()
    {
        ArrayList args = new ArrayList();
        args.Add((double)1);
        args.Add("text");
        if (OnDone != null) OnDone(args);
    }
}

throw new PMLNetException(1000, 1, "message");
```

Type marshalling: `REAL` ↔ `double`, `STRING` ↔ `string`, `BOOLEAN` ↔ `bool`, `ARRAY` ↔ `Hashtable` (keys are `double`, starting at 1).

From PML:

```
import |C:\AVEVA\Custom\MyCompany.Tools|
handle ANY
endhandle
using namespace |MyCompany.Tools|
!o = object MYCLASS()
q var !o.methods()
```

## Addins

```csharp
public class MyAddin : IAddinInjected
{
    public string Name        { get { return "MyAddin"; } }
    public string Description { get { return "..."; } }

    public void Start(IDependencyResolver resolver)
    {
        IWindowManager  wm = resolver.GetImplementationOf<IWindowManager>();
        ICommandManager cm = resolver.GetImplementationOf<ICommandManager>();

        DockedWindow w = wm.CreateDockedWindow(
            "MyCompany.Key", "Title", new MyControl(), DockedPosition.Right);
        w.SaveLayout = true;

        cm.Commands.Add(new MyCommand(w));
    }

    public void Start(ServiceManager serviceManager) { }
    public void Stop() { }
}

public class MyCommand : Command
{
    public MyCommand(DockedWindow w) { this.Key = "MyCompany.MyCommand"; }
    public override void Execute() { }
}
```

`DockedPosition`: `Left`, `Right`, `Top`, `Bottom`, `Floating`.

## Pseudo UDAs

```csharp
DbAttribute uda = DbAttribute.GetDbAttribute(":VOLUME");
if (uda != null)
{
    Ps.GetDoubleDelegate d = new Ps.GetDoubleDelegate(Calc);
    Ps.AddGetDoubleAttribute(uda, NOUN.BOX, d);          // or without the type
    Ps.AddGetDoubleAttributeForOneType(uda, NOUN.BOX, d);// exact type only
}

static double Calc(DbElement e, DbAttribute a, DbQualifier q)
{
    return e.GetDouble(ATT.XLEN) * e.GetDouble(ATT.YLEN) * e.GetDouble(ATT.ZLEN);
}
```

Getter delegates: `GetIntDelegate`, `GetDoubleDelegate`, `GetBoolDelegate`, `GetStringDelegate`, `GetRefDelegate`, `GetPositionDelegate`, `GetDirectionDelegate`, `GetOrientationDelegate`, and `...ArrayDelegate` for each. Setters mirror them and always take an element type.

## Standalone

```csharp
using Aveva.E3D.Standalone;

if (!Standalone.Start()) return 1;
// Standalone.Start(environmentHashtable)
// Standalone.Start(moduleNumber)

PdmsMessage error;
if (!Standalone.Open("SAM", "SYSTEM", "PASS", "SAMPLE", out error))
{
    Console.Error.WriteLine(error.MessageText());
    return 2;
}

// ... normal API ...

MDB.CurrentMDB.CloseMDB();        // Standalone.Close() is obsolete in 3.1
Project.CurrentProject.Close();
```

## Running a PML command

```csharp
using Aveva.Core.Utilities.CommandLine;

Command.CreateCommand("SHOW !!MyForm").Run();
```

## Attribute names worth memorising

| Attribute | Meaning |
| --- | --- |
| `NAME` | Full name with leading `/`, empty if unnamed |
| `NAMN` | Name without the leading `/` |
| `FLNM` | Full name, falling back to the reference — use this in UI |
| `DESC` | Description |
| `TYPE` | Element type |
| `OWNER` | Owning element |
| `POS`, `ORI` | Position, orientation |
| `XLEN`, `YLEN`, `ZLEN` | Box dimensions |
| `HREF`, `TREF` | Branch head and tail references |
| `HDIR`, `TDIR` | Branch head and tail directions |
| `SPRE` | Specification reference |
| `CATR` | Catalogue reference |
| `BORE`, `HBOR`, `TBOR`, `LBOR`, `PBOR` | Bore: generic, head, tail, leave, p-point |
| `BUIL` | Built flag |
| `ISNAME` | Whether the element is named |
| `LCLM`, `LCLMH` | Claim state, element and hierarchy |
