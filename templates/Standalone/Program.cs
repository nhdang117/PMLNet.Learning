using System;
using Aveva.Core.Database;
using Aveva.Core.Database.Filters;
using Aveva.Core.Utilities.Messaging;
using Aveva.E3D.Standalone;

namespace MyCompany.E3D.Reports
{
    /// <summary>
    /// A standalone AVEVA E3D tool: no E3D window, no user, no PML.
    ///
    /// Run it through run.bat, which sets E3D's environment first. Without
    /// that environment (PATH in particular) Standalone.Start() returns false
    /// and says nothing useful.
    ///
    /// Usage:
    ///     run.bat --project SAM --user SYSTEM --mdb SAMPLE --root /ZONE-01
    ///
    /// The password is read from the E3D_PASSWORD environment variable so it
    /// does not appear in the process list or in any log.
    /// </summary>
    internal static class Program
    {
        private const int ExitOk = 0;
        private const int ExitStartFailed = 1;
        private const int ExitLoginFailed = 2;
        private const int ExitError = 3;
        private const int ExitBadArguments = 4;

        private static int Main(string[] args)
        {
            Options options;
            if (!Options.TryParse(args, out options))
            {
                Console.Error.WriteLine(Options.Usage);
                return ExitBadArguments;
            }

            bool opened = false;

            try
            {
                if (!Standalone.Start())
                {
                    Console.Error.WriteLine(
                        "Failed to start the E3D core. Check that the E3D "
                        + "environment is set (run through run.bat) and that "
                        + "a licence is available.");
                    return ExitStartFailed;
                }

                PdmsMessage error;
                opened = Standalone.Open(options.Project,
                                         options.User,
                                         options.Password,
                                         options.Mdb,
                                         out error);
                if (!opened)
                {
                    Console.Error.WriteLine("Login failed: " + error.MessageText());
                    return ExitLoginFailed;
                }

                Report(options);
                return ExitOk;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return ExitError;
            }
            finally
            {
                // Always close, or the project stays locked.
                // Standalone.Close() is obsolete in E3D 3.1 - close the MDB
                // and the project explicitly instead.
                if (opened)
                {
                    MDB.CurrentMDB.CloseMDB();
                    Project.CurrentProject.Close();
                }
            }
        }

        /// <summary>
        /// Read-only work. Everything below here is the same API as in-process
        /// code, except that there is no current element to start from.
        /// </summary>
        private static void Report(Options options)
        {
            MDB mdb = MDB.CurrentMDB;
            Console.Error.WriteLine("MDB: " + mdb.Name);

            DbElement root = string.IsNullOrEmpty(options.Root)
                ? mdb.GetFirstWorld(DbType.Design)
                : DbElement.GetElement(options.Root);

            if (!root.IsValid)
            {
                Console.Error.WriteLine("Root element not found: " + options.Root);
                return;
            }

            TypeFilter equipment = new TypeFilter(DbElementTypeInstance.EQUIPMENT);
            DBElementCollection collection = new DBElementCollection(root, equipment);

            Console.WriteLine("Name\tDescription\tPosition");

            int count = 0;
            foreach (DbElement element in collection)
            {
                Console.WriteLine("{0}\t{1}\t{2}",
                    element.GetAsString(DbAttributeInstance.FLNM),
                    element.GetString(DbAttributeInstance.DESC),
                    element.GetAsString(DbAttributeInstance.POS));
                count++;
            }

            Console.Error.WriteLine(count + " elements reported");
        }

        private sealed class Options
        {
            public const string Usage =
                "Usage: MyCompany.E3D.Reports --project <code> --user <name> "
                + "--mdb <name> [--root <element>]\n"
                + "The password is read from the E3D_PASSWORD environment variable.";

            public string Project { get; private set; }
            public string User { get; private set; }
            public string Mdb { get; private set; }
            public string Root { get; private set; }
            public string Password { get; private set; }

            public static bool TryParse(string[] args, out Options options)
            {
                options = new Options();

                for (int i = 0; i < args.Length - 1; i += 2)
                {
                    switch (args[i].ToLowerInvariant())
                    {
                        case "--project": options.Project = args[i + 1]; break;
                        case "--user":    options.User = args[i + 1]; break;
                        case "--mdb":     options.Mdb = args[i + 1]; break;
                        case "--root":    options.Root = args[i + 1]; break;
                        default: return false;
                    }
                }

                options.Password = Environment.GetEnvironmentVariable("E3D_PASSWORD");

                return !string.IsNullOrEmpty(options.Project)
                    && !string.IsNullOrEmpty(options.User)
                    && !string.IsNullOrEmpty(options.Mdb)
                    && !string.IsNullOrEmpty(options.Password);
            }
        }
    }
}
