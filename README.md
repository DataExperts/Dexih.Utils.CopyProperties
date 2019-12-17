# Dexih.Utils.CopyProperties

[build]:    https://ci.appveyor.com/project/dataexperts/dexih-utils-copyproperties
[build-img]: https://ci.appveyor.com/api/projects/status/ie0k2cjje6g032j9?svg=true
[nuget]:     https://www.nuget.org/packages/Dexih.Utils.CopyProperties
[nuget-img]: https://badge.fury.io/nu/Dexih.Utils.CopyProperties.svg
[nuget-name]: Dexih.Utils.CopyProperties

[![Build status][build-img]][build] [![Nuget][nuget-img]][nuget]

The `CopyProperties` library allows deep copy of c# objects along with merge and delta capabilities.

The primary benefits:

 * Performs well, often faster then via serialization.
 * Copy between two classes with similar properties, even when they are not the same primary types.
 * Use property decorators to customize how properties should be copied (such as by reference, ignored etc.)
 * Merge collections/arrays using keys.
 * Can cascade parent keys to child records.
 * Supports Lists/Arrays and other a number of other collection types.

A primary use-case we use the library for, is that is allows us to merge classes changed through a javascript front end, and then merge them into existing Entity Framework entities.  Using the CopyProperties function which merges values (rather than serialization which just creates new instances) the Entity Framework change detection works, and the database changes are applied correctly.

---

### Installation

Add the [latest version][nuget] of the package "Dexih.Utils.CopyProperties" to a .net core/.net project.  This supports .net standard framework 1.3 or newer, or the .net framework 4.6 or newer.

---

### Limitations

This is still early release, and passing our basic tests.  Use with care, and ensure you build your own tests to confirm specific functionality is working. 

This has the following limitations:

* Only supports class properties (i.e. properties declared with a get/set).  Fields are ignored.
* Only supports a limited number of collection types such as List/HashSet.  It should work if the list is derived from `IEnumerable` and contains a `Add(item)` and `Remove(item)` function.  Dictionary/Queue and other types that don't meet this format are not supported.

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

