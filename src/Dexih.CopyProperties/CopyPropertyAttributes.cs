using System;
using System.Collections.Generic;
using System.Text;

namespace Dexih.CopyProperties
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CollectionKeyAttribute : Attribute
    {
        public object DefaultKeyValue { get;set;}
        public bool ResetNegativeKeys { get; set; }
        /// <summary>
        /// Property is a key value in the collection.  The CopyProperties will use this to lookup values in collections and modify (rather than add) to them.
        /// </summary>
        public CollectionKeyAttribute() { }

        /// <summary>
        /// <see cref="CollectionKeyAttribute"/>
        /// </summary>
        /// <param name="defaultKeyValue">Value which will be ignored as a key always added to the target collection.</param>
        /// <param name="resetNegativeKeys">If true, any key values less than 0 will be reset to the defaultKeyValue.</param>
        public CollectionKeyAttribute(object defaultKeyValue, bool resetNegativeKeys = false)
        {
            DefaultKeyValue = defaultKeyValue;
            ResetNegativeKeys = resetNegativeKeys;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IsValidAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ParentCollectionKeyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreCopyAttribute : Attribute
    {
    }
}
