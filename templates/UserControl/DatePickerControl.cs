using System;
using System.Collections;
using System.Windows.Forms;
using Aveva.Core.PMLNet;

namespace MyCompany.E3D.Controls
{
    /// <summary>
    /// A PMLNetCallable user control, hosted on a PML form through a
    /// container gadget declared with the PMLNETCONTROL keyword.
    ///
    /// The UI is built in code rather than by the designer, so the template
    /// carries no .Designer.cs or .resx and works in any Visual Studio version.
    /// </summary>
    [PMLNetCallable()]
    public class DatePickerControl : UserControl
    {
        private readonly DateTimePicker mPicker;

        /// <summary>
        /// Raised at PML. PMLNetEventHandler is the only signature PML
        /// understands, and it needs no [PMLNetCallable] of its own.
        /// </summary>
        public event PMLNetDelegate.PMLNetEventHandler OnDatePicked;

        [PMLNetCallable()]
        public DatePickerControl()
        {
            mPicker = new DateTimePicker
            {
                Dock = DockStyle.Top,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm:ss dd-MMM-yyyy"
            };
            mPicker.ValueChanged += PickerValueChanged;

            this.Controls.Add(mPicker);
            this.Height = mPicker.PreferredHeight;
        }

        /// <summary>Carry the visible state across a PML assignment.</summary>
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
            {
                throw new PMLNetException(1000, 1,
                    "'" + dateString + "' is not a date");
            }
            mPicker.Value = parsed;
        }

        [PMLNetCallable()]
        public string GetDate()
        {
            return mPicker.Value.ToString("dd-MMM-yyyy");
        }

        [PMLNetCallable()]
        public string GetTime()
        {
            return mPicker.Value.ToString("HH:mm:ss");
        }

        private void PickerValueChanged(object sender, EventArgs e)
        {
            if (OnDatePicked == null) return;

            // Only REAL / STRING / BOOLEAN / ARRAY survive the trip to PML.
            ArrayList args = new ArrayList();
            args.Add(mPicker.Value.ToString("HH:mm:ss"));     // !data[1]
            args.Add(mPicker.Value.ToString("dd-MMM-yyyy"));  // !data[2]
            OnDatePicked(args);
        }
    }
}
