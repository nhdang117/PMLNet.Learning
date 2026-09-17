# 8. How to Create a PMLNetCallable User Control

## What it is

A PMLNetCallable class that happens to derive from `System.Windows.Forms.UserControl`. A PML form declares a `container` gadget with the `PMLNETCONTROL` keyword, and at runtime the control is placed inside it.

```
PML form
┌──────────────────────────────────────┐
│  text .date                          │
│  ┌────────────────────────────────┐  │
│  │ container .frame PMLNETCONTROL │  │ ◄── your WinForms UserControl
│  │   [ 09:24:11  17-Sep-2026  ▾ ] │  │     lives in here
│  └────────────────────────────────┘  │
│  button .update |Update|             │
└──────────────────────────────────────┘
```

You get the whole WinForms toolbox — tree views, grids, date pickers, web browsers, charts — on a form that is otherwise ordinary PML.

## Why it matters

PML's gadget set is fixed and dated. When a user asks for something PML cannot draw, this is usually the cheapest answer, because the surrounding form, its layout and its callbacks stay in PML where they are quick to change.

## The rules, on top of Chapter 5

A user control is a PMLNetCallable class, so everything in [Chapter 5](./pmlnetcallable-class) applies. Three additions:

1. The class derives from `UserControl`.
2. The constructor must call `InitializeComponent()` — and still carry `[PMLNetCallable()]`.
3. `Assign` should transfer meaningful visual state, not just compile.

## The control

```csharp
using System;
using System.Collections;
using System.Windows.Forms;
using Aveva.Core.PMLNet;

namespace MyCompany.Tools
{
    [PMLNetCallable()]
    public partial class DatePickerControl : UserControl
    {
        [PMLNetCallable()]
        public DatePickerControl()
        {
            InitializeComponent();
        }

        // Carry the visible state across a PML assignment.
        [PMLNetCallable()]
        public void Assign(DatePickerControl that)
        {
            this.dateTimePicker1.Value = that.dateTimePicker1.Value;
        }

        // Fired at PML whenever the user picks a date.
        public event PMLNetDelegate.PMLNetEventHandler OnDatePicked;

        [PMLNetCallable()]
        public void SetDate(string dateString)
        {
            DateTime parsed;
            if (!DateTime.TryParse(dateString, out parsed))
            {
                throw new PMLNetException(1000, 1,
                    "'" + dateString + "' is not a date");
            }
            this.dateTimePicker1.Value = parsed;
        }

        [PMLNetCallable()]
        public string GetDate()
        {
            return this.dateTimePicker1.Value.ToString("dd-MMM-yyyy");
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            if (OnDatePicked == null) return;

            ArrayList args = new ArrayList();
            args.Add(this.dateTimePicker1.Value.ToLongTimeString());
            args.Add(this.dateTimePicker1.Value.ToLongDateString());
            OnDatePicked(args);
        }
    }
}
```

## The PML form

```
setup form !!cSharpDatePicker dialog resizeable
   import |C:\AVEVA\Custom\MyCompany.Tools|
   handle ANY
   endhandle
   using namespace |MyCompany.Tools|

   text .date width 15 is STRING
   button .update |Update| at xmax ymin

   container .datePickerFrame NOBOX PMLNETCONTROL |date| width 22 height 2
   member .datePicker is DATEPICKERCONTROL
exit

define method .cSharpDatePicker()
   using namespace |MyCompany.Tools|

   -- create the .NET object
   !this.datePicker = object DATEPICKERCONTROL()

   -- hand its window handle to the container gadget
   !this.datePickerFrame.control = !this.datePicker.handle()

   -- wire the .NET event to a PML method
   !this.datePicker.addeventhandler(|OnDatePicked|, !this, |datePicked|)

   !this.update.callback = |!this.datePicker.setDate(!this.date.val)|
endmethod

define method .datePicked(!data is ARRAY)
   !this.date.val = !data[2]
endmethod
```

Three lines do the work:

| Line | What it does |
| --- | --- |
| `container ... PMLNETCONTROL` | Reserves the space and declares that a .NET control fills it |
| `member .datePicker is DATEPICKERCONTROL` | Declares the typed member on the form |
| `!this.frame.control = !this.obj.handle()` | Binds the control into the container |

`.handle()` is added automatically by the PML .NET engine to every PMLNetCallable control. You never write it in C#.

## Showing it

```
pml rehash all
show !!cSharpDatePicker
```

`pml rehash all` rebuilds PML's index of forms and functions. A new `.pmlfrm` file is invisible until you run it.

While iterating on the form itself:

```
pml reload form !!cSharpDatePicker
```

That reloads the PML without restarting E3D. It does **not** reload the DLL — that still needs a restart.

## Events in detail

`PMLNetDelegate.PMLNetEventHandler` is the only event signature PML understands. It takes one `ArrayList`.

```csharp
public event PMLNetDelegate.PMLNetEventHandler OnSelectionChanged;
```

**The event itself does not need `[PMLNetCallable()]`** — the delegate type is already known to the PML .NET engine.

Raising it:

```csharp
if (OnSelectionChanged != null)
{
    ArrayList args = new ArrayList();
    args.Add((double)index);            // REAL
    args.Add(selectedText);             // STRING
    args.Add(isChecked);                // BOOLEAN
    OnSelectionChanged(args);
}
```

Subscribing from PML:

```
!this.control.addeventhandler(|OnSelectionChanged|, !this, |selectionChanged|)
```

| Argument | Meaning |
| --- | --- |
| 1 | The event name, exactly as spelled in C# |
| 2 | The PML object holding the handler — usually `!this` |
| 3 | The PML method name to call |

The handler takes exactly one `ARRAY`:

```
define method .selectionChanged(!data is ARRAY)
   $p Index $!data[1], text $!data[2]
endmethod
```

