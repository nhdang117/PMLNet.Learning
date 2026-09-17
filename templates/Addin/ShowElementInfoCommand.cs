using System;
using Aveva.ApplicationFramework;
using Aveva.ApplicationFramework.Presentation;

namespace MyCompany.E3D.Addin
{
    /// <summary>
    /// Toggles the addin's docked window.
    ///
    /// Bind it to a StateButtonTool in a .uic file using the Key below. The
    /// key must match exactly, and must be unique across every loaded addin.
    /// </summary>
    public class ShowElementInfoCommand : Command
    {
        private readonly DockedWindow mWindow;

        public ShowElementInfoCommand(DockedWindow window)
        {
            this.Key = "MyCompany.E3D.Addin.ShowElementInfo";

            mWindow = window;
            mWindow.Closed += OnWindowClosed;

            DependencyResolver.GetImplementationOf<IWindowManager>()
                .WindowLayoutLoaded += OnWindowLayoutLoaded;
        }

        private void OnWindowLayoutLoaded(object sender, EventArgs e)
        {
            // Match the toggle state to the layout E3D just restored.
            this.Checked = mWindow.Visible;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            this.Checked = false;
        }

        public override void Execute()
        {
            if (this.Checked)
            {
                mWindow.Show();
            }
            else
            {
                mWindow.Hide();
            }
        }
    }
}
