# Dexih.Utils.CopyProperties

[build]:    https://ci.appveyor.com/project/dataexperts/dexih-utils-copyproperties
[build-img]: https://ci.appveyor.com/api/projects/status/ie0k2cjje6g032j9?svg=true
[nuget]:     https://www.nuget.org/packages/Dexih.Utils.CopyProperties
[nuget-img]: https://badge.fury.io/nu/Dexih.Utils.CopyProperties.svg
[nuget-name]: Dexih.Utils.CopyProperties

[![Build status][build-img]][build] [![Nuget][nuget-img]][nuget]

The CopyProperties library allows sophisticated deep copy of c# objects. The will automatically duplicate equivalent properties in across two classes, including child properties, arrays and collections.

The primary benefits of this library:
 * Performance copies faster then via serialization.
 * Automatically copy properties between two classes with similar properties, even then they are not the same types.
 * Use property decorators to customize how properties should be copied (such as by reference, ignored etc.)
 * Merge to target collections/arrays using lookup keys.
 
---

### Installation

Add the latest version of the package "Dexih.Utils.CopyProperties" to a .net core/.net project.

---

### Hello World Example

```csharp
using System;
using Dexih.Utils.CopyProperties;
					
public class Program
{
	public static void Main()
	{
		var array = new string[] {"hello", "world"};
		var cloned = array.CloneProperties<string[]>();
		
		Console.WriteLine(string.Join(" ", cloned));
	}
}
```
[Hello world fiddle](https://dotnetfiddle.net/0GQjJh)

### Performance

The following method is a commonly used alternative for cloning using the `JsonConvert` library to serialize/deserialize the object.

```csharp
var serialized = JsonConvert.SerializeObject(original);
var copy = JsonConvert.DeserializeObject<SampleClass>(serialized);
```

The same result can be achieved with the CopyProperties library.

```csharp
var copyClass = original.CloneProperties<SampleClass>();
```

With a small set of data (~50 row collection) the performance is about 10x faster (50ms for the CopyProperties, 500ms for the JsonConvert).
For a larger set of data(~500,000 row collection) the performance gain is more modest about 30% faster (3 seconds for CopyProperties, 4 seconsd for JsonConvert)

Special note: for best performance a hand coded copy will perform significatly better than either this library or serlialization, as it won't depend on the `Reflection` library, which adds a significant overhead.


### Usage

To get started, add the following name space.
```csharp
using Dexih.Utils.CopyProperties;
```

There are two main key function the `CopyProperties` and the `CloneProperties`.  The only difference between these is the `CopyProerties` populates an already created instance, adn the `CloneProperties` creates and rerturns a copy.

The following example performs the same result using the two available functions.

```charp
// CopyProperties requires an instance to be already created.
var copy = new SampleClass();
original.CopyProperties(copy);

// CloneProperties creates and returns a clone automatically.
copy = original.CloneProperties<SampleClass>();
```

By default the CopyProperties function will perform a deep copy of the object.  This means is will recurse through object proerties, lists and collections.  If all that is required is a shallow copy, that isonly copy the top level primary properties such as int/string/dates, this can be done by setting the `shallowCopy` parameter to `false` as follows:
```charp
// CopyProperties shallow copy
var copy = new SampleClass(false);
original.CopyProperties(copy);

// CloneProperties shallow copy
copy = original.CloneProperties<SampleClass>(false);
```

### Merging Objects




### Attributes

* CopyCollectionKey(object defaultKeyValue, bool resetNegativeKeys = false)
* CopyIsValid
* CopyParentCollectionKey
* CopyIgnore
* CopyReference
* CopySetNull
* CopyIfTargetNull
* CopyIfTargetNotNull
* CopyIfTargetDefault
* CopyIfTargetNotDefault

MORE DOCUMENTATION SHORTLY.
