using System;
using System.Collections.Generic;
using Aveva.Core.Database;
using Aveva.Core.Database.Filters;
using ATT = Aveva.Core.Database.DbAttributeInstance;
using NOUN = Aveva.Core.Database.DbElementTypeInstance;
using Ps = Aveva.Core.Database.DbPseudoAttribute;

namespace MyCompany.E3D.PseudoUdas
{
    /// <summary>
    /// :SITECODE - a text UDA derived from the owning SITE's name.
    ///
    /// Also shows the caching pattern. The handler runs on every read, so
    /// anything expensive must be computed once and remembered. Never do I/O
    /// directly in a handler.
    /// </summary>
    internal static class SiteCodeUda
    {
        private const string UdaName = ":SITECODE";

        private static readonly Dictionary<string, string> Cache =
            new Dictionary<string, string>();

        public static void Register()
        {
            DbAttribute uda = DbAttribute.GetDbAttribute(UdaName);
            if (uda == null) return;

            Ps.GetStringDelegate handler = new Ps.GetStringDelegate(SiteCode);

            // No element type argument: every type the UDA is valid for.
            Ps.AddGetStringAttribute(uda, handler);
        }

        private static string SiteCode(DbElement element,
                                       DbAttribute attribute,
                                       DbQualifier qualifier)
        {
            try
            {
                TypeFilter siteFilter = new TypeFilter(NOUN.SITE);
                DbElement site = siteFilter.Parent(element);

                if (!site.IsValid) return string.Empty;

                string key = site.GetString(ATT.NAMN);
                if (string.IsNullOrEmpty(key)) return string.Empty;

                string code;
                if (Cache.TryGetValue(key, out code)) return code;

                code = key.Length <= 3
                     ? key.ToUpperInvariant()
                     : key.Substring(0, 3).ToUpperInvariant();

                Cache[key] = code;
                return code;
            }
            catch (Exception)
            {
                // An exception escaping a handler crosses back into native
                // code. Always return a default instead.
                return string.Empty;
            }
        }
    }
}
