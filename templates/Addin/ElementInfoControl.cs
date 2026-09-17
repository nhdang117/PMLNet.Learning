using System;
using System.Windows.Forms;
using Aveva.Core.Database;

namespace MyCompany.E3D.Addin
{
    /// <summary>
    /// The docked window's contents. Built in code, so the template needs no
    /// .Designer.cs or .resx.
    /// </summary>
    public class ElementInfoControl : UserControl
    {
        private readonly ListView mList;

        public ElementInfoControl()
        {
            mList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false
            };
            mList.Columns.Add("Attribute", 110);
            mList.Columns.Add("Value", 150);

            this.Controls.Add(mList);
        }

        public void Clear()
        {
            mList.Items.Clear();
        }

        /// <summary>
        /// Lists every attribute of the element. GetAsString is used because it
        /// formats any attribute type and does not throw on a mismatch.
        /// </summary>
        public void Show(DbElement element)
        {
            mList.BeginUpdate();
            try
            {
                mList.Items.Clear();

                if (!element.IsValid) return;

                foreach (DbAttribute attribute in element.GetAttributes())
                {
                    string value;
                    try
                    {
                        value = element.GetAsString(attribute);
                    }
                    catch (Exception)
                    {
                        // Some attributes are not readable for some element
                        // states. Skipping is correct for a best-effort browser.
                        continue;
                    }

                    ListViewItem item = mList.Items.Add(attribute.Name);
                    item.SubItems.Add(value);
                }
            }
            finally
            {
                mList.EndUpdate();
            }
        }
    }
}
