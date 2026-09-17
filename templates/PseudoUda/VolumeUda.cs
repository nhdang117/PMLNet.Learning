using System;
using Aveva.Core.Database;
using ATT = Aveva.Core.Database.DbAttributeInstance;
using NOUN = Aveva.Core.Database.DbElementTypeInstance;
using Ps = Aveva.Core.Database.DbPseudoAttribute;

namespace MyCompany.E3D.PseudoUdas
{
    /// <summary>
    /// :VOLUME - a real UDA computed from the element's dimensions.
    ///
    /// Test with:  q var !ce.:VOLUME
    /// </summary>
    internal static class VolumeUda
    {
        private const string UdaName = ":VOLUME";

        public static void Register()
        {
            // Returns null when the project does not define the UDA. That is a
            // normal condition, not an error.
            DbAttribute uda = DbAttribute.GetDbAttribute(UdaName);
            if (uda == null) return;

            // The delegate type must match the UDA's declared type in Lexicon.
            // A mismatch registers without complaint and never fires.
            Ps.GetDoubleDelegate box = new Ps.GetDoubleDelegate(BoxVolume);
            Ps.AddGetDoubleAttribute(uda, NOUN.BOX, box);

            Ps.GetDoubleDelegate cylinder = new Ps.GetDoubleDelegate(CylinderVolume);
            Ps.AddGetDoubleAttribute(uda, NOUN.CYLINDER, cylinder);
        }

        /// <summary>
        /// Runs on EVERY read of the attribute - once per element per grid
        /// refresh, per report row, per query. Keep it cheap, do no I/O, show
        /// no UI, write nothing to the database, and never throw.
        /// </summary>
        private static double BoxVolume(DbElement element,
                                        DbAttribute attribute,
                                        DbQualifier qualifier)
        {
            try
            {
                return element.GetDouble(ATT.XLEN)
                     * element.GetDouble(ATT.YLEN)
                     * element.GetDouble(ATT.ZLEN);
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        private static double CylinderVolume(DbElement element,
                                             DbAttribute attribute,
                                             DbQualifier qualifier)
        {
            try
            {
                double diameter = element.GetDouble(ATT.DIAM);
                double height = element.GetDouble(ATT.HEIG);
                double radius = diameter / 2.0;
                return Math.PI * radius * radius * height;
            }
            catch (Exception)
            {
                return 0.0;
            }
        }
    }
}
