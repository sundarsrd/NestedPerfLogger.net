using System;
using System.Threading;
using NUnit.Framework;
using PerfLogger;

namespace PerfLoggerTest
{
    /// <summary>
    /// 
    /// </summary>
    [TestFixture]
    public class Tests
    {
                
        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void TestNestedPerfLogger()
        {
            NestedPerfLogger perfLogger = new NestedPerfLogger();
            Console.Out.WriteLine(perfLogger.LogHeaders());
            //
            // NestedPerfLogger.Test();
            // Nested measure - start, stop - w custom key, log message & data
            Console.Out.WriteLine(perfLogger.StartMeasure("Product",
                "Testing for 150 ms w Addl. data and Nested Measurements", "Shoe"));
            Thread.Sleep(150);
            Console.Out.WriteLine(perfLogger.StartMeasure("Item",
                "Testing for 150 ms w Addl. data and Nested Measurements", "Steve Madden Men's Jagwar"));
            Thread.Sleep(150);
            Console.Out.WriteLine(perfLogger.StartMeasure("Node",
                "Testing for 150 ms w Addl. data and Nested Measurements", "Men's Shoes"));
            Console.Out.WriteLine(perfLogger.StartMeasure("", "Node-Nested", 3));
            Thread.Sleep(150);
            Console.Out.WriteLine(perfLogger.StopMeasure("done 4"));
            Console.Out.WriteLine(perfLogger.StopMeasure("done 3"));
            Console.Out.WriteLine(perfLogger.StopMeasure("done - Steve Madden Men's Jagwar"));
            Console.Out.WriteLine(perfLogger.StopMeasure("done - Shoe"));
            Console.Out.WriteLine(new string('=', 80));            
            //
            Assert.True(String.Equals(perfLogger.LogHeaders(),"TimeStamp,Level Indicator,Level,Key,Action,Elapsed Time,Log Message,Data",StringComparison.InvariantCultureIgnoreCase));
        }
    }
}