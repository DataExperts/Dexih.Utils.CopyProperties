using Dexih.Utils.CopyProperties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace PerformanceCompare
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Create class");

            Stopwatch stopwatch = Stopwatch.StartNew();
            var original = new SampleClass();
            original.InitSamples(50000);

//            Console.WriteLine($"Time to create sample class: {stopwatch.Elapsed}");
//
//            stopwatch.Restart();
//            var copyClass = original.CloneProperties<SampleClass>();
//
//            Console.WriteLine($"Time to copy empty class: {stopwatch.Elapsed} {copyClass.children.Count} ");
//
//            stopwatch.Restart();
//            copyClass = original.CloneProperties<SampleClass>();
//
//            Console.WriteLine($"Time to copy empty class(cached): {stopwatch.Elapsed} {copyClass.children.Count} ");
//
//            
//            stopwatch.Restart();
//            original.CopyProperties(copyClass);
//            Console.WriteLine($"Time to copy populated class: {stopwatch.Elapsed} {copyClass.children.Count}");

            stopwatch.Restart();
            var copy = original.CloneProperties<SampleClass>();
            Console.WriteLine($"Time to copy populated class: {stopwatch.Elapsed} {copy.children.Count}");

            stopwatch.Restart();
            copy = original.CloneProperties<SampleClass>();
            Console.WriteLine($"Time to copy populated class (2nd): {stopwatch.Elapsed} {copy.children.Count}");

            
            stopwatch.Restart();
            var serialized = JsonConvert.SerializeObject(original);
            var searlizedCopy = JsonConvert.DeserializeObject<SampleClass>(serialized);
            Console.WriteLine($"Time to copy via json serialize: {stopwatch.Elapsed} {searlizedCopy.children.Count}");

            stopwatch.Restart();
            serialized = JsonConvert.SerializeObject(original);
            searlizedCopy = JsonConvert.DeserializeObject<SampleClass>(serialized);
            Console.WriteLine($"Time to copy via json serialize(2nd): {stopwatch.Elapsed} {searlizedCopy.children.Count}");

            Console.ReadLine();
        }
    }

    class SampleClass
    {
        public string value1 { get; set; }
        public ChildClass[] values2 { get; set; }
        public List<ChildClass> children { get; set; }

        public void InitSamples(long size)
        {
            value1 = "abcde";
            values2 = new ChildClass[size];
            children = new List<ChildClass>();

            for(var i = 0; i< size; i++)
            {
                var child = new ChildClass()
                {
                    key = i,
                    values = new string[] { "123", "123", "123" }
                };

                values2[i] = child;

                var child2 = new ChildClass()
                {
                    key = i,
                    values = new string[] { "123", "123", "123" }
                };
                children.Add(child2);
            }
        }
    }

    class ChildClass
    {
        [CopyCollectionKey]
        public long key { get; set; }

        public string[] values { get; set; }
    }




}
