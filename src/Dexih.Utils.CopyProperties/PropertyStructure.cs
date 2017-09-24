using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Dexih.Utils.CopyProperties
{
    public class PropertyElement
    {

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

        public bool IsSourceEnumerable { get; set; } = false;
        public bool IsTargetArray { get; set; } = false;
        public bool IsTargetCollection { get; set; } = false;
        public MethodInfo AddMethod { get; set; }
        public Type ItemType { get; set; }
        public Dictionary<string, PropertyElement> ItemPropertyElements { get; set; }
        public PropertyElement ItemCollectionKey { get; set; }
        public PropertyElement ItemIsValid { get; set; }
        public bool IsSimpleType { get; set; } = false;

    }
}
