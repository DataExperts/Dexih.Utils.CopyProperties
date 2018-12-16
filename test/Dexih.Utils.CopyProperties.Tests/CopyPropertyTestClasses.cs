using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Dexih.Utils.CopyProperties.Tests
{
    /// simple enum used for IsSimpleType test
    public enum ETest { v1, v2, v3 };


    public class MixedCollection : List<string>
    {
        public string string1 { get; set; }
        public int int1 { get; set; }
    }

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
        
        [CopyParentCollectionKey(nameof(Teacher.FirstName))]
        public string TeacherName { get; set; }

    }

    public class Student2
    {
        [CopyCollectionKey]
        public string StudentId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        [CopyIsValid]
        public bool IsCurrentStudent { get; set; }
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
            DatetimeValue = new DateTime(2001, 01, 01);
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
            TimespanValue = new TimeSpan(4, 5, 6);
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

    public class Children : List<ChildTest>
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

    public class InheritedCollection : List<object[]>
    {

    }
    class CopyPropertyTestClasses
    {
    }
}
