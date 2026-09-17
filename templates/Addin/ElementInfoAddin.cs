using System;
using Aveva.ApplicationFramework;
using Aveva.ApplicationFramework.Presentation;
using Aveva.Core.Database;

namespace MyCompany.E3D.Addin
{
    /// <summary>
    /// A .NET addin for AVEVA E3D.
    ///
    /// Register it by adding this line to your own copy of DesignAddins.xml
    /// (no .dll extension):
    ///     <string>C:\AVEVA\Custom\MyCompany.E3D.Addin</string>
    /// </summary>
    public class ElementInfoAddin : IAddinInjected
    {
        private const string WindowKey = "MyCompany.E3D.Addin.ElementInfo";

        private DockedWindow mWindow;
        private ElementInfoControl mControl;
        private bool mSubscribed;

        public string Name
        {
            get { return "ElementInfoAddin"; }
        }

        public string Description
        {
            get { return "Shows information about the current element"; }
        }

        /// <summary>
        /// The injected entry point - use this one for new code.
        /// It must never throw: an exception here can take E3D startup with it.
        /// </summary>
        public void Start(IDependencyResolver resolver)
        {
            try
            {
                IWindowManager windowManager =
                    resolver.GetImplementationOf<IWindowManager>();

                mControl = new ElementInfoControl();

                mWindow = windowManager.CreateDockedWindow(
                    WindowKey,
                    "Element Info",
                    mControl,
                    DockedPosition.Right);

                mWindow.Width = 280;

                // Remember where the user put it between sessions.
                mWindow.SaveLayout = true;

                ICommandManager commandManager =
                    resolver.GetImplementationOf<ICommandManager>();

                commandManager.Commands.Add(new ShowElementInfoCommand(mWindow));

                CurrentElement.CurrentElementChanged += OnCurrentElementChanged;
                mSubscribed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ElementInfoAddin failed to start: " + ex);
            }
        }

        /// <summary>
        /// Required by the host. Intentionally empty - all setup lives in the
        /// injected overload, and doing it in both would run it twice.
        /// </summary>
        public void Start(ServiceManager serviceManager)
        {
        }

        public void Stop()
        {
            if (mSubscribed)
            {
                CurrentElement.CurrentElementChanged -= OnCurrentElementChanged;
                mSubscribed = false;
            }
        }

        private void OnCurrentElementChanged(object sender,
                                             CurrentElementChangedEventArgs e)
        {
            try
            {
                DbElement element = CurrentElement.Element;
                if (!element.IsValid)
                {
                    mControl.Clear();
                    return;
                }

                mWindow.Title = "Element Info - "
                    + element.GetAsString(DbAttributeInstance.FLNM);

                mControl.Show(element);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ElementInfoAddin: " + ex.Message);
            }
        }
    }
}
