using Dexih.Utils.CopyProperties;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Newtonsoft.Json;

namespace PerformanceCompare
{
    static class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Copy>();  
        }
    }

    [RPlotExporter, RankColumn]
    public class Copy
    {
        SampleClass original = new SampleClass();

        [Params(1000, 10000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
                
            original.InitSamples(50000);
        }

        [Benchmark]
        public void CopyProperties() => original.CloneProperties<SampleClass>();

        [Benchmark]
        public void NewtonSoftCopy()
        {
            var serialized = JsonConvert.SerializeObject(original);
            var searlizedCopy = JsonConvert.DeserializeObject<SampleClass>(serialized);
        }

        [Benchmark]
        public void TextJsonCopy()
        {
            var serialized = System.Text.Json.JsonSerializer.Serialize(original);
            var searlizedCopy = System.Text.Json.JsonSerializer.Deserialize<SampleClass>(serialized);
            
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
