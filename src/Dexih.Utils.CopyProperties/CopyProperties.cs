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
        public static Dictionary<string, PropertyElement> GetPropertyElement(Type sourceType, Type targetType)
        {
            var sourceProps = sourceType.GetProperties();
            var targetProps = targetType.GetProperties();

            // if this is a simple type, throw exception.
            if (IsSimpleType(sourceType))
            {
                // throw new CopyPropertiesSimpleTypeException(sourceType);
                return null;
            }

            Dictionary<string, PropertyElement> properties = new Dictionary<string, PropertyElement>();
            
            foreach (var srcProp in sourceProps)
            {
                var propertyElement = new PropertyElement();
                properties.Add(srcProp.Name, propertyElement);

                propertyElement.SourcePropertyInfo = srcProp;

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
                            propertyElement.CopyCollectionKey = true;
                            break;
                        case CopyReferenceAttribute a:
                            propertyElement.CopyReference = true;
                            break;
                    }
                }

                if(sourceType == targetType)
                {
                    propertyElement.TargetPropertyInfo = srcProp;
                    GetPropertyCollectionInfo(propertyElement, srcProp, srcProp);
                }
            }

            if (targetType != sourceType)
            {

                foreach (var targetProp in targetProps)
                {
                    var propertyElement = new PropertyElement();

                    if (properties.ContainsKey(targetProp.Name))
                    {
                        propertyElement = properties[targetProp.Name];
                    }
                    else
                    {
                        propertyElement = new PropertyElement();
                        properties.Add(targetProp.Name, propertyElement);
                    }

                    propertyElement.TargetPropertyInfo = targetProp;

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
                                propertyElement.CopyCollectionKey = true;
                                break;
                            case CopyReferenceAttribute a:
                                propertyElement.CopyReference = true;
                                break;
                        }
                    }

                    if (propertyElement.SourcePropertyInfo != null)
                    {
                        GetPropertyCollectionInfo(propertyElement, propertyElement.SourcePropertyInfo, targetProp);
                    }

                }
            }

            return properties;
        }

        private static void GetPropertyCollectionInfo(PropertyElement propertyElement, PropertyInfo sourceProp, PropertyInfo targetProp)
        {

            if(sourceProp.PropertyType == typeof(string))
            {
                propertyElement.IsSimpleType = true;
            }
            else if (typeof(IEnumerable).IsAssignableFrom(sourceProp.PropertyType))
            {
                Type sourceItemType = GetItemElementType(sourceProp.PropertyType);

                propertyElement.IsSourceEnumerable = true;

                if (targetProp.PropertyType.IsArray)
                {
                    propertyElement.IsTargetArray = true;
                    var targetItemType = targetProp.PropertyType.GetElementType();
                    propertyElement.ItemType = targetItemType;
                    propertyElement.ItemPropertyElements = GetPropertyElement(sourceItemType, targetItemType);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(targetProp.PropertyType))
                {
                    propertyElement.AddMethod = targetProp.PropertyType.GetMethod("Add");

                    if (propertyElement.AddMethod == null)
                    {
                        throw new CopyPropertiesInvalidCollectionException($"The target object contains a collection ${sourceProp.GetType().Name} which does not contain an \"Add\" method.  The copy properties can only function with collections such as List<> which have an \"Add\" method");
                    }

                    if (propertyElement.AddMethod.GetParameters().Length != 1)
                    {
                        throw new CopyPropertiesInvalidCollectionException($"The target object contains a collection ${sourceProp.GetType().Name} contains an \"Add\" method which has more than one parameter.  The copy properties can only function with collections such as List<> which have simple \"Add\" method with one parameter.");
                    }

                    var targetItemType = propertyElement.AddMethod.GetParameters()[0].ParameterType;
                    propertyElement.IsTargetCollection = true;
                    propertyElement.ItemType = targetItemType;
                    propertyElement.ItemPropertyElements = GetPropertyElement(sourceItemType, targetItemType);
                    propertyElement.ItemCollectionKey = propertyElement.ItemPropertyElements.Values.SingleOrDefault(c => c.CopyCollectionKey);
                    propertyElement.ItemIsValid = propertyElement.ItemPropertyElements.Values.SingleOrDefault(c => c.CopyIsValid);
                }
                else
                {
                    throw new CopyPropertiesInvalidCollectionException($"The source property {sourceProp.Name} is a collection, however the target {targetProp.Name} is not.");
                }
            }
            else if (IsSimpleType(sourceProp.PropertyType))
            {
                propertyElement.IsSimpleType = true;
            }
            else
            {
                propertyElement.ItemType = targetProp.PropertyType;
                propertyElement.ItemPropertyElements = GetPropertyElement(sourceProp.PropertyType, targetProp.PropertyType);
            }
        }

        public static void CopyProperties(this object source, object target, bool onlySimpleProperties = false)
        {
            // If source is null throw an exception
            if (source == null)
            {
                throw new CopyPropertiesNullException();
            }

            var srcType = source.GetType();
            var targetType = target.GetType();

            var properties = GetPropertyElement(srcType, targetType);

            if(properties == null)
            {
                throw new CopyPropertiesSimpleTypeException(srcType);
            }

            CopyProperties(source, target, properties, onlySimpleProperties, null);
        }

        /// <summary>
        /// Extension for 'Object' that copies matching properties from the source to destination object.
        /// If the onlySimpleProperties = false this will also make copies of child objects such as collections, and arrays.
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="target">The destination object</param>
        /// <param name="onlySimpleProperties">Indicates only simple values will be copied such as string, int, date etc.  This includes any properties that can be copied with a simple "=". </param>
        /// <param name="parentKeyValue">The destination object</param>
        public static void CopyProperties(this object source, object target, Dictionary<string, PropertyElement> properties, bool onlySimpleProperties = false, object parentKeyValue = null)
        { 
            // If source is null throw an exception
            if (source == null || properties == null)
            {
                throw new CopyPropertiesNullException();
            }


            // get the collectionKey value first
            object collectionKeyValue = null;
            foreach (var prop in properties.Values)
            {
                if (prop.CopyParentCollectionKey)
                {
                    collectionKeyValue = prop.SourcePropertyInfo.GetValue(source);
                    break;
                }
            }

            // loop through each property in the object.
            foreach (var prop in properties.Values)
            {
                try
                {
                    // no matching target property, then continue
                    if (prop.TargetPropertyInfo == null)
                    {
                        continue;
                    }

                    // can't read the source property.
                    if (!prop.SourcePropertyInfo.CanRead)
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

                    if (prop.CopyCollectionKey && prop.ResetNegativeKeys)
                    {
                        prop.TargetPropertyInfo.SetValueIfchanged(target, prop.DefaultKeyValue);
                        continue;
                    }

                    // do a normal copy
                    if (prop.CopyReference || prop.IsSimpleType)
                    {
                        prop.TargetPropertyInfo.SetValueIfchanged(target, prop.SourcePropertyInfo.GetValue(source));
                        continue;
                    }

                    if (!onlySimpleProperties)
                    {
                        if (prop.IsSourceEnumerable)
                        {
                            IEnumerable sourceCollection = prop.SourcePropertyInfo.GetValue(source, null) as IEnumerable;
                            IEnumerable targetCollection = prop.TargetPropertyInfo.GetValue(target, null) as IEnumerable;

                            // if there is no collection key in the target, or there are no items, then simply copy the collection/array over.
                            if (prop.ItemCollectionKey == null || targetCollection == null || !targetCollection.GetEnumerator().MoveNext())
                            {
                                if (prop.IsTargetArray)
                                {
                                    var targetArray = Array.CreateInstance(prop.ItemType, sourceCollection.Cast<object>().Count()) as Array;
                                    var i = 0;
                                    foreach (var item in sourceCollection)
                                    {
                                        if (prop.ItemPropertyElements == null)
                                        {
                                            targetArray.SetValue(item, i);
                                        }
                                        else
                                        {
                                            var targetItem = Activator.CreateInstance(prop.ItemType);
                                            item.CopyProperties(targetItem, prop.ItemPropertyElements, false, collectionKeyValue);
                                            targetArray.SetValue(targetItem, i);
                                            i++;
                                        }
                                    }

                                    prop.TargetPropertyInfo.SetValue(target, targetArray);
                                    continue;
                                }
                                else if (prop.IsTargetCollection)
                                {
                                    var newTargetCollection = Activator.CreateInstance(prop.TargetPropertyInfo.PropertyType) as IEnumerable;
                                    foreach (var item in sourceCollection)
                                    {
                                        if (prop.ItemPropertyElements == null)
                                        {
                                            prop.AddMethod.Invoke(newTargetCollection, new[] { item });
                                        }
                                        else
                                        {
                                            var targetItem = Activator.CreateInstance(prop.ItemType);
                                            item.CopyProperties(targetItem, prop.ItemPropertyElements, false, collectionKeyValue);
                                            prop.AddMethod.Invoke(newTargetCollection, new[] { targetItem });
                                        }
                                    }

                                    prop.TargetPropertyInfo.SetValue(target, newTargetCollection);
                                    continue;
                                }
                                else
                                {
                                    throw new CopyPropertiesInvalidCollectionException($"The source is a collection, howeve the equivalent target property is {prop.TargetPropertyInfo.PropertyType.Name}.");
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
                                    var targetItem = Activator.CreateInstance(prop.ItemType);
                                    var key = prop.ItemCollectionKey.TargetPropertyInfo.GetValue(item);
                                    indexedTargetCollection.Add(key, item);
                                }

                                // create a temporary indexed targetcollection, and merge all source items to it.
                                Dictionary<object, object> newIndexedTargetCollection = new Dictionary<object, object>();
                                foreach (var item in sourceCollection)
                                {
                                    var key = prop.ItemCollectionKey.TargetPropertyInfo.GetValue(item);
                                    object targetItem;
                                    if (indexedTargetCollection.ContainsKey(key))
                                    {
                                        targetItem = indexedTargetCollection[key];
                                    }
                                    else
                                    {
                                        targetItem = Activator.CreateInstance(prop.ItemType);
                                    }

                                    item.CopyProperties(targetItem, prop.ItemPropertyElements, false, collectionKeyValue);

                                    //set isvalid property to true.
                                    if (prop.ItemIsValid != null)
                                    {
                                        prop.ItemIsValid.TargetPropertyInfo.SetValue(item, true);
                                    }

                                    newIndexedTargetCollection.Add(key, targetItem);
                                }

                                //if there is an invalid property, copy any deleted items back into the target collection
                                // with the invalid property set to false.
                                if (prop.ItemIsValid != null)
                                {
                                    foreach (var item in indexedTargetCollection.Values)
                                    {
                                        var key = prop.ItemCollectionKey.TargetPropertyInfo.GetValue(item);
                                        if (!newIndexedTargetCollection.ContainsKey(key))
                                        {
                                            prop.ItemIsValid.TargetPropertyInfo.SetValue(item, false);
                                            newIndexedTargetCollection.Add(key, item);
                                        }
                                    }
                                }

                                if (prop.IsTargetArray)
                                {
                                    var targetArray = Array.CreateInstance(prop.ItemType, newIndexedTargetCollection.Count) as Array;
                                    var i = 0;
                                    foreach (var item in newIndexedTargetCollection)
                                    {
                                        targetArray.SetValue(item, i);
                                        i++;
                                    }

                                    prop.TargetPropertyInfo.SetValue(target, targetArray);
                                    continue;
                                }
                                else if (prop.IsTargetCollection)
                                {
                                    var newTargetCollection = Activator.CreateInstance(prop.TargetPropertyInfo.PropertyType) as IEnumerable;
                                    foreach (var item in newIndexedTargetCollection.Values)
                                    {
                                        prop.AddMethod.Invoke(newTargetCollection, new[] { item });
                                    }

                                    prop.TargetPropertyInfo.SetValue(target, newTargetCollection);
                                    continue;
                                }
                                else
                                {
                                    throw new CopyPropertiesInvalidCollectionException($"The source is a collection, howeve the equivalent target property is {prop.TargetPropertyInfo.PropertyType.Name}.");
                                }
                            }
                        }
                        else
                        {
                            var sourceValue = prop.SourcePropertyInfo.GetValue(source);
                            if(sourceValue == null)
                            {
                                prop.TargetPropertyInfo.SetValue(target, null);
                                continue;
                            }

                            var targetValue = prop.TargetPropertyInfo.GetValue(target);
                            if(targetValue == null)
                            {
                                targetValue = Activator.CreateInstance(prop.TargetPropertyInfo.PropertyType);
                                prop.TargetPropertyInfo.SetValue(target, targetValue);
                            }
                            sourceValue.CopyProperties(targetValue, prop.ItemPropertyElements, false);
                            continue;
                        }

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
