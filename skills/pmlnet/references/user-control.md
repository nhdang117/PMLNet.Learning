# PMLNetCallable User Control

A WinForms `UserControl` hosted inside a PML form's `container` gadget.

Template: `assets/templates/UserControl/`

Everything in `callable-class.md` applies. Three additions:

1. The class derives from `System.Windows.Forms.UserControl`.
2. The constructor carries `[PMLNetCallable()]` **and** calls `InitializeComponent()` if the designer generated one.
3. `Assign` copies visible state, not just compiles.

## Shape

```csharp
using System;
using System.Collections;
using System.Windows.Forms;
using Aveva.Core.PMLNet;

namespace MyCompany.Controls
{
    [PMLNetCallable()]
    public class DatePickerControl : UserControl
    {
        private readonly DateTimePicker mPicker;

        public event PMLNetDelegate.PMLNetEventHandler OnDatePicked;

        [PMLNetCallable()]
        public DatePickerControl()
        {
            mPicker = new DateTimePicker { Dock = DockStyle.Top };
            mPicker.ValueChanged += PickerValueChanged;
            this.Controls.Add(mPicker);
        }

        [PMLNetCallable()]
        public void Assign(DatePickerControl that)
        {
            this.mPicker.Value = that.mPicker.Value;
        }

        [PMLNetCallable()]
        public void SetDate(string dateString)
        {
            DateTime parsed;
            if (!DateTime.TryParse(dateString, out parsed))
                throw new PMLNetException(1000, 1, "not a date");
            mPicker.Value = parsed;
        }

        private void PickerValueChanged(object sender, EventArgs e)
        {
            if (OnDatePicked == null) return;
            ArrayList args = new ArrayList();
            args.Add(mPicker.Value.ToString("HH:mm:ss"));
            args.Add(mPicker.Value.ToString("dd-MMM-yyyy"));
            OnDatePicked(args);
        }
    }
}
```

Building the UI in code avoids `.Designer.cs` and `.resx` entirely, which keeps a generated project portable. Use the designer only when the user asked for it.

## The PML form

```
setup form !!cSharpDatePicker dialog resize
   import |C:\AVEVA\Custom\MyCompany.Controls|
   handle ANY
   endhandle
   using namespace |MyCompany.Controls|

   container .datePickerFrame NOBOX PMLNETCONTROL |date| width 22 height 2
   member .datePicker is DATEPICKERCONTROL
exit

define method .cSharpDatePicker()
   using namespace |MyCompany.Controls|
   !this.datePicker = object DATEPICKERCONTROL()
   !this.datePickerFrame.control = !this.datePicker.handle()
   !this.datePicker.addeventhandler(|OnDatePicked|, !this, |datePicked|)
endmethod

define method .datePicked(!data is ARRAY)
   $p $!data[1] $!data[2]
endmethod
```

Three load-bearing lines:

| Line | Role |
| --- | --- |
| `container ... PMLNETCONTROL` | Declares the host gadget |
| `member .x is CONTROLTYPE` | Declares the typed form member |
| `!this.frame.control = !this.obj.handle()` | Binds the control in |

`.handle()` is provided by the engine — never written in C#.

## Showing it

```
pml rehash all
show !!cSharpDatePicker
```

`pml reload form !!name` reloads PML only; the DLL still needs an E3D restart.

## Sizing

- C#: `Dock = DockStyle.Fill` or explicit `Anchor` on child controls.
- PML: give the container `width`/`height`, and `anchor all` on a resizeable form.
- PML units are not pixels.

## Reusing AVEVA controls

`Aveva.Core.Presentation.DataGrid.DataGridControl` is the grid behind PML's `GridControl`. `PMLNetUtilities.dll` has `PMLImageViewerControl`, `StageBar`, `Wheel`, `PMLFontDialog`, `PMLClipboard`.

## Rules

- Exceptions in WinForms event handlers do **not** become PML errors. Catch inside the handler.
- No AVEVA API calls in the constructor — the WinForms designer runs it at design time.
- `MessageBox.Show` blocks the E3D UI thread.
- Same assembly locking as any PMLNetCallable DLL.
