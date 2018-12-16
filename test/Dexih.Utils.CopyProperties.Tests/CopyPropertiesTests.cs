using System;
using System.Collections.Generic;
using Dexih.Utils;
using Xunit;
using System.Linq;
using System.Collections;
using Dexih.Utils.CopyProperties;

namespace Dexih.Utils.CopyProperties.Tests
{
    public class CopyPropertiesTests
    {
        [Theory]
        [InlineData("hi", true)]
        [InlineData(1, true)]
        [InlineData(1.1, true)]
        [InlineData(ETest.v1, true)]
        [InlineData(true, true)]
        [MemberData(nameof(OtherProperties))]
        public void IsSimpleTypeTests(object value, bool expected)
        {
            Assert.Equal(expected, value.GetType().IsSimpleType());
        }

        private static IEnumerable<object[]> OtherProperties()
        {
            var dateValue = DateTime.Parse("2001-01-01");
            var timeValue = new TimeSpan(1, 2, 3);

            return new[]
            {
                new object[] { new object[] {1,2,3}, false },
                new object[] { new int[] {1,2,3}, false },
                new object[] { new string[] { "hi", "there" }, false },
                new object[] { dateValue, true },
                new object[] { timeValue, true }
            };
        }

        [Fact]
        public void CopyPropertiesSimpleTypeThrows()
        {
            var value = "test";
            var newValue = "";
            Assert.Throws(typeof(CopyPropertiesSimpleTypeException), () => value.CopyProperties(newValue));
        }

        [Fact]
        public void CopyPropertiesNullThrows()
        {
            object value = null;
            var newValue = "";
            Assert.Throws(typeof(CopyPropertiesNullException), () => value.CopyProperties(newValue));
        }

        [Fact]
        public void CopyPropertiesPrimaryOnly()
        {

            var copyTest1 = new CopyTest();
            var copyTest2 = new CopyTest();

            copyTest1.InitSampleValues1();
            copyTest1.CopyProperties(copyTest2, true);

            int count = 0;
            var type = copyTest1.GetType();
            var properties = copyTest1.GetType().GetProperties();
            foreach (var property in properties)
            {
                if(Reflection.IsSimpleType(property.PropertyType) && property.Name != "IgnoreThis")
                {
                    count++;
                    Assert.Equal(property.GetValue(copyTest1), property.GetValue(copyTest2));
                }
            }

            // confirm the childarray was not copied
            Assert.Null(copyTest2.ChildArray);
            Assert.Null(copyTest2.ChildList);

            // confirm all properties were tested
            Assert.Equal(count, 17);
        }

        [Fact]
        public void CopyPropertiesHashSet()
        {
            var hashSet = new HashSet<string>() { "123", "456", "789" };
            var hashSet2 = hashSet.CloneProperties<HashSet<string>>();

            Assert.Equal(3, hashSet2.Count);
            Assert.Equal("123", hashSet2.ElementAt(0));
            Assert.Equal("456", hashSet2.ElementAt(1));
            Assert.Equal("789", hashSet2.ElementAt(2));
           
        }

        [Fact]
        public void OverwriteEqualSizeArray()
        {
            var array1 = new string[] { "123", "456", "789" };
            var array2 = new string[] { "abc", "def", "hij" };

            array1.CopyProperties(array2);

            Assert.Equal("123", array2[0]);
            Assert.Equal("456", array2[1]);
            Assert.Equal("789", array2[2]);
        }

        [Fact]
        public void OverwriteEmptyArray()
        {
            var array1 = new string[] { "123", "456", "789" };
            var array2 = new string[] { };

            // won't work as the arrays are difference sizes
            Assert.Throws(typeof(CopyPropertiesTargetInstanceException), () => array1.CopyProperties(array2));

            object arrayObject = null;
            array1.CopyProperties(ref arrayObject, false);

            var arrayReturn = (string[])arrayObject;

            Assert.Equal("123", arrayReturn[0]);
            Assert.Equal("456", arrayReturn[1]);
            Assert.Equal("789", arrayReturn[2]);
        }


        [Fact]
        public void CopyPropertiesArray()
        {
            var copyTestArray1 = new ChildTest[]
            {
                    new ChildTest() {Key = 1, Name = "value 1", Valid = true, IgnoreThis = "abc" },
                    new ChildTest() {Key = 2, Name = "value 2", Valid = true, IgnoreThis = "abc" },
                    new ChildTest() {Key = 3, Name = "value 3", Valid = true, IgnoreThis = "abc" },
            };

            var copyTestArray2 = (ChildTest[]) copyTestArray1.CloneProperties();

            Assert.Equal(3, copyTestArray2.Count());
            for (var i = 0; i < copyTestArray1.Length; i++)
            {
                Assert.Equal(copyTestArray1[i].Key, copyTestArray2[i].Key);
                Assert.Equal(copyTestArray1[i].Name, copyTestArray2[i].Name);
                Assert.Equal(copyTestArray1[i].Valid, copyTestArray2[i].Valid);
                Assert.NotEqual(copyTestArray1[i].IgnoreThis, copyTestArray2[i].IgnoreThis);
            }
        }

