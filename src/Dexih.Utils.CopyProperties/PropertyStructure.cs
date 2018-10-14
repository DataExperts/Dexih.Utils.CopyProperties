using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dexih.Utils.CopyProperties
{
    public class PropertyStructure
    {
        public Dictionary<string, PropertyElement> PropertyElements { get; set; } = new Dictionary<string, PropertyElement>();

        /// <summary>
        /// Indicates a simple type (such as string, int, etc).
        /// </summary>
        public bool IsSimpleType { get; set; } = false;

        public Type SourceType { get; set; }
        public Type TargetType { get; set;
        }
        public bool IsSourceEnumerable { get; set; } = false;

        public bool IsTargetArray { get; set; } = false;

        public bool IsTargetCollection { get; set; } = false;
        public MethodInfo AddMethod { get; set; }
        public MethodInfo RemoveMethod { get; set; }

        /// <summary>
        /// If the property is a collection, this is the structure for each item in the collection.
        /// </summary>
        public PropertyStructure ItemStructure { get; set; }

        /// <summary>
        /// A reference to the property set with the collectionKey attribute.
        /// </summary>
        public PropertyElement ItemCollectionKey { get; set; }

        /// <summary>
        /// A reference to the property set with the isValid attribute.
        /// </summary>
        public PropertyElement ItemIsValid { get; set; }

    }

    public class PropertyElement
    {
        public PropertyStructure PropertyStructure { get; set; }
        public PropertyInfo SourcePropertyInfo { get; set; }
        public PropertyInfo TargetPropertyInfo { get; set; }
        public bool CopyCollectionKey { get; set; } = false;
        public object DefaultKeyValue { get; set; }
        public bool ResetNegativeKeys { get; set; } = false;
        public bool CopyParentCollectionKey { get; set; } = false;
        public bool CopySetNull { get; set; } = false;
        public bool CopyIfTargetDefault { get; set; } = false;
        public bool CopyIfTargetNotDefault { get; set; } = false;
        public object DefaultValue { get; set; }

        public bool CopyIfTargetNotNull { get; set; } = false;
        public bool CopyIfTargetNull { get; set; } = false;
        public bool CopyIgnore { get; set; } = false;
        public bool CopyIsValid { get; set; } = false;
        public bool CopyReference { get; set; } = false;
    }
}
