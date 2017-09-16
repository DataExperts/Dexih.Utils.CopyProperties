using System;
using System.Collections;
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
        /// <summary>
        /// Extension for 'Object' that copies matching properties from the source to destination object.
        /// If the onlySimpleProperties = false this will also make copies of child objects such as collections, and arrays.
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="target">The destination object</param>
        /// <param name="onlySimpleProperties">Indicates only simple values will be copied such as string, int, date etc. </param>
        /// <param name="parentKeyValue">The destination object</param>
        public static void CopyProperties(this object source, object target, bool onlySimpleProperties = false, object parentKeyValue = null)
        {
                // If source/dest are null throw an exception
                if (source == null)
                {
                    throw new CopyPropertiesNullException();
                }

                // if this is a simple type, throw exception.
                if (IsSimpleType(source.GetType()))
                {
                    throw new CopyPropertiesSimpleTypeException(source);
                }

                // Getting the Types of the objects
                var typeDest = target.GetType();
                var typeSrc = source.GetType();

                // Iterate the Properties of the source instance and  
                // populate them from their desination counterparts  
                var srcProps = typeSrc.GetProperties();

                // get the collectionKey value first
                object collectionKeyValue = null;
                foreach (var srcProp in srcProps)
                {
                    if (srcProp.GetCustomAttribute(typeof(CopyCollectionKeyAttribute), true) != null)
                    {
                        collectionKeyValue = srcProp.GetValue(source);
                    }
                }

            // loop through each property in the object.
            foreach (var srcProp in srcProps)
            {
                try
                {
                    var targetProperty = typeDest.GetProperty(srcProp.Name);

                    // no matching target property, then continue
                    if (targetProperty == null)
                    {
                        continue;
                    }

                    // can't read the source property.
                    if (!srcProp.CanRead)
                    {
                        continue;
                    }

                    // can't write to the target property.
                    if (!targetProperty.CanWrite)
                    {
                        continue;
                    }

                    // ignore attribute on the source property, then skip
                    if (srcProp.GetCustomAttribute(typeof(CopyIgnoreAttribute), true) != null)
                    {
                        continue;
                    }

                    // ignore attribute on the target property, then skip
                    if (targetProperty.GetCustomAttribute(typeof(CopyIgnoreAttribute), true) != null)
                    {
                        continue;
                    }

                    if (!onlySimpleProperties)
                    {
                        IEnumerable srcCollection = srcProp.GetValue(source, null) as IEnumerable;
                        IEnumerable targetCollection;

                        // if this is an array, then temporarily use a list as the target collection.
                        if (targetProperty.PropertyType.IsArray)
                        {
                            var arrayItemType = targetProperty.PropertyType.GetElementType();
                            var targetArray = srcProp.GetValue(target, null) as IEnumerable;
                            if (targetArray == null)
                            {
                                var listType = typeof(List<>).MakeGenericType(arrayItemType);
                                targetCollection = (IEnumerable)Activator.CreateInstance(listType);
                            }
                            else
                            {
                                targetCollection = targetArray.Cast<object>().ToList();
                            }
                        }

                        // if the item is a collection, then iterate through each property
                        else if (srcProp.PropertyType.IsNonStringEnumerable() && srcProp.CanWrite)
                        {
                            srcCollection = (IEnumerable)srcProp.GetValue(source, null);
                            targetCollection = (IEnumerable)targetProperty.GetValue(target, null);

                            if (targetCollection == null)
                            {
                                targetCollection = (IEnumerable)Activator.CreateInstance(targetProperty.PropertyType);
                                targetProperty.SetValue(target, targetCollection);
                            }
                        }

                        // if it is a simple type (aka string, int, etc), then copy it across.
                        else if (IsSimpleType(targetProperty.PropertyType))
                        {
                            targetProperty.SetValue(target, srcProp.GetValue(source, null), null);
                            continue;
                        }

                        else
                        {
                            var srcValue = srcProp.GetValue(source);
                            var targetValue = targetProperty.GetValue(target);

                            if (srcValue == null)
                            {
                                targetProperty.SetValue(target, null);
                            }
                            else
                            {
                                if (targetValue == null)
                                {
                                    targetValue = Activator.CreateInstance(targetProperty.PropertyType);
                                    targetProperty.SetValue(target, targetValue);
                                }

                                srcValue.CopyProperties(targetValue, false, null);
                            }
                            continue;
                        }

                        if (srcCollection == null)
                        {
                            targetProperty.SetValue(target, null);
                            continue;
                        }

                        var addMethod = targetCollection.GetType().GetMethod("Add");

                        if (addMethod == null)
                        {
                            throw new CopyPropertiesInvalidCollectionException($"The target object contains a collection ${targetCollection.GetType().ToString()} which does not contain an \"Add\" method.  The copy properties can only function with collections such as List<> which have an \"Add\" method");
                        }

                        if (addMethod.GetParameters().Length != 1)
                        {
                            throw new CopyPropertiesInvalidCollectionException($"The target object contains a collection ${targetCollection.GetType().ToString()} contains an \"Add\" method which has more than one parameter.  The copy properties can only function with collections such as List<> which have simple \"Add\" method with one parameter.");
                        }

                        var collectionItemType = addMethod.GetParameters()[0].ParameterType;
                        var collectionProps = collectionItemType.GetProperties();

                        PropertyInfo keyAttribute = null;
                        CopyCollectionKeyAttribute keyAttributeProperties = null;
                        PropertyInfo isValidAttribute = null;

                        foreach (var prop in collectionProps)
                        {
                            if (prop != null && prop.GetCustomAttribute<CopyCollectionKeyAttribute>(true) != null)
                            {
                                keyAttribute = prop;
                                keyAttributeProperties = prop.GetCustomAttribute<CopyCollectionKeyAttribute>(true);
                            }
                            if (prop != null && prop.GetCustomAttribute(typeof(CopyIsValidAttribute), true) != null)
                            {
                                isValidAttribute = prop;
                            }
                        }

                        // if there is an IsValid attribute, set all target items to isvalid = false.  
                        if (isValidAttribute != null && keyAttribute != null)
                        {
                            foreach (var item in (IEnumerable)targetCollection)
                            {
                                isValidAttribute.SetValue(item, false);
                            }
                        }

                        foreach (var item in srcCollection)
                        {
                            object targetItem = null;
                            object keyvalue = null;
                            if (keyAttribute != null && keyAttributeProperties != null)
                            {
                                keyvalue = keyAttribute.GetValue(item);
                                if (keyAttributeProperties.DefaultKeyValue != null && Equals(keyvalue, keyAttributeProperties.DefaultKeyValue))
                                {

                                }
                                else
                                {
                                    foreach (var matchItem in (IEnumerable)targetCollection)
                                    {
                                        var targetValue = keyAttribute.GetValue(matchItem);
                                        if (Equals(targetValue, keyvalue))
                                        {
                                            if (targetItem != null)
                                            {
                                                throw new Exception($"The collections could not be merge due to multiple target key values of {keyvalue} in the collection {collectionItemType}.");
                                            }
                                            targetItem = matchItem;
                                        }
                                    }
                                }
                            }

                            if (targetItem == null)
                            {
                                targetItem = Activator.CreateInstance(collectionItemType);
                                item.CopyProperties(targetItem, false, collectionKeyValue);
                                addMethod.Invoke(targetCollection, new[] { targetItem });
                            }
                            else
                            {
                                item.CopyProperties(targetItem, false, collectionKeyValue);
                            }

                        }

                        //reset all the keyvalues < 0 to 0.  Negative numbers are used to maintain links, but need to be zero before saving datasets to repository.
                        if (keyAttribute != null && keyAttributeProperties.ResetNegativeKeys)
                        {
                            foreach (var item in (IEnumerable)targetCollection)
                            {
                                var itemValue = keyAttribute.GetValue(item);
                                var longValue = Convert.ToInt64(itemValue);
                                if (longValue < 0)
                                {
                                    keyAttribute.SetValue(item, keyAttributeProperties.DefaultKeyValue);
                                }
                            }
                        }

                        // if the target is an array, copy the temporary collection back to an array.
                        if (srcProp.PropertyType.IsArray)
                        {
                            var targetArray = Array.CreateInstance(collectionItemType, targetCollection.Cast<object>().Count());
                            var i = 0;
                            foreach (object item in targetCollection)
                            {
                                targetArray.SetValue(item, i);
                                i++;
                            }
                            targetProperty.SetValue(target, targetArray);
                        }
                    }

                    if (!IsSimpleType(srcProp.PropertyType))
                    {
                        continue;
                    }

                    if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate)
                    {
                        continue;
                    }

                    if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                    {
                        continue;
                    }

                    if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                    {
                        continue;
                    }

                    if (targetProperty.GetCustomAttribute(typeof(CopyParentCollectionKeyAttribute), true) != null && parentKeyValue != null)
                    {
                        targetProperty.SetValue(target, parentKeyValue);
                        continue;
                    }

                    // Passed all tests, lets set the value
                    targetProperty.SetValue(target, srcProp.GetValue(source, null), null);
                }
                catch (Exception ex)
                {
                    throw new CopyPropertiesException($"CopyProperties failed in property {srcProp.Name}.  {ex.Message}.");
                }
            }
        }

        public static bool IsSimpleType(this Type type)
        {
            return
                type.GetTypeInfo().IsPrimitive ||
                type.GetTypeInfo().IsEnum ||
                new[] {
                    typeof(Enum),
                    typeof(string),
                    typeof(decimal),
                    typeof(DateTime),
                    typeof(DateTimeOffset),
                    typeof(TimeSpan),
                    typeof(Guid)
                }.Contains(type) ||
                type.GetTypeInfo().BaseType == typeof(Enum) ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]))
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

    }
}