        [Fact]
        public void CopyWithIsValidFalse()
        {
            var copyTestArray1 = new ChildTest[]
            {
                    new ChildTest() {Key = 1, Name = "value 1", Valid = false, IgnoreThis = "abc" },
                    new ChildTest() {Key = 2, Name = "value 2", Valid = false, IgnoreThis = "abc" },
            };

            var copyTestArray2 = (ChildTest[]) copyTestArray1.CloneProperties();

            Assert.Equal(2, copyTestArray2.Count());
            for (var i = 0; i < copyTestArray1.Length; i++)
            {
                Assert.Equal(copyTestArray1[i].Key, copyTestArray2[i].Key);
                Assert.Equal(copyTestArray1[i].Name, copyTestArray2[i].Name);
                Assert.Equal(copyTestArray1[i].Valid, copyTestArray2[i].Valid);
                Assert.NotEqual(copyTestArray1[i].IgnoreThis, copyTestArray2[i].IgnoreThis);
            }
        }

        [Fact]
        public void CopyPropertyMixedCollection()
        {
            var mixed = new MixedCollection();
            mixed.string1 = "abc";
            mixed.int1 = 2;

            mixed.Add("123");
            mixed.Add("456");
            mixed.Add("789");

            var mixed2 = mixed.CloneProperties<MixedCollection>();

            Assert.Equal(3, mixed2.Count);
            Assert.Equal("abc", mixed2.string1);
            Assert.Equal(2, mixed2.int1);
            Assert.Equal("123", mixed2[0]);
            Assert.Equal("456", mixed2[1]);
            Assert.Equal("789", mixed2[2]);

        }

        [Fact]
        public void CopyParentCollectionKeyTest()
        {
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
            Assert.Equal("T100", newTeacher.Students[0].TeacherId);
            Assert.Equal("T100", newTeacher.Students[1].TeacherId);
            Assert.Equal("T100", newTeacher.Students[2].TeacherId);
            
            Assert.Equal("Edna", newTeacher.Students[0].TeacherName);
            Assert.Equal("Edna", newTeacher.Students[1].TeacherName);
            Assert.Equal("Edna", newTeacher.Students[2].TeacherName);
        }

        [Fact]
        public void CopyCollectionKeyTest()
        {
            var studentlist = new List<Student>()
            {
                new Student() { StudentId = "100", FirstName = "John", LastName = "Doe" },
                new Student() { StudentId = "200", FirstName = "Jane", LastName = "Smith" },
                new Student() { StudentId = "300", FirstName = "Joe", LastName = "Bloggs" },
            };

            var newList = studentlist.CloneProperties<List<Student>>();

            Assert.Equal(3, newList.Count);
            Assert.Equal("100", newList[0].StudentId);
            Assert.Equal("John", newList[0].FirstName);
            Assert.Equal("Doe", newList[0].LastName);
            Assert.Equal("200", newList[1].StudentId);
            Assert.Equal("300", newList[2].StudentId);

            // test a modification to the source list.
            studentlist[0].LastName = "Does";
            studentlist.CopyProperties(newList);
            Assert.Equal("100", newList[0].StudentId);
            Assert.Equal("Does", newList[0].LastName);

            // test an addition to the source list
            studentlist.Add(new Student() { StudentId = "400", FirstName = "Destiny", LastName = "Child" });
            studentlist.CopyProperties(newList);
            Assert.Equal("400", newList[3].StudentId);

            // test a remove from the source list
            studentlist.RemoveAt(1);
            studentlist.CopyProperties(newList);
            Assert.Equal(3, newList.Count);
            Assert.Equal(0, newList.Count(c => c.StudentId == "200"));

        }

     