[Hello world demo](https://dotnetfiddle.net/0GQjJh)

### Performance

The following sample is an alternative for cloning using the `JsonConvert` library to serialize/deserialize the object.

```csharp
var serialized = JsonConvert.SerializeObject(original);
var copy = JsonConvert.DeserializeObject<SampleClass>(serialized);
```

The same result can be achieved with the CopyProperties library.

```csharp
var copyClass = original.CloneProperties<SampleClass>();
```

With a small set of data (~50 row collection) the performance is about 10x faster (50ms for the CopyProperties, 500ms for the JsonConvert).
For a larger set of data(~500,000 row collection) the performance gain is more modest about 30% faster (3 seconds for CopyProperties, 4 seconds for JsonConvert)

[Performance demo](https://dotnetfiddle.net/SMR1vF)

**Special Note**: For best performance a hand coded copy will perform significantly better than either this library or serialization, as the `Reflection` library adds significant overhead.

### Usage

To get started, add the following name space.

```csharp

using Dexih.Utils.CopyProperties;
```

There are two functions available; the `CopyProperties` and the `CloneProperties`.  The only difference between these is the `CopyProperties` populates an already created instance, adn the `CloneProperties` creates and returns a new instance.

The following example performs the same result using the two available functions.

```csharp
// CopyProperties requires an instance to be already created.
var copy = new SampleClass();
original.CopyProperties(copy);

// CloneProperties creates and returns a clone automatically.
copy = original.CloneProperties<SampleClass>();
```

By default these functions will perform a deep copy of the object which will recurse through any child object properties, arrays and collections.  If all that is required is a shallow copy (i.e. only top level primary properties such as int/string/dates), this can be done by setting the `shallowCopy` parameter to `false` as follows.

```csharp
// CopyProperties shallow copy
var copy = new SampleClass(false);
original.CopyProperties(copy);

// CloneProperties shallow copy
copy = original.CloneProperties<SampleClass>(false);
```

### Merging Objects

By using class decorators, the CopyProperties is able to perform a delta between arrays/collections.

This is done by setting a key attribute in the class that is being merged.  The key value must be decorated with the [CopyCollectionKey] attribute.  In addition an optional [CopyIsValid] property can be decorated, which will be set `false` when the target list contains a record in the target list.  If there is no [CopyIsValid] attribute set, then target records that do not exist in the source will be removed.

The following is a sample class that uses these decorators.

```csharp

public class Student
{
  [CopyCollectionKey]
  public string StudentId { get; set; }
  public string FirstName { get; set; }
  public string Surname { get; set; }
  public string Class {get ;set; }

  [CopyIsValid]
  public bool IsCurrentStudent {get; set; }

}
```

If this class is part of an array or collection, when records are removed from the source, they will **not** be removed from the target, rather the `CopyIsValid` property will be set to false.

```csharp
var students = new List<Student>()
{
  new Student() { StudentId = "100", FirstName = "John", LastName = "Doe" },
  new Student() { StudentId = "200", FirstName = "Jane", LastName = "Smith" },
  new Student() { StudentId = "300", FirstName = "Joe", LastName = "Bloggs" },
};

// create a student list.  The IsCurrentStudent will be true for all records
var studentsList = students.CloneProperties<List<Student>>();

// remove an item from the source list
var student = students.Single(c => c.StudentId == "200");
students.Remove(student);
students.CopyProperties(studentList);

// the student list still contains 3 records, however the StudentId="200" will have a IsCurrentStudent=false
```

[Merge demo](https://dotnetfiddle.net/T67cgJ)

### Cascading a ParentKey to a ChildRecord

In some scenarios, such as dealing with objects that will be written to database tables, it can be useful to cascade key values in parent record to child records.

The following student/teacher record shows how to achieve this.

```csharp
public class Teacher
{
  [CopyCollectionKey]
  public string TeacherId { get; set; }

  public string FirstName { get; set; }
  public string LastName { get; set; }

  public List<Student> Students { get; set; }
}

public class Student
{
  [CopyCollectionKey]
  public string StudentId { get; set; }

  public string FirstName { get; set; }
  public string LastName { get; set; }

  [CopyParentCollectionKey]
  public string TeacherId { get; set; }
}
```

If the teacher is created, and populated with students, the `CopyProperties` library will automatically cascade the TeacherId into the student record.

```csharp

var studentlist = new List<Student>()
{
  new Student() { StudentId = "100", FirstName = "John", LastName = "Doe" },
  new Student() { StudentId = "200", FirstName = "Jane", LastName = "Smith" },
  new Student() { StudentId = "300", FirstName = "Joe", LastName = "Bloggs" },
};

var teacher = new Teacher()
{
  TeacherId = "T100",
  FirstName = "Edna",
  LastName = "Krabappel",
  Students = studentlist
};

var newTeacher = teacher.CloneProperties<Teacher>();

// the newTeacher.Students record will now all contain the value "T100"
```

[ParentKey Demo](https://dotnetfiddle.net/LNky35)

### Attributes

The following is a complete list of the attributes available for decorating class properties:

* CopyCollectionKey(object defaultKeyValue, bool resetNegativeKeys = false) - Specifies that the property is a key.  The `defaultKeyValue` is applied to records which are null, or negative values when the `resetNegativeKeys=true`.
* CopyIsValid - Specifies a target record should not be removed, instead this property will be set to `false`.  Otherwise, this wil be set to `true`.
* CopyParentCollectionKey - Specifies that the `CopyCollectionKey` property from the nearest parent class should be applied to this property.
* CopyIgnore - Ignore the property.
* CopyReference - Copy the object reference, rather then performing a recursive deep copy of the property.
* CopySetNull - Set the target to null.
* CopyIfTargetNull - Only copy if the target value is null.
* CopyIfTargetNotNull - Only copy if the target value is not null.
* CopyIfTargetDefault - Only copy if the target is the default value for the variable type.  For example if the variable is an `int`, the copy will only occur when the vaue is `0`.
* CopyIfTargetNotDefault - Only copy if the target is not the default value.

### Contributions / Feedback

I welcome feedback and contributions to this library.

* For issues/bugs please try to provide a test (preferably a [fiddle](https://dotnetfiddle.net)) that demonstrates the issue.
* For pull requests, please provide adequate tests before submitting.

Good luck.

Gary (https://dataexpertsgroup.com)
