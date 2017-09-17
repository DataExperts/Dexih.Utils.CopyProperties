using System;
using System.Collections.Generic;
using System.Text;

namespace Dexih.Utils.CopyProperties
{
    /// <summary>
    /// Indicates a property is the "key" value in a collection.  When merging collections
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CopyCollectionKeyAttribute : Attribute
    {
        public object DefaultKeyValue { get;set;}
        public bool ResetNegativeKeys { get; set; }
        /// <summary>
        /// Property is a key value in the collection.  The CopyProperties will use this to lookup values in collections and modify (rather than add) to them.
        /// </summary>
        public CopyCollectionKeyAttribute() { }

        /// <summary>
        /// <see cref="CollectionKeyAttribute"/>
        /// </summary>
        /// <param name="defaultKeyValue">Value which will be ignored as a key always added to the target collection.</param>
        /// <param name="resetNegativeKeys">If true, any key values less than 0 will be reset to the defaultKeyValue.</param>
        public CopyCollectionKeyAttribute(object defaultKeyValue, bool resetNegativeKeys = false)
        {
            DefaultKeyValue = defaultKeyValue;
            ResetNegativeKeys = resetNegativeKeys;
        }
    }

    /// <summary>
    /// Indicates the property should be treated as an IsValid property.
    /// If this is part of a record in a target collection, which is deleted, it will be set to false.
    /// Otherwise it will be set to true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CopyIsValidAttribute : Attribute
    {
    }

    /// <summary>
    /// The property value will be set to the key value of the parent record.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CopyParentCollectionKeyAttribute : Attribute
    {
    }

    /// <summary>
    /// Ignore this attribute completely.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CopyIgnoreAttribute : Attribute
    {
    }

    /// <summary>
    /// Directly copy the attribute reference.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CopyReference : Attribute
    {
    }

    /// <summary>
    /// Set the target value to null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CopySetNull : Attribute
    {
    }

    /// <summary>
    /// Only copies if the target element is null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CopyIfTargetNull : Attribute
    {
    }

    /// <summary>
    /// Only copies if the target element is not null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CopyIfTargetNotNull : Attribute
    {
    }
}
