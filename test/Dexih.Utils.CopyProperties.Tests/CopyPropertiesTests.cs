using System;
using System.Collections.Generic;
using Dexih.Utils;
using Xunit;
using System.Linq;
using System.Collections;
using Dexih.Utils.CopyProperties;

namespace Dexih.CopyProperties.Tests
{
    public class CopyPropertiesTests
    {
        /// simple enum used for IsSimpleType test
        public enum ETest { v1, v2, v3 };

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

        public class CopyTest
        {
            // basic values
            public Byte ByteValue { get; set; }
            public SByte SbyteValue { get; set; }
            public UInt16 Uint16Value { get; set; }
            public UInt32 Uint32Value { get; set; }
            public UInt64 Uint64Value { get; set; }
            public Int16 Int16Value { get; set; }
            public Int32 Int32Value { get; set; }
            public Int64 Int64Value { get; set; }
            public Decimal DecValue { get; set; }
            public Double DoubleValue { get; set; }
            public Single SingleValue { get; set; }
            public String StringValue { get; set; }
            public Boolean BooleanValue { get; set; }
            public DateTime DatetimeValue { get; set; }
            public TimeSpan TimespanValue { get; set; }
            public Guid GuidValue { get; set; }
            public ETest EnumValue { get; set; }

            public ChildTest NullChildTest { get; set; } = null;

            [CopyIgnore]
            public string IgnoreThis { get; set; }

            public ChildTest ChildValue { get; set; }

            [CopySetNull]
            public ChildTest ChildNullTarget { get; set; } 

            [CopyReference]
            public ChildTest ChildCopyReference { get; set; } 

            public ChildTest[] ChildArray { get; set; }
            public List<ChildTest> ChildList { get; set; }
            public Children Children { get; set; }

            public object[] EmptyArray { get; set; }

            public InheritedCollection InheritedCollection { get; set; }

            public void InitSampleValues1()
            {
                ByteValue = 1;
                SbyteValue = 2;
                Uint16Value = 3;
                Uint32Value = 4;
                Uint64Value = 5;
                Int16Value = 6;
                Int32Value = 7;
                Int64Value = 8;
                DecValue = 9;
                DoubleValue = 10.1;
                SingleValue = 11;
                StringValue = "12";
                BooleanValue = true;
                DatetimeValue = new DateTime(2001,01,01);
                TimespanValue = new TimeSpan(1, 2, 3);
                GuidValue = new Guid("e596b14b-f804-49c5-99dd-a0b900286f50");
                EnumValue = ETest.v1;

                ChildValue = new ChildTest() { Key = 50, Name = "childValue", Valid = true, IgnoreThis = "abc" };
                ChildNullTarget = ChildValue;
                ChildCopyReference = ChildValue;

                ChildArray = new ChildTest[]
                {
                    new ChildTest() {Key = 1, Name = "value 1", Valid = true, IgnoreThis = "abc" },
                    new ChildTest() {Key = 2, Name = "value 2", Valid = true, IgnoreThis = "abc" },
                    new ChildTest() {Key = 3, Name = "value 3", Valid = true, IgnoreThis = "abc" },
                };

                EmptyArray = new object[0];
                InheritedCollection = new InheritedCollection();

                ChildList = ChildArray.ToList();

                Children = new Children()
                {
                    new ChildTest() {Key = 1, Name = "value 1", Valid = true, IgnoreThis = "abc" },
                    new ChildTest() {Key = 2, Name = "value 2", Valid = true, IgnoreThis = "abc" },
                    new ChildTest() {Key = 3, Name = "value 3", Valid = true, IgnoreThis = "abc" },
                };
            }

            public void InitSampleValues2()
            {
                ByteValue = 100;
                SbyteValue = 20;
                Uint16Value = 300;
                Uint32Value = 400;
                Uint64Value = 500;
                Int16Value = 600;
                Int32Value = 700;
                Int64Value = 800;
                DecValue = 900;
                DoubleValue = 1000.1;
                SingleValue = 1100;
                StringValue = "1200";
                BooleanValue = true;
                DatetimeValue = new DateTime(2017, 01, 01);
                TimespanValue = new TimeSpan(4,5,6);
                GuidValue = new Guid("e596b14b-1111-1111-1111-a0b900286f50");
                EnumValue = ETest.v2;
            }

            public void InitRandomValues()
            {
                var random = new Random();

                ByteValue = Convert.ToByte(random.Next(Byte.MaxValue));
                SbyteValue = Convert.ToSByte(random.Next(SByte.MaxValue));
                Uint16Value = Convert.ToUInt16(random.Next(UInt16.MaxValue));
                Uint32Value = Convert.ToUInt32(random.Next(UInt16.MaxValue));
                Uint64Value = Convert.ToUInt32(random.Next(Int32.MaxValue));
                Int16Value = Convert.ToInt16(random.Next(Int16.MaxValue));
                Int32Value = Convert.ToInt32(random.Next(Int32.MaxValue));
                Int64Value = Convert.ToInt64(random.Next(Int32.MaxValue));
                DecValue = Convert.ToInt32(random.Next(Int32.MaxValue));
                DoubleValue = Convert.ToInt32(random.Next(Int32.MaxValue));
                SingleValue = Convert.ToInt32(random.Next(Int32.MaxValue));
                StringValue = Convert.ToInt32(random.Next(Int32.MaxValue)).ToString();
                BooleanValue = Convert.ToBoolean(random.Next(1));
                DatetimeValue = DateTime.Now;
                TimespanValue = DateTime.Now.TimeOfDay;
                GuidValue = Guid.NewGuid();
                EnumValue = (ETest)random.Next(2);
            }
        }

        public class Children: List<ChildTest>
        {
  
        }

        public class ChildTest
        {
            [CopyCollectionKey(0)]
            public int Key { get; set; }

            public string Name { get; set; }

            [CopyIsValid]
            public bool Valid { get; set; }

            [CopyIgnore]
            public string IgnoreThis { get; set; }
        }

        public class InheritedCollection: List<object[]>
        {

        }

    }
}
