using System;
using System.Collections;
using Aveva.Core.PMLNet;

namespace MyCompany.E3D.Tools
{
    /// <summary>
    /// Minimal PMLNetCallable class. Shows the four mandatory pieces:
    /// assembly attribute (AssemblyInfo.cs), class attribute, constructor, Assign.
    ///
    /// From PML:
    ///     import |C:\AVEVA\Custom\MyCompany.E3D.Tools|
    ///     handle ANY
    ///     endhandle
    ///     using namespace |MyCompany.E3D.Tools|
    ///     !s = object NETSTRING(|abcde|)
    ///     q var !s.methods()
    /// </summary>
    [PMLNetCallable()]
    public class NetString
    {
        private string mValue;

        [PMLNetCallable()]
        public NetString()
        {
            mValue = string.Empty;
        }

        [PMLNetCallable()]
        public NetString(string value)
        {
            mValue = value ?? string.Empty;
        }

        /// <summary>
        /// Called by PML for  !a = !b.  Mandatory, even when there is no state.
        /// </summary>
        [PMLNetCallable()]
        public void Assign(NetString that)
        {
            this.mValue = that.mValue;
        }

        /// <summary>A property becomes VAL() and VAL(STRING) in PML.</summary>
        [PMLNetCallable()]
        public string Val
        {
            get { return mValue; }
            set { mValue = value ?? string.Empty; }
        }

        [PMLNetCallable()]
        public string Append(string value)
        {
            mValue += value;
            return mValue;
        }

        /// <summary>Returns double, not int - PML REAL maps to System.Double.</summary>
        [PMLNetCallable()]
        public double Length()
        {
            return mValue.Length;
        }

        /// <summary>Hashtable becomes a PML ARRAY. Keys are doubles from 1.</summary>
        [PMLNetCallable()]
        public Hashtable Split(string separator)
        {
            Hashtable result = new Hashtable();

            if (string.IsNullOrEmpty(separator))
            {
                result.Add(1.0, mValue);
                return result;
            }

            string[] parts = mValue.Split(new[] { separator }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                result.Add((double)(i + 1), parts[i]);
            }
            return result;
        }

        /// <summary>
        /// Errors reach PML as PMLNetException, caught with  handle(1000, 1).
        /// </summary>
        [PMLNetCallable()]
        public double Real()
        {
            double parsed;
            if (!double.TryParse(mValue, out parsed))
            {
                throw new PMLNetException(1000, 1,
                    "'" + mValue + "' cannot be converted to a number");
            }
            return parsed;
        }

        /// <summary>No attribute: invisible to PML, usable from C#.</summary>
        public void Reset()
        {
            mValue = string.Empty;
        }
    }
}
