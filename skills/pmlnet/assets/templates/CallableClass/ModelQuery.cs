using System;
using System.Collections;
using Aveva.Core.Database;
using Aveva.Core.Database.Filters;
using Aveva.Core.PMLNet;
using Aveva.Core.Utilities.Messaging;

namespace MyCompany.E3D.Tools
{
    /// <summary>
    /// A PMLNetCallable class that reads the model. Shows the boundary rules:
    /// only REAL / STRING / BOOLEAN / ARRAY cross into PML, so elements are
    /// passed by name and database failures are translated to PMLNetException.
    ///
    /// From PML:
    ///     !q = object MODELQUERY()
    ///     !names = !q.membersOfType(|/ZONE-01|, |NOZZ|)
    ///     q var !names
    /// </summary>
    [PMLNetCallable()]
    public class ModelQuery
    {
        private const int ModuleNumber = 1000;

        [PMLNetCallable()]
        public ModelQuery()
        {
        }

        [PMLNetCallable()]
        public void Assign(ModelQuery that)
        {
            // No state to copy.
        }

        /// <summary>
        /// Names of every element of the given type below the named root.
        /// </summary>
        [PMLNetCallable()]
        public Hashtable MembersOfType(string rootName, string typeName)
        {
            DbElement root = GetElementOrThrow(rootName);

            DbElementType type = DbElementType.GetElementType(typeName);
            if (type == null || !type.IsValid)
            {
                throw new PMLNetException(ModuleNumber, 2,
                    "'" + typeName + "' is not an element type");
            }

            Hashtable result = new Hashtable();
            double index = 1;

            DBElementCollection collection =
                new DBElementCollection(root, new TypeFilter(type));

            foreach (DbElement element in collection)
            {
                result.Add(index++, element.GetAsString(DbAttributeInstance.FLNM));
            }

            return result;
        }

        /// <summary>Reads any attribute as a formatted string.</summary>
        [PMLNetCallable()]
        public string AttributeAsString(string elementName, string attributeName)
        {
            DbElement element = GetElementOrThrow(elementName);

            DbAttribute attribute = DbAttribute.GetDbAttribute(attributeName);
            if (attribute == null)
            {
                throw new PMLNetException(ModuleNumber, 3,
                    "'" + attributeName + "' is not defined in this project");
            }

            try
            {
                return element.GetAsString(attribute);
            }
            catch (PdmsException ex)
            {
                throw new PMLNetException(ModuleNumber, 4, ex.Message);
            }
        }

        /// <summary>Counts matches without materialising the whole list.</summary>
        [PMLNetCallable()]
        public double CountOfType(string rootName, string typeName)
        {
            DbElement root = GetElementOrThrow(rootName);

            DbElementType type = DbElementType.GetElementType(typeName);
            if (type == null || !type.IsValid)
            {
                throw new PMLNetException(ModuleNumber, 2,
                    "'" + typeName + "' is not an element type");
            }

            double count = 0;
            DBElementCollection collection =
                new DBElementCollection(root, new TypeFilter(type));

            foreach (DbElement element in collection)
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// GetElement never returns null and never throws for a missing name -
        /// it returns an invalid element, so IsValid must be checked here.
        /// </summary>
        private static DbElement GetElementOrThrow(string name)
        {
            DbElement element = DbElement.GetElement(name);
            if (!element.IsValid)
            {
                throw new PMLNetException(ModuleNumber, 1,
                    "'" + name + "' was not found");
            }
            return element;
        }
    }
}
