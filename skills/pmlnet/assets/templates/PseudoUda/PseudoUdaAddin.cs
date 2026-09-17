using System;
using Aveva.ApplicationFramework;

namespace MyCompany.E3D.PseudoUdas
{
    /// <summary>
    /// Registers pseudo UDA handlers at E3D startup.
    ///
    /// Registration is per-process and is not persisted, so it has to happen
    /// every session - which is exactly what an addin is for. Doing it from a
    /// PMLNetCallable class instead would make an attribute's value depend on
    /// whether someone had imported the assembly yet.
    ///
    /// The UDAs themselves must exist in Lexicon AND be marked as pseudo.
    /// Without the pseudo flag these handlers are never called.
    /// </summary>
    public class PseudoUdaAddin : IAddinInjected
    {
        public string Name
        {
            get { return "PseudoUdaAddin"; }
        }

        public string Description
        {
            get { return "Registers calculated (pseudo) UDAs"; }
        }

        public void Start(IDependencyResolver resolver)
        {
            try
            {
                VolumeUda.Register();
                SiteCodeUda.Register();
            }
            catch (Exception ex)
            {
                // Never let a registration failure take E3D startup down.
                Console.WriteLine("PseudoUdaAddin failed to start: " + ex);
            }
        }

        public void Start(ServiceManager serviceManager)
        {
        }

        public void Stop()
        {
        }
    }
}