`removeeventhandler(|EventName|, !token)` unsubscribes, using the REAL returned by `addeventhandler`.

::: warning The event name is a string
Rename the C# event and PML keeps calling `addeventhandler` with the old name. There is no compile-time link and no error — the handler simply never fires. Grep your PML when you rename.
:::

## Sizing and anchoring

The container gadget defines the space; the control fills it. Getting this wrong produces a control clipped at 20% of its intended size.

- In C#, set the child controls' `Dock` or `Anchor` so they follow the `UserControl`.
- In PML, give the container a width and height in PML units, and `anchor all` if the form is resizeable.

```
container .frame NOBOX PMLNETCONTROL |grid| anchor all width 29 height 7.5
```

PML units are not pixels. Expect a round or two of trial and error.

## Reusing AVEVA's own controls

Some AVEVA controls are themselves PMLNetCallable and can be hosted inside your control:

```csharp
using Aveva.Core.Presentation.DataGrid;

public partial class ResultsControl : UserControl
{
    private DataGridControl mGrid;

    public ResultsControl()
    {
        InitializeComponent();
        mGrid = new DataGridControl();
        mGrid.Dock = DockStyle.Fill;
        mResultsPanel.Controls.Add(mGrid);
    }
}
```

`Aveva.Core.Presentation.DataGrid.dll` holds `DataGridControl` and its PML-facing wrapper `PMLDataGridControl` — the grid behind PML's `GridControl`. The older `NetGridControl` in `GridControl.dll` is still present for compatibility; new code should use `DataGridControl`.

`PMLNetUtilities.dll` holds several more ready-made ones — `PMLImageViewerControl`, `StageBar`, `Wheel`, `PMLFontDialog`, `PMLClipboard`. Worth checking before you build something from scratch.

## A second example: a checked list

The pattern generalises. A control that exposes list operations to PML:

```csharp
[PMLNetCallable()]
public partial class CheckListBoxControl : UserControl
{
    [PMLNetCallable()]
    public CheckListBoxControl()
    {
        InitializeComponent();
        this.listView1.CheckBoxes = true;
        this.listView1.View = View.List;
    }

    [PMLNetCallable()]
    public void Assign(CheckListBoxControl that)
    {
        this.listView1.Items.Clear();
        foreach (ListViewItem item in that.listView1.Items)
        {
            this.listView1.Items.Add((ListViewItem)item.Clone());
        }
    }

    public event PMLNetDelegate.PMLNetEventHandler OnChecked;

    [PMLNetCallable()]
    public void AddValue(string value)
    {
        this.listView1.Items.Add(value);
    }

    [PMLNetCallable()]
    public void AddValue(string value, bool state)
    {
        ListViewItem item = this.listView1.Items.Add(value);
        item.Checked = state;
    }

    [PMLNetCallable()]
    public void Clear()
    {
        this.listView1.Items.Clear();
    }

    [PMLNetCallable()]
    public void SetAllChecked(bool state)
    {
        foreach (ListViewItem item in this.listView1.Items)
        {
            item.Checked = state;
        }
    }

    // Hashtable -> PML ARRAY, keys from 1
    [PMLNetCallable()]
    public Hashtable GetSelectedItems()
    {
        Hashtable result = new Hashtable();
        double key = 1;
        foreach (ListViewItem item in this.listView1.Items)
        {
            if (item.Checked)
            {
                result.Add(key++, item.Text);
            }
        }
        return result;
    }

    private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
    {
        if (OnChecked == null) return;

        ArrayList args = new ArrayList();
        args.Add((double)(e.Item.Index + 1));
        args.Add(e.Item.Text);
        args.Add(e.Item.Checked);
        OnChecked(args);
    }
}
```

Note the overloaded `AddValue` — PML sees `ADDVALUE(STRING)` and `ADDVALUE(STRING, BOOLEAN)`, two distinct signatures, which is exactly the overloading PML can resolve.

## Rules and gotchas

**Rule — the constructor calls `InitializeComponent()`.** Without it the control is an empty grey rectangle and nothing explains why.

**Rule — `Assign` should copy visible state.** PML calls it on `!a = !b`. An empty `Assign` on a control with state gives two variables that look identical and behave differently.

**Gotcha — the DLL is locked once the form is shown.** Same restart cycle as any PMLNetCallable assembly.

**Gotcha — exceptions inside WinForms event handlers.** An unhandled exception in `dateTimePicker1_ValueChanged` does not become a PML error; it surfaces as a .NET crash dialog, or nothing at all. Catch inside the handler and either swallow deliberately or raise a PML event carrying the message.

**Gotcha — modal dialogs.** `MessageBox.Show` from inside a hosted control works, but blocks the E3D UI thread. Fine for confirmation; wrong for progress.

**Gotcha — the designer needs a parameterless constructor with no side effects.** Do not touch the AVEVA API in the constructor, or the WinForms designer will fail to load the control at design time.

## Checklist

- [ ] The class derives from `UserControl`, is `public`, and has `[PMLNetCallable()]`.
- [ ] The constructor has the attribute and calls `InitializeComponent()`.
- [ ] `Assign` copies the visible state.
- [ ] Events use `PMLNetDelegate.PMLNetEventHandler` and pass only REAL/STRING/BOOLEAN/ARRAY.
- [ ] The container gadget uses the `PMLNETCONTROL` keyword.
- [ ] The constructor method sets `frame.control = object.handle()`.
- [ ] `pml rehash all` has been run since the `.pmlfrm` was created.
- [ ] Child controls are docked or anchored so they resize with the container.
- [ ] No AVEVA API calls happen in the constructor.

## Next

[Chapter 9](./dotnet-addin) drops PML entirely and loads code at E3D startup.
