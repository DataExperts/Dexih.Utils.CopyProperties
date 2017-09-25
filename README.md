# Dexih.Utils.CopyProperties

[build]:    https://ci.appveyor.com/project/dataexperts/dexih-utils-copyproperties
[build-img]: https://ci.appveyor.com/api/projects/status/y9i1n0992fxg5ci0?svg=true
[nuget]:     https://www.nuget.org/packages/Dexih.Utils.CopyProperties
[nuget-img]: https://badge.fury.io/nu/Dexih.Utils.CopyProperties.svg
[nuget-name]: Dexih.Utils.CopyProperties

[![Build status][build-img]][build] [![Nuget][nuget-img]][nuget]

The CopyProperties library allows sophisticated duplication of c# objects. The will automatically duplicate equivalent properties in across two classes, including child properties, arrays and collections.

The library also provides advanced options such as:
 * Use property decorators to indicate how properties should be copied (such as by reference, ignored etc.)
 * Copy to target collections/arrays using lookup keys.
 
---

### Installation

Add the latest version of the package "Dexih.Utils.CopyProperties" to a .net core/.net project.

---

### Performance

The library performance well in initial test.

The PerformanceSample project can be run to perform some simple performance comparisons.

On a class containing an array with 500,000 items, and a list with 500,000 items.  The library performance as follows:

CopyProperties - 2.9 seconds.
Json Serliaize/Deserlialize - 3.8 seconds.

### Usage

The `CopyProperties` is used to perform the copy

To get started, add the following name space.
```csharp
using Dexih.Utils.CopyProperties;
```

To copy a object

```csharp
source.CopyProperties(target);
```


MORE DOCUMENTATION SHORTLY.
