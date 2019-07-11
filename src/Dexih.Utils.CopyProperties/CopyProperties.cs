using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dexih.Utils.CopyProperties
{
    /// <summary>
    /// A static class for reflection type functions
    /// </summary>
    public static class Reflection
    {
        private static readonly ConcurrentDictionary<(Type sourcetype, Type targetType), PropertyStructure> _cachePropertyStructures = new ConcurrentDictionary<(Type sourcetype, Type targetType), PropertyStructure>();

        public static PropertyStructure GetPropertyStructure(Type sourceType, Type targetType = null)
        {
            if(targetType == null)
            {
                targetType = sourceType;
            }

            var propertyStructure = _cachePropertyStructures.GetOrAdd((sourceType, targetType),
                type => BuildPropertyStructure(type.sourcetype, type.targetType, new Dictionary<(Type sourcetype, Type targetType), PropertyStructure>()));

            return propertyStructure;
        }


        /// <summary>
        /// Extension for 'Object' that copies matching properties from the source to destination object.
        /// If the onlySimpleProperties = false this will also make copies of child objects such as collections, and arrays.
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="targetType"></param>
        /// <param name="otherTypes"></param>
        private static PropertyStructure BuildPropertyStructure(Type sourceType, Type targetType, Dictionary<(Type sourcetype, Type targetType), PropertyStructure> otherTypes)
        {
            
            if(otherTypes.TryGetValue((sourceType, targetType), out var existingPropertyStructure))
            {
                return existingPropertyStructure;
            }

            var propertyStructure = new PropertyStructure
            {
                IsSimpleType = IsSimpleType(sourceType),
                SourceType = sourceType,
                TargetType = targetType
            };

            otherTypes.Add((sourceType, targetType), propertyStructure);

            // if this is a simple type, we're done.
            if (propertyStructure.IsSimpleType)
            {
                return propertyStructure;
            }

            // if the structure is a collection, or array
            if (typeof(IEnumerable).IsAssignableFrom(sourceType))
            {
                var sourceItemType = GetItemElementType(sourceType);

                propertyStructure.IsSourceEnumerable = true;

                if (targetType.IsArray)
                {
                    propertyStructure.IsTargetArray = true;
                    var targetItemType = targetType.GetElementType();
                    propertyStructure.ItemStructure = BuildPropertyStructure(sourceItemType, targetItemType, otherTypes);
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
                    if(propertyStructure.AddMethod == null) 
                    {
                        throw new CopyPropertiesInvalidCollectionException($"The target property {targetType.Name} is a collection, however no Add method could be found.");
                    }
                    var targetItemType = propertyStructure.AddMethod.GetParameters()[0].ParameterType;
                    propertyStructure.IsTargetCollection = true;
                    propertyStructure.ItemStructure = BuildPropertyStructure(sourceItemType, targetItemType, otherTypes);
                    if (propertyStructure.ItemStructure.PropertyElements != null)
                    {
                        propertyStructure.ItemCollectionKey = propertyStructure.ItemStructure.PropertyElements.Values.SingleOrDefault(c => c.CopyCollectionKey && c.SourcePropertyInfo != null && c.TargetPropertyInfo != null);
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

            PropertyInfo[] targetProps = null;
            
            if (targetType != sourceType)
            {
                targetProps = targetType.GetProperties();
            }


            foreach (var srcProp in sourceProps)
            {
                // if this is an indexed property (such as Item in a list), then skip.
                if(srcProp.GetIndexParameters().Any())
                {
                    continue;
                }

                // don't get the built in array properties.
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

                // don't get the built in collection properties.
                if (propertyStructure.IsTargetCollection)
                {
                    switch (srcProp.Name)
                    {
                        case "Count":
                        case "Capacity":
                        case "IsReadOnly":
                            continue;
                    }
                }

     
                var propertyElement = new PropertyElement();
                propertyStructure.PropertyElements.Add(srcProp.Name, propertyElement);

                var attributes = srcProp.GetCustomAttributes();

                foreach(var attrib in attributes)
                {
                    switch(attrib)
                    {
                        case CopyCollectionKeyAttribute a:
                            propertyElement.CopyCollectionKey = true;
                            propertyElement.ResetNegativeKeys = a.ResetNegativeKeys;
                            propertyElement.DefaultKeyValue = a.DefaultKeyValue;
                            break;
                        case CopySetNullAttribute _:
                            propertyElement.CopySetNull = true;
                            break;
                        case CopyIfTargetDefaultAttribute _:
                            propertyElement.CopyIfTargetDefault = true;
                            propertyElement.DefaultValue = Activator.CreateInstance(srcProp.PropertyType);
                            break;
                        case CopyIfTargetNotDefaultAttribute _:
                            propertyElement.CopyIfTargetNotDefault = true;
                            propertyElement.DefaultValue = Activator.CreateInstance(srcProp.PropertyType);
                            break;
                        case CopyIfTargetNotNullAttribute _:
                            propertyElement.CopyIfTargetNotNull = true;
                            break;
                        case CopyIfTargetNullAttribute _:
                            propertyElement.CopyIfTargetNull = true;
                            break;
                        case CopyIgnoreAttribute _:
                            propertyElement.CopyIgnore = true;
                            break;
                        case CopyIsValidAttribute _:
                            propertyElement.CopyIsValid = true;
                            break;
                        case CopyParentCollectionKeyAttribute attribute:
                            propertyElement.CopyParentCollectionKey = true;
                            propertyElement.CopyParentCollectionProperty = attribute.ParentProperty;
                            propertyElement.CopyParentCollectionEntity = attribute.ParentEntity;
                            break;
                        case CopyReferenceAttribute _:
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
                        propertyElement.PropertyStructure = BuildPropertyStructure(srcProp.PropertyType, srcProp.PropertyType, otherTypes);
                    }
                }
            }

            if (targetType != sourceType)
            {
                foreach (var targetProp in targetProps)
                {
                    PropertyElement propertyElement;

                    if (propertyStructure.PropertyElements.ContainsKey(targetProp.Name))
                    {
                        propertyElement = propertyStructure.PropertyElements[targetProp.Name];
                    }
                    else
                    {
                        propertyElement = new PropertyElement();
                        propertyStructure.PropertyElements.Add(targetProp.Name, propertyElement);
                    }

                    var attributes = targetProp.GetCustomAttributes();

                    foreach (var attrib in attributes)
                    {
                        switch (attrib)
                        {
                            case CopyCollectionKeyAttribute a:
                                propertyElement.CopyCollectionKey = true;
                                propertyElement.ResetNegativeKeys = a.ResetNegativeKeys;
                                propertyElement.DefaultKeyValue = a.DefaultKeyValue;
                                break;
                            case CopySetNullAttribute _:
                                propertyElement.CopySetNull = true;
                                break;
                            case CopyIfTargetDefaultAttribute _:
                                propertyElement.CopyIfTargetDefault = true;
                                propertyElement.DefaultValue = Activator.CreateInstance(targetProp.PropertyType);
                                break;
                            case CopyIfTargetNotDefaultAttribute _:
                                propertyElement.CopyIfTargetNotDefault = true;
                                propertyElement.DefaultValue = Activator.CreateInstance(targetProp.PropertyType);
                                break;
                            case CopyIfTargetNotNullAttribute _:
                                propertyElement.CopyIfTargetNotNull = true;
                                break;
                            case CopyIfTargetNullAttribute _:
                                propertyElement.CopyIfTargetNull = true;
                                break;
                            case CopyIgnoreAttribute _:
                                propertyElement.CopyIgnore = true;
                                break;
                            case CopyIsValidAttribute _:
                                propertyElement.CopyIsValid = true;
                                break;
                            case CopyParentCollectionKeyAttribute attribute:
                                propertyElement.CopyParentCollectionKey = true;
                                propertyElement.CopyParentCollectionProperty = attribute.ParentProperty;
                                propertyElement.CopyParentCollectionEntity = attribute.ParentEntity;
                                break;
                            case CopyReferenceAttribute _:
                                propertyElement.CopyReference = true;
                                break;
                        }
                    }

                    propertyElement.TargetPropertyInfo = targetProp;


                    if (!propertyElement.CopyIgnore && propertyElement.SourcePropertyInfo != null)
                    {
                        propertyElement.PropertyStructure = BuildPropertyStructure(propertyElement.SourcePropertyInfo.PropertyType, targetProp.PropertyType, otherTypes);
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
        /// Note: this will only copy object properties (i.e declared with get/set).
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

            CopyProperties(source, ref target, properties, shallowCopy, null, null, null);

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

            CopyProperties(source, ref target, properties, shallowCopy, null, null, null);
        }

        /// <summary>
        /// Performance a copy/merge between two objects.  
        /// Note: this will only copy object properties (i.e declared with get/set).
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="target">The target object</param>
        /// <param name="shallowCopy">Set true to performa a shallow copy, otherwise will perform a deep copy.</param>
        public static void CopyProperties(this object source, object target, bool shallowCopy = false)
        {
            var originalTarget = target;

            CopyProperties(source, ref target, shallowCopy);

            if(!ReferenceEquals(originalTarget, target))
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
        /// <param name="propertyStructure"></param>
        /// <param name="shallowCopy">Indicates only simple values will be copied such as string, int, date etc.  This includes any properties that can be copied with a simple "=". </param>
        /// <param name="parentPropertyInfo"></param>
        /// <param name="parentSource"></param>
        /// <param name="parentTarget"></param>
        public static void CopyProperties(this object source, ref object target, PropertyStructure propertyStructure, bool shallowCopy, PropertyStructure parentPropertyInfo, object parentSource, object parentTarget)
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


            PropertyStructure collectionParentPropertyInfo = parentPropertyInfo;
            object collectionParentSource = parentSource;
            object collectionParentTarget = parentTarget;
            
            if (propertyStructure.PropertyElements.Count > 0)
            {
                collectionParentSource = source;
                collectionParentTarget = target;
                collectionParentPropertyInfo = propertyStructure;
            }

            //Create the target structure
            // if there is no collection key in the target, or there are no items, then simply copy the collection/array over.
            if (shallowCopy == false)
            {
                if (propertyStructure.IsSourceEnumerable)
                {
                    var sourceCollection = source as IEnumerable;
                    var targetCollection = target as IEnumerable;
                    if (propertyStructure.ItemCollectionKey == null || targetCollection == null || !targetCollection.GetEnumerator().MoveNext())
                    {
                        if (propertyStructure.IsTargetArray)
                        {
                            var targetArray = (Array)targetCollection;
                            var count = sourceCollection.Cast<object>().Count();
                            if (targetArray == null || targetArray.Length != sourceCollection.Cast<object>().Count())
                            {
                                targetArray = Array.CreateInstance(propertyStructure.ItemStructure.TargetType, count);
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
                                    item.CopyProperties(ref targetItem, propertyStructure.ItemStructure, false, collectionParentPropertyInfo, collectionParentSource, collectionParentTarget);
                                    targetArray.SetValue(targetItem, i);
                                }
                                i++;
                            }

                            target = targetArray;
                        }
                        else if (propertyStructure.IsTargetCollection)
                        {
                            var newTargetCollection = targetCollection;
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
                                    item.CopyProperties(ref targetItem, propertyStructure.ItemStructure, false, collectionParentPropertyInfo, collectionParentSource, collectionParentTarget);
                                    propertyStructure.AddMethod.Invoke(newTargetCollection, new[] { targetItem });
                                }
                            }

                            target = newTargetCollection;
                        }
                        else
                        {
                            throw new CopyPropertiesInvalidCollectionException($"The source is a collection, however the equivalent target property is type {propertyStructure.TargetType.Name}.");
                        }
                    }
                    else
                    {
                        // if there is a collectionKey, then attempt a delta.

                        // create a dictionary, with the key as index, and copy target items to it.
                        var indexedTargetCollection = new Dictionary<object, object>();
                        targetCollection.GetEnumerator().Reset();
                        foreach (var item in targetCollection)
                        {
                            var key = propertyStructure.ItemCollectionKey.TargetPropertyInfo.GetValue(item);
                            if (key != null)
                            {
                                indexedTargetCollection.Add(key, item);
                            }
                        }

                        // create a temporary indexed target collection, and merge all source items to it.
                        var newIndexedTargetCollection = new Dictionary<object, object>();
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

                            item.CopyProperties(ref targetItem, propertyStructure.ItemStructure, false, collectionParentPropertyInfo, collectionParentSource, collectionParentTarget);

                            //set isValid property to true.
                            if (propertyStructure.ItemIsValid != null && propertyStructure.ItemIsValid.TargetPropertyInfo.GetValue(item) == null)
                            {
                                propertyStructure.ItemIsValid.TargetPropertyInfo.SetValue(item, true);
                            }

                            if(newIndexedTargetCollection.ContainsKey(key))
                            {
                                throw new CopyPropertiesException($"The copy collection failed as the source collect of {propertyStructure.ItemStructure.SourceType.Name} contained multiple items with the same key field {propertyStructure.ItemCollectionKey.TargetPropertyInfo.Name}.");
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
                            var targetArray = (Array)targetCollection;
                            if (targetArray.Length != newIndexedTargetCollection.Count)
                            {
                                targetArray = Array.CreateInstance(propertyStructure.ItemStructure.TargetType, newIndexedTargetCollection.Count);
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
                            var newTargetCollection = targetCollection;
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
                        prop.TargetPropertyInfo.SetValueIfChanged(target, null);
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

                    if(prop.CopyParentCollectionKey && parentPropertyInfo != null)
                    {
                        // if there is no named parent key, then use the default key.
                        if (string.IsNullOrEmpty(prop.CopyParentCollectionProperty) )
                        {
                            object parentKeyValue = null;
                            foreach(var parentProp in parentPropertyInfo.PropertyElements.Values.Where(c=>c.CopyCollectionKey))
                            {
                                if (parentProp.SourcePropertyInfo != null)
                                {
                                    parentKeyValue = parentProp.SourcePropertyInfo.GetValue(parentSource);
                                }
                                else if(parentProp.TargetPropertyInfo != null)
                                {
                                    parentKeyValue = parentProp.TargetPropertyInfo.GetValue(parentTarget);
                                }
                            }
                            prop.TargetPropertyInfo.SetValueIfChanged(target, parentKeyValue);
                        }
                        else
                        {
                            // if the parent collection key uses a named property, then use this.
                            if (source != null)
                            {
                                if (parentPropertyInfo.PropertyElements.TryGetValue(prop.CopyParentCollectionProperty, out var property))
                                {
                                    if (property.SourcePropertyInfo != null && (prop.CopyParentCollectionEntity == null ||
                                                                                property.SourcePropertyInfo.Name == prop.CopyParentCollectionEntity))
                                    {
                                        var value = property.SourcePropertyInfo.GetValue(parentSource);
                                        prop.TargetPropertyInfo.SetValueIfChanged(target, value);
                                    }
                                    else if (property.TargetPropertyInfo != null && (prop.CopyParentCollectionEntity == null ||
                                                                                     property.TargetPropertyInfo.Name == prop.CopyParentCollectionEntity))
                                    {
                                        var value = property.TargetPropertyInfo.GetValue(parentTarget);
                                        prop.TargetPropertyInfo.SetValueIfChanged(target, value);
                                    }
                                }
                            }
                        }

                        continue;
                    }

                    // if the target "isValid = null, then set to true"
                    if (prop.CopyIsValid && prop.TargetPropertyInfo.GetValue(target) == null)
                    {
                        prop.TargetPropertyInfo.SetValueIfChanged(target, true);
                        continue;
                    }

                    // if this is a key, and reset negative number is true, then set this value to 0.
                    if (prop.SourcePropertyInfo != null && prop.CopyCollectionKey && prop.ResetNegativeKeys)
                    {
                        if (Convert.ToDouble(prop.SourcePropertyInfo.GetValue(source)) < 0)
                        {
                            prop.TargetPropertyInfo.SetValueIfChanged(target, prop.DefaultKeyValue);
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
                        prop.TargetPropertyInfo.SetValueIfChanged(target, prop.SourcePropertyInfo.GetValue(source));
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
                            sourceValue.CopyProperties(ref targetValue, prop.PropertyStructure, false, collectionParentPropertyInfo, collectionParentSource, collectionParentTarget);
                            prop.TargetPropertyInfo.SetValue(target, targetValue);
                        }
                        else
                        {
                            sourceValue.CopyProperties(ref targetValue, prop.PropertyStructure, false, collectionParentPropertyInfo, collectionParentSource, collectionParentTarget);
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

        public static void SetValueIfChanged(this PropertyInfo property, object obj, object value)
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
