using System;
using System.Collections;
using System.Windows.Forms;
using Aveva.Core.PMLNet;

namespace MyCompany.E3D.Controls
{
    /// <summary>
    /// A checked list PML has no equivalent for. Shows overloading that PML can
    /// resolve (different arity), returning an ARRAY, and raising an event.
    /// </summary>
    [PMLNetCallable()]
    public class CheckListControl : UserControl
    {
        private readonly ListView mList;

        public event PMLNetDelegate.PMLNetEventHandler OnChecked;

        [PMLNetCallable()]
        public CheckListControl()
        {
            mList = new ListView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = true,
                View = View.List,
                HideSelection = false
            };
            mList.ItemChecked += ListItemChecked;

            this.Controls.Add(mList);
        }

        [PMLNetCallable()]
        public void Assign(CheckListControl that)
        {
            mList.Items.Clear();
            foreach (ListViewItem item in that.mList.Items)
            {
                mList.Items.Add((ListViewItem)item.Clone());
            }
        }

        /// <summary>PML sees ADDVALUE(STRING).</summary>
        [PMLNetCallable()]
        public void AddValue(string value)
        {
            mList.Items.Add(value ?? string.Empty);
        }

        /// <summary>PML sees ADDVALUE(STRING, BOOLEAN) - a distinct signature.</summary>
        [PMLNetCallable()]
        public void AddValue(string value, bool state)
        {
            ListViewItem item = mList.Items.Add(value ?? string.Empty);
            item.Checked = state;
        }

        [PMLNetCallable()]
        public void Clear()
        {
            mList.Items.Clear();
        }

        [PMLNetCallable()]
        public void SetAllChecked(bool state)
        {
            foreach (ListViewItem item in mList.Items)
            {
                item.Checked = state;
            }
        }

        [PMLNetCallable()]
        public double Count()
        {
            return mList.Items.Count;
        }

        /// <summary>Returns a PML ARRAY of the checked values, indexed from 1.</summary>
        [PMLNetCallable()]
        public Hashtable GetSelectedItems()
        {
            Hashtable result = new Hashtable();
            double index = 1;

            foreach (ListViewItem item in mList.Items)
            {
                if (item.Checked)
                {
                    result.Add(index++, item.Text);
                }
            }

            return result;
        }

        private void ListItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (OnChecked == null) return;

            ArrayList args = new ArrayList();
            args.Add((double)(e.Item.Index + 1));   // PML indices start at 1
            args.Add(e.Item.Text);
            args.Add(e.Item.Checked);
            OnChecked(args);
        }
    }
}