        [Fact]
        public void CopyCollectionKeyInvalidItemTest()
        {
            var studentList = new List<Student2>()
            {
                new Student2() { StudentId = "100", FirstName = "John", LastName = "Doe", IsCurrentStudent = true },
                new Student2() { StudentId = "200", FirstName = "Jane", LastName = "Smith", IsCurrentStudent = true },
                new Student2() { StudentId = "300", FirstName = "Joe", LastName = "Bloggs", IsCurrentStudent = true },
            };

            var newList = studentList.CloneProperties<List<Student2>>();

            Assert.Equal(3, newList.Count);
            Assert.Equal("100", newList[0].StudentId);
            Assert.Equal("John", newList[0].FirstName);
            Assert.Equal(true, newList[0].IsCurrentStudent);
            Assert.Equal("Doe", newList[0].LastName);
            Assert.Equal("200", newList[1].StudentId);
            Assert.Equal(true, newList[1].IsCurrentStudent);
            Assert.Equal("300", newList[2].StudentId);
            Assert.Equal(true, newList[2].IsCurrentStudent);

            // test a modification to the source list.
            studentList[0].LastName = "Does";
            studentList.CopyProperties(newList);
            Assert.Equal(3, newList.Count);
            Assert.Equal("100", newList[0].StudentId);
            Assert.Equal("Does", newList[0].LastName);
            Assert.Equal(true, newList[0].IsCurrentStudent);

            // test an addition to the source list
            studentList.Add(new Student2() { StudentId = "400", FirstName = "Destiny", LastName = "Child", IsCurrentStudent = true });
            studentList.CopyProperties(newList);
            Assert.Equal("400", newList[3].StudentId);
            Assert.Equal(true, newList[3].IsCurrentStudent);

            // test a remove from the source list
            studentList.RemoveAt(1);
            studentList.CopyProperties(newList);
            Assert.Equal(4, newList.Count);
            Assert.Equal(1, newList.Where(c => c.StudentId == "200" && !c.IsCurrentStudent).Count());

        }




        [Fact]
        public void CopyPropertiesArrayLists()
        {

            var copyTest1 = new CopyTest();
            var copyTest2 = new CopyTest();

            copyTest1.InitSampleValues1();
            copyTest1.CopyProperties(copyTest2, false);

            int count = 0;
            int enumerableCount = 0;
            int childValueCount = 0;

            var type = copyTest1.GetType();
            var properties = copyTest1.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (Reflection.IsSimpleType(property.PropertyType) && property.Name != "IgnoreThis")
                {
                    count++;
                    Assert.Equal(property.GetValue(copyTest1), property.GetValue(copyTest2));
                }

                else if(Reflection.IsNonStringEnumerable(property))
                {
                    enumerableCount++;
                    var items1 = (IEnumerable)property.GetValue(copyTest1, null);
                    var items2 = (IEnumerable)property.GetValue(copyTest2, null);

                    var items1Array = items1.Cast<ChildTest>().ToArray();
                    var items2Array = items2.Cast<ChildTest>().ToArray();

                    Assert.Equal(items1Array.Length, items2Array.Length);

                    for(var i =0; i < items1Array.Length; i++)
                    {
                        Assert.Equal(items1Array[i].Key, items2Array[i].Key);
                        Assert.Equal(items1Array[i].Name, items2Array[i].Name);
                        Assert.Equal(items1Array[i].Valid, items2Array[i].Valid);
                        Assert.NotEqual(items1Array[i].IgnoreThis, items2Array[i].IgnoreThis);
                    }
                }

                else if(property.Name == "ChildValue")
                {
                    childValueCount++;

                    var item1 = (ChildTest)property.GetValue(copyTest1);
                    var item2 = (ChildTest)property.GetValue(copyTest2);

                    Assert.Equal(item1.Key, item2.Key);
                    Assert.Equal(item1.Name, item2.Name);
                    Assert.Equal(item1.Valid, item2.Valid);
                    Assert.NotEqual(item1.IgnoreThis, item2.IgnoreThis);

                    //the child value is a copy, so we should be able to change value in the source without impacting target
                    item1.Name = "changed name";
                    Assert.NotEqual(item1.Name, item2.Name);
                }

                else if (property.Name == "ChildCopyReference")
                {
                    childValueCount++;

                    var item1 = (ChildTest)property.GetValue(copyTest1);
                    var item2 = (ChildTest)property.GetValue(copyTest2);

                    Assert.Equal(item1.Key, item2.Key);
                    Assert.Equal(item1.Name, item2.Name);
                    Assert.Equal(item1.Valid, item2.Valid);
                    Assert.Equal(item1.IgnoreThis, item2.IgnoreThis); //equal the parent object reference was copied so subproperties will be ignored.

                    //the child value is a reference, so we should be able to change value in the source and this will change target.
                    item1.Name = "changed name";
                    Assert.Equal(item1.Name, item2.Name);
                }

                else if (property.Name == "ChildNullTarget")
                {
                    childValueCount++;

                    var item1 = (ChildTest)property.GetValue(copyTest1);
                    var item2 = (ChildTest)property.GetValue(copyTest2);

                    Assert.Null(item2);
                }
            }
    
            // confirm all properties were tested
            Assert.Equal(count, 17);
            Assert.Equal(enumerableCount, 5);
            Assert.Equal(childValueCount, 3);
        }

   

    }
}
