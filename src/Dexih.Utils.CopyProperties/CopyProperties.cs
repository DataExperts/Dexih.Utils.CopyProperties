using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Dexih.Utils.CopyProperties
{
    /// <summary>
    /// A static class for reflection type functions
    /// </summary>
    public static class Reflection
    {
        /// <summary>
        /// Extension for 'Object' that copies matching properties from the source to destination object.
        /// If the onlySimpleProperties = false this will also make copies of child objects such as collections, and arrays.
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="target">The destination object</param>
        /// <param name="onlySimpleProperties">Indicates only simple values will be copied such as string, int, date etc.  This includes any properties that can be copied with a simple "=". </param>
        /// <param name="parentKeyValue">The destination object</param>
        public static PropertyStructure GetPropertyStructure(Type sourceType, Type targetType = null)
        {
            if(targetType == null)
            {
                targetType = sourceType;
            }

            var propertyStructure = new PropertyStructure();
            propertyStructure.IsSimpleType = IsSimpleType(sourceType);
            propertyStructure.SourceType = sourceType;
            propertyStructure.TargetType = targetType;

            // if this is a simple type, we're done.
            if (propertyStructure.IsSimpleType)
            {
                return propertyStructure;
            }

            // if the structure is a collection, or array
            if (typeof(IEnumerable).IsAssignableFrom(sourceType))
            {
                Type sourceItemType = GetItemElementType(sourceType);

                propertyStructure.IsSourceEnumerable = true;

                if (targetType.IsArray)
                {
                    propertyStructure.IsTargetArray = true;
                    var targetItemType = targetType.GetElementType();
                    propertyStructure.ItemStructure = GetPropertyStructure(sourceItemType, targetItemType);
                    if (propertyStructure.ItemStructure.PropertyElements != null)
                    {
                        propertyStructure.ItemCollectionKey = propertyStructure.ItemStructure.PropertyElements.Values.SingleOrDefault(c => c.CopyCollectionKey);
                        propertyStructure.ItemIsValid = propertyStructure.ItemStructure.PropertyElements.Values.SingleOrDefault(c => c.CopyIsValid);
                    }
                }
                else if (typeof(IEnumerable).IsAssignableFrom(targetType))
                {
                    propertyStructure.AddMethod = targetType.GetMethod(nameof(ICollection<object>.Add));
                    propertyStructure.RemoveMethod = targetType.GetMethod(nameof(ICollection<object>.Remove));
                    var targetItemType = propertyStructure.AddMethod.GetParameters()[0].ParameterType;
                    propertyStructure.IsTargetCollection = true;
                    propertyStructure.ItemStructure = GetPropertyStructure(sourceItemType, targetItemType);
                    if (propertyStructure.ItemStructure.PropertyElements != null)
                    {
                        propertyStructure.ItemCollectionKey = propertyStructure.ItemStructure.PropertyElements.Values.SingleOrDefault(c => c.CopyCollectionKey);
                        propertyStructure.ItemIsValid = propertyStructure.ItemStructure.PropertyElements.Values.SingleOrDefault(c => c.CopyIsValid);
                    }
                }
                else
                {
                    throw new CopyPropertiesInvalidCollectionException($"The source property {sourceType.Name} is a collection, however the target {targetType.Name} is not.");
                }
            }

            // add any properties to the structure.
            var sourceProps = sourceType.GetProperties();

            foreach (var srcProp in sourceProps)
            {
                // if this is an indexed property (such as Item in a list), then skip.
                if(srcProp.GetIndexParameters().Count() > 0)
                {
                    continue;
                }

                if(targetType.IsArray)
                {
                    switch (srcProp.Name)
                    {
                        case "IsFixedSize":
                        case "IsReadOnly":
                        case "IsSynchronized":
                        case "Length":
                        case "LongLength":
                        case "Rank":
                        case "SyncRoot":
                            continue;
                    }
                }

                if (propertyStructure.IsTargetCollection)
                {
                    switch (srcProp.Name)
                    {
                        case "Count":
                        case "Capacity":
                            continue;
                    }
                }

                var propertyElement = new PropertyElement();
                propertyStructure.PropertyElements.Add(srcProp.Name, propertyElement);

                var attrbitues = srcProp.GetCustomAttributes();

                foreach(var attrib in attrbitues)
                {
                    switch(attrib)
                    {
                        case CopyCollectionKeyAttribute a:
                            propertyElement.CopyCollectionKey = true;
                            propertyElement.ResetNegativeKeys = a.ResetNegativeKeys;
                            propertyElement.DefaultKeyValue = a.DefaultKeyValue;
                            break;
                        case CopySetNullAttribute a:
                            propertyElement.CopySetNull = true;
                            break;
                        case CopyIfTargetDefaultAttribute a:
                            propertyElement.CopyIfTargetDefault = true;
                            propertyElement.DefaultValue = Activator.CreateInstance(srcProp.PropertyType);
                            break;
                        case CopyIfTargetNotDefaultAttribute a:
                            propertyElement.CopyIfTargetNotDefault = true;
                            propertyElement.DefaultValue = Activator.CreateInstance(srcProp.PropertyType);
                            break;
                        case CopyIfTargetNotNullAttribute a:
                            propertyElement.CopyIfTargetNotNull = true;
                            break;
                        case CopyIfTargetNullAttribute a:
                            propertyElement.CopyIfTargetNull = true;
                            break;
                        case CopyIgnoreAttribute a:
                            propertyElement.CopyIgnore = true;
                            break;
                        case CopyIsValidAttribute a:
                            propertyElement.CopyIsValid = true;
                            break;
                        case CopyParentCollectionKeyAttribute a:
                            propertyElement.CopyParentCollectionKey = true;
                            break;
                        case CopyReferenceAttribute a:
                            propertyElement.CopyReference = true;
                            break;
                    }
                }

                propertyElement.SourcePropertyInfo = srcProp;
                if (sourceType == targetType)
                {
                    propertyElement.TargetPropertyInfo = srcProp;
                    if (!propertyElement.CopyIgnore)
                    {
                        propertyElement.PropertyStructure = GetPropertyStructure(srcProp.PropertyType, srcProp.PropertyType);
                    }
                }
            }

            if (targetType != sourceType)
            {
                var targetProps = targetType.GetProperties();

                foreach (var targetProp in targetProps)
                {
                    var propertyElement = new PropertyElement();

                    if (propertyStructure.PropertyElements.ContainsKey(targetProp.Name))
                    {
                        propertyElement = propertyStructure.PropertyElements[targetProp.Name];
                    }
                    else
                    {
                        propertyElement = new PropertyElement();
                        propertyStructure.PropertyElements.Add(targetProp.Name, propertyElement);
                    }

                    var attrbitues = targetProp.GetCustomAttributes();

                    foreach (var attrib in attrbitues)
                    {
                        switch (attrib)
                        {
                            case CopyCollectionKeyAttribute a:
                                propertyElement.CopyCollectionKey = true;
                                propertyElement.ResetNegativeKeys = a.ResetNegativeKeys;
                                propertyElement.DefaultKeyValue = a.DefaultKeyValue;
                                break;
                            case CopySetNullAttribute a:
                                propertyElement.CopySetNull = true;
                                break;
                            case CopyIfTargetDefaultAttribute a:
                                propertyElement.CopyIfTargetDefault = true;
                                propertyElement.DefaultValue = Activator.CreateInstance(targetProp.PropertyType);
                                break;
                            case CopyIfTargetNotDefaultAttribute a:
                                propertyElement.CopyIfTargetNotDefault = true;
                                propertyElement.DefaultValue = Activator.CreateInstance(targetProp.PropertyType);
                                break;
                            case CopyIfTargetNotNullAttribute a:
                                propertyElement.CopyIfTargetNotNull = true;
                                break;
                            case CopyIfTargetNullAttribute a:
                                propertyElement.CopyIfTargetNull = true;
                                break;
                            case CopyIgnoreAttribute a:
                                propertyElement.CopyIgnore = true;
                                break;
                            case CopyIsValidAttribute a:
                                propertyElement.CopyIsValid = true;
                                break;
                            case CopyParentCollectionKeyAttribute a:
                                propertyElement.CopyParentCollectionKey = true;
                                break;
                            case CopyReferenceAttribute a:
                                propertyElement.CopyReference = true;
                                break;
                        }
                    }

                    propertyElement.TargetPropertyInfo = targetProp;


                    if (!propertyElement.CopyIgnore && propertyElement.SourcePropertyInfo != null)
                    {
                        propertyElement.PropertyStructure = GetPropertyStructure(propertyElement.SourcePropertyInfo.PropertyType, targetProp.PropertyType);
                    }

                }
            }

            return propertyStructure;
        }

        public static T CloneProperties<T>(this object source, bool shallowCopy = false)
        {
            var target = Activator.CreateInstance(typeof(T));
            source.CopyProperties(ref target, shallowCopy);
            return (T)target;
        }

        /// <summary>
        /// Clone the properties from the source object.
        /// Note: this will only copy object proerties (i.e declared with get/set).
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="shallowCopy">Set true to performa a shallow copy, otherwise will perform a deep copy.</param>
        /// <returns></returns>
        public static object CloneProperties(this object source, bool shallowCopy = false)
        {
            // If source is null throw an exception
            if (source == null)
            {
                throw new CopyPropertiesNullException();
            }

            var srcType = source.GetType();
            var properties = GetPropertyStructure(srcType, srcType);
            object target = null;

            CopyProperties(source, ref target, properties, shallowCopy, null);

            return target;
        }

        public static void CopyProperties(this object source, ref object target, bool shallowCopy = false)
        {
            // If source is null throw an exception
            if (source == null)
            {
                throw new CopyPropertiesNullException();
            }

            var srcType = source.GetType();
            Type targetType = null;
            if (target != null)
            {
                targetType = target.GetType();
            }

            var properties = GetPropertyStructure(srcType, targetType);

            if (properties.IsSimpleType)
            {
                throw new CopyPropertiesSimpleTypeException(srcType);
            }

            CopyProperties(source, ref target, properties, shallowCopy, null);
        }

        /// <summary>
        /// Performance a copy/merge between two objects.  
        /// Note: this will only copy object proerties (i.e declared with get/set).
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="target">The target object</param>
        /// <param name="shallowCopy">Set true to performa a shallow copy, otherwise will perform a deep copy.</param>
        public static void CopyProperties(this object source, object target, bool shallowCopy = false)
        {
            var originalTarget = target;

            CopyProperties(source, ref target, shallowCopy);

            if(!Object.ReferenceEquals(originalTarget, target))
            {
                throw new CopyPropertiesTargetInstanceException();
            }
        }

        /// <summary>
        /// Extension for 'Object' that copies matching properties from the source to destination object.
        /// If the onlySimpleProperties = false this will also make copies of child objects such as collections, and arrays.
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="target">The destination object</param>
        /// <param name="shallowCopy">Indicates only simple values will be copied such as string, int, date etc.  This includes any properties that can be copied with a simple "=". </param>
        /// <param name="parentKeyValue">The destination object</param>
        public static void CopyProperties(this object source, ref object target, PropertyStructure propertyStructure, bool shallowCopy = false, object parentKeyValue = null)
        { 
            // If source is null throw an exception
            if (source == null || propertyStructure == null)
            {
                throw new CopyPropertiesNullException();
            }

            if(propertyStructure.IsSimpleType)
            {
                throw new CopyPropertiesSimpleTypeException(propertyStructure.TargetType);
            }

            // if there is a collectionKey, then store it for providing as the parentkey for recursive calls.
            object collectionKeyValue = parentKeyValue;
            foreach(var prop in propertyStructure.PropertyElements.Values.Where(c=>c.CopyCollectionKey))
            {
                if (prop.SourcePropertyInfo != null)
                {
                    collectionKeyValue = prop.SourcePropertyInfo.GetValue(source);
                }
                else if(prop.TargetPropertyInfo != null)
                {
                    collectionKeyValue = prop.TargetPropertyInfo.GetValue(target);
                }
            }

            //Create the target structure
            // if there is no collection key in the target, or there are no items, then simply copy the collection/array over.
            if (shallowCopy == false)
            {
                if (propertyStructure.IsSourceEnumerable)
                {
                    IEnumerable sourceCollection = source as IEnumerable;
                    IEnumerable targetCollection = target as IEnumerable;
                    if (propertyStructure.ItemCollectionKey == null || targetCollection == null || !targetCollection.GetEnumerator().MoveNext())
                    {
                        if (propertyStructure.IsTargetArray)
                        {
                            Array targetArray = (Array)targetCollection;
                            var count = sourceCollection.Cast<object>().Count();
                            if (targetArray == null || targetArray.Length != sourceCollection.Cast<object>().Count())
                            {
                                targetArray = Array.CreateInstance(propertyStructure.ItemStructure.TargetType, count) as Array;
                            }

                            var i = 0;
                            foreach (var item in sourceCollection)
                            {
                                if (propertyStructure.ItemStructure.IsSimpleType)
                                {
                                    targetArray.SetValue(item, i);
                                }
                                else
                                {
                                    var targetItem = Activator.CreateInstance(propertyStructure.ItemStructure.TargetType);
                                    item.CopyProperties(ref targetItem, propertyStructure.ItemStructure, false, collectionKeyValue);
                                    targetArray.SetValue(targetItem, i);
                                }
                                i++;
                            }

                            target = targetArray;
                        }
                        else if (propertyStructure.IsTargetCollection)
                        {
                            IEnumerable newTargetCollection = (IEnumerable) targetCollection;
                            if (newTargetCollection == null)
                            {
                                newTargetCollection = Activator.CreateInstance(propertyStructure.TargetType) as IEnumerable;
                            }
                            foreach (var item in sourceCollection)
                            {
                                if (propertyStructure.ItemStructure.IsSimpleType)
                                {
                                    propertyStructure.AddMethod.Invoke(newTargetCollection, new[] { item });
                                }
                                else
                                {
                                    var targetItem = Activator.CreateInstance(propertyStructure.ItemStructure.TargetType);
                                    item.CopyProperties(ref targetItem, propertyStructure.ItemStructure, false, collectionKeyValue);
                                    propertyStructure.AddMethod.Invoke(newTargetCollection, new[] { targetItem });
                                }
                            }

                            target = newTargetCollection;
                        }
                        else
                        {
                            throw new CopyPropertiesInvalidCollectionException($"The source is a collection, howeve the equivalent target property is {propertyStructure.TargetType.Name}.");
                        }
                    }
                    else
                    {
                        // if there is a collectionKey, then attempt a delta.

                        // create a dictionary, with the key as index, and copy target items to it.
                        Dictionary<object, object> indexedTargetCollection = new Dictionary<object, object>();
                        targetCollection.GetEnumerator().Reset();
                        foreach (var item in targetCollection)
                        {
                            var key = propertyStructure.ItemCollectionKey.TargetPropertyInfo.GetValue(item);
                            indexedTargetCollection.Add(key, item);
                        }

                        // create a temporary indexed targetcollection, and merge all source items to it.
                        Dictionary<object, object> newIndexedTargetCollection = new Dictionary<object, object>();
                        foreach (var item in sourceCollection)
                        {
                            var key = propertyStructure.ItemCollectionKey.TargetPropertyInfo.GetValue(item);
                            object targetItem;
                            if (indexedTargetCollection.ContainsKey(key))
                            {
                                targetItem = indexedTargetCollection[key];
                            }
                            else
                            {
                                targetItem = Activator.CreateInstance(propertyStructure.ItemStructure.TargetType);
                            }

                            item.CopyProperties(ref targetItem, propertyStructure.ItemStructure, false, collectionKeyValue);

                            //set isvalid property to true.
                            if (propertyStructure.ItemIsValid != null)
                            {
                                propertyStructure.ItemIsValid.TargetPropertyInfo.SetValue(item, true);
                            }

                            newIndexedTargetCollection.Add(key, targetItem);
                        }

                        //if there is an invalid property, copy any deleted items back into the target collection
                        // with the invalid property set to false.
                        if (propertyStructure.ItemIsValid != null)
                        {
                            foreach (var item in indexedTargetCollection.Values)
                            {
                                var key = propertyStructure.ItemCollectionKey.TargetPropertyInfo.GetValue(item);
                                if (!newIndexedTargetCollection.ContainsKey(key))
                                {
                                    propertyStructure.ItemIsValid.TargetPropertyInfo.SetValue(item, false);
                                    newIndexedTargetCollection.Add(key, item);
                                }
                            }
                        }

                        if (propertyStructure.IsTargetArray)
                        {
                            Array targetArray = (Array)targetCollection;
                            if (targetArray.Length != newIndexedTargetCollection.Count)
                            {
                                targetArray = Array.CreateInstance(propertyStructure.ItemStructure.TargetType, newIndexedTargetCollection.Count) as Array;
                            }

                            var i = 0;
                            foreach (var item in newIndexedTargetCollection.Values)
                            {
                                targetArray.SetValue(item, i);
                                i++;
                            }

                            target = targetArray;
                        }
                        else if (propertyStructure.IsTargetCollection)
                        {
                            var newTargetCollection = (IEnumerable)targetCollection;
                            if (newTargetCollection == null || propertyStructure.RemoveMethod == null)
                            {
                                newTargetCollection = Activator.CreateInstance(propertyStructure.TargetType) as IEnumerable;
                            }

                            // create a  list of items to remove 
                            var removeItems = new List<object>();
                            foreach (var item in newTargetCollection)
                            {
                                var key = propertyStructure.ItemCollectionKey.TargetPropertyInfo.GetValue(item);
                                if(!newIndexedTargetCollection.ContainsKey(key))
                                {
                                    removeItems.Add(item);
                                }

                                newIndexedTargetCollection.Remove(key);
                            }

                            // remove the items from the target list
                            foreach(var item in removeItems)
                            {
                                propertyStructure.RemoveMethod.Invoke(newTargetCollection, new[] { item });
                            }

                            // finally add remaining items to the target collection.
                            foreach (var item in newIndexedTargetCollection.Values)
                            {
                                propertyStructure.AddMethod.Invoke(newTargetCollection, new[] { item });
                            }

                            target = newTargetCollection;
                        }
                        else
                        {
                            throw new CopyPropertiesInvalidCollectionException($"The source is a collection, however the equivalent target property is {propertyStructure.TargetType.Name}.");
                        }
                    }
                }
            }

            if(target == null)
            {
                target = Activator.CreateInstance(propertyStructure.TargetType);
            }


            // loop through each property in the object.
            foreach (var prop in propertyStructure.PropertyElements.Values)
            {
                try
                {
                    // no matching target property, then continue
                    if (prop.TargetPropertyInfo == null)
                    {
                        continue;
                    }

                    // can't write to the target property.
                    if (!prop.TargetPropertyInfo.CanWrite)
                    {
                        continue;
                    }

                    // ignore attribute on the source property, then skip
                    if (prop.CopyIgnore)
                    {
                        continue;
                    }

                    // set the target value to null
                    if (prop.CopySetNull)
                    {
                        prop.TargetPropertyInfo.SetValueIfchanged(target, null);
                        continue;
                    }

                    if (prop.CopyIfTargetNull)
                    {
                        // if target property is not null, the ignore and continue.
                        if (prop.TargetPropertyInfo.GetValue(target) != null)
                        {
                            continue;
                        }
                    }

                    if (prop.CopyIfTargetNotNull)
                    {
                        // if target value is null, then ignore and continue.
                        if (prop.TargetPropertyInfo.GetValue(target) == null)
                        {
                            continue;
                        }
                    }

                    if (prop.CopyIfTargetDefault)
                    {
                        // if target property is not null, the ignore and continue.
                        if (prop.TargetPropertyInfo.GetValue(target) != prop.DefaultValue)
                        {
                            continue;
                        }
                    }

                    if (prop.CopyIfTargetNotDefault)
                    {
                        // if target value is null, then ignore and continue.
                        if (prop.TargetPropertyInfo.GetValue(target) == prop.DefaultValue)
                        {
                            continue;
                        }
                    }

                    if(prop.CopyParentCollectionKey)
                    {
                        prop.TargetPropertyInfo.SetValueIfchanged(target, parentKeyValue);
                        continue;
                    }

                    if (prop.CopyIsValid)
                    {
                        prop.TargetPropertyInfo.SetValueIfchanged(target, true);
                        continue;
                    }

                    // if this is a key, and reset negative number is true, then set this value to 0.
                    if (prop.SourcePropertyInfo != null && prop.CopyCollectionKey && prop.ResetNegativeKeys)
                    {
                        if (Convert.ToDouble(prop.SourcePropertyInfo.GetValue(source)) < 0)
                        {
                            prop.TargetPropertyInfo.SetValueIfchanged(target, prop.DefaultKeyValue);
                            continue;
                        }
                    }

                    // can't read the source property.
                    if (prop.SourcePropertyInfo == null || !prop.SourcePropertyInfo.CanRead)
                    {
                        continue;
                    }
                   
                    // do a normal copy
                    if (prop.CopyReference || prop.PropertyStructure.IsSimpleType)
                    {
                        prop.TargetPropertyInfo.SetValueIfchanged(target, prop.SourcePropertyInfo.GetValue(source));
                        continue;
                    }

                    if (!shallowCopy)
                    {
                        var sourceValue = prop.SourcePropertyInfo.GetValue(source);
                        var targetValue = prop.TargetPropertyInfo.GetValue(target);

                        if (sourceValue == null)
                        {
                            prop.TargetPropertyInfo.SetValue(target, null);
                            continue;
                        }

                        if (targetValue == null)
                        {
                            sourceValue.CopyProperties(ref targetValue, prop.PropertyStructure, false, collectionKeyValue);
                            prop.TargetPropertyInfo.SetValue(target, targetValue);
                        }
                        else
                        {
                            sourceValue.CopyProperties(ref targetValue, prop.PropertyStructure, false, collectionKeyValue);
                        }

                        continue;
                    }

                    // throw new CopyPropertiesException($"CopyProperties failed in property {source.GetType().Name}.  Unknown error.");

                }
                catch (Exception ex)
                {
                    throw new CopyPropertiesException($"CopyProperties failed in property {source?.GetType().Name}.  {ex.Message}.", ex);
                }
            }
        }


             public static bool IsSimpleType(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return
                typeInfo.IsPrimitive ||
                typeInfo.IsEnum ||
                new[] {
                    typeof(Enum),
                    typeof(string),
                    typeof(decimal),
                    typeof(DateTime),
                    typeof(DateTimeOffset),
                    typeof(TimeSpan),
                    typeof(Guid)
                }.Contains(type) ||
                typeInfo.BaseType == typeof(Enum) ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                (typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]))
                ;
        }

        // 
        // found at http://stackoverflow.com/questions/3569811/how-to-know-if-a-propertyinfo-is-a-collection
        //
        public static bool IsNonStringEnumerable(this PropertyInfo pi)
        {
            return pi != null && pi.PropertyType.IsNonStringEnumerable();
        }

        public static bool IsNonStringEnumerable(this object instance)
        {
            return instance != null && instance.GetType().IsNonStringEnumerable();
        }

        public static bool IsNonStringEnumerable(this Type type)
        {
            if (type == null || type == typeof(string))
                return false;
            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        public static void SetValueIfchanged(this PropertyInfo property, object obj, object value)
        {
            if (IsSimpleType(property.PropertyType))
            {
                if (!Equals(property.GetValue(obj), value) || (property.GetValue(obj) == null && value != null) || (property.GetValue(obj) != null && value == null))
                {
                    property.SetValue(obj, value);
                }
            }
            else
            {
                property.SetValue(obj, value);
            }

        }

        public static Type GetItemElementType(Type type)
        {
            // Type is Array
            // short-circuit if you expect lots of arrays 
            if (type.IsArray)
                return type.GetElementType();

            // type is IEnumerable<T>;
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            // type implements/extends IEnumerable<T>;
            var enumType = type.GetInterfaces()
                                    .Where(t => t.IsConstructedGenericType &&
                                           t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
            return enumType ?? type;
        }

    }
}
