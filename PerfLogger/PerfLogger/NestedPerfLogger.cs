using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace PerfLogger
{
    public class NestedPerfLogger : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        // --IDisposable.Dispose----------------------------------------------------------------------------------------------------------------
        public void Dispose()
        {
            foreach (KeyValuePair<String, PerfEntry> peThis in _perfStack)
            {
                peThis.Value.StopWatch.Stop();
            }

            _perfStack.Clear();
        }

        // --Internal Perf. Config Entity----------------------------------------------------------------------------------------------------------------
        private class PerfConfig
        {
            // --Config. Flags-------------------------------------------------------------------------------------------------------------------------------
            public Boolean DoLogTime { get; set; }
            public Boolean DoLogLevel { get; set; }
            public Boolean DoLogIndicator { get; set; }
            public Boolean DoLogAction { get; set; }
            public Boolean DoLogMeasure { get; set; }
            public Boolean DoLogMessage { get; set; }
            public Boolean DoLogData { get; set; }
            public String Delimiter { get; set; }
            public String DataDelimiter { get; set; }
            public String NsDelimiter { get; set; }
            public Boolean FixedColumns { get; set; }

            public PerfConfig()
            {
                Configure();
            }

            public void Configure(Boolean doLogTime = false, Boolean doLogLevel = false, Boolean doLogIndicator = false,
                Boolean doLogAction = false, Boolean doLogMeasure = false, Boolean doLogMessage = false,
                Boolean doLogData = false, String deLimiter = ",", String dataDelimiter = "|", String nsDelimiter = ".",
                Boolean fixedColumns = false)
            {
                DoLogTime = doLogTime;
                DoLogLevel = doLogLevel;
                DoLogIndicator = doLogIndicator;
                DoLogAction = doLogAction;
                DoLogMeasure = doLogMeasure;
                DoLogMessage = doLogMessage;
                DoLogData = doLogData;
                Delimiter = deLimiter;
                DataDelimiter = dataDelimiter;
                NsDelimiter = nsDelimiter;
                FixedColumns = fixedColumns;
            }
        }

        // --Internal Perf. Entry Entity----------------------------------------------------------------------------------------------------------------
        private class PerfEntry
        {
            // --For Future Use--
            public Object[] Data { get; set; }
            public Stopwatch StopWatch { get; set; }

            public PerfEntry(params Object[] args)
            {
                Data = args;
                StopWatch = Stopwatch.StartNew();
            }
        }

        private readonly Stack<KeyValuePair<String, PerfEntry>> _perfStack;
        private readonly PerfConfig _config;

        // --Private Methods-----------------------------------------------------------------------------------------------------------------------------
        private String InternalGetStringOnCondition(String msgData, Boolean conditionalExpression = true)
        {
            // If we want the log to be printed in fixed columns, don't do null/empty checks
            Boolean fixedExpression = (_config.FixedColumns) ? true : !String.IsNullOrEmpty(msgData);
            String result = fixedExpression && conditionalExpression ? msgData : "";
            return (result);
        }

        private String TicksToMS()
        {
            return (new TimeSpan(DateTime.UtcNow.Ticks)).TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        }

        private String InternalLog(KeyValuePair<string, PerfEntry> peThis, String logAction = "info",
            String logMessage = "")
        {
            String result = "";
            if (_perfStack.Count > 0)
            {
                String msgPrefix = InternalGetPrefix();
                //
                String msgOutput = String.Join(_config.Delimiter,
                    InternalGetStringOnCondition(msgPrefix),
                    InternalGetStringOnCondition(peThis.Key),
                    InternalGetStringOnCondition(logAction, _config.DoLogAction),
                    InternalGetStringOnCondition(peThis.Value.StopWatch.ElapsedMilliseconds.ToString(),
                        _config.DoLogMeasure),
                    // Log message and data are always enclosed in quotes to help better parsing
                    InternalGetStringOnCondition(logMessage, _config.DoLogMessage),
                    InternalGetStringOnCondition("\"" + String.Join(_config.DataDelimiter, peThis.Value.Data) + "\"",
                        _config.DoLogData && peThis.Value.Data != null)
                );
                //
                result = msgOutput;
            }

            return (result);
        }

        private String InternalGetPrefix()
        {
            return String.Join(_config.Delimiter,
                // Timestamp with milliseconds
                InternalGetStringOnCondition(DateTime.Now.ToString("hh.mm.ss.fff")),
                // Nested Levels as a visual form : ex-> "->, -->, --->"
                InternalGetStringOnCondition(new String('-', _perfStack.Count) + ">"),
                // Nested level in number : ex: 1, 2, 3 ...
                InternalGetStringOnCondition($"{_perfStack.Count:D4}", _perfStack.Count > 0)
            );
        }

        // --Public Methods-----------------------------------------------------------------------------------------------------------------------------
        public NestedPerfLogger()
        {
            _perfStack = new Stack<KeyValuePair<String, PerfEntry>>();
            _config = new PerfConfig();
            _config.Configure(true, true, true, true, true, true, true);
        }

        public String StartMeasure(string key = "", String logMessage = "", params object[] args)
        {
            // Create a unique key concatinating the parent key using a namespaced
            String result =
                _perfStack.Count > 0
                    ? InternalGetStringOnCondition(_perfStack.Peek().Key + _config.NsDelimiter + key)
                    : (String.IsNullOrEmpty(key) ? TicksToMS() : key);

            KeyValuePair<string, PerfEntry> peThis = new KeyValuePair<string, PerfEntry>(result, new PerfEntry(args));
            //
            _perfStack.Push(peThis);
            result = InternalLog(peThis, "Start", logMessage);
            //
            return (result);
        }

        public String StopMeasure(String logMessage = "")
        {
            String result = "";
            //
            if (_perfStack.Count > 0)
            {
                KeyValuePair<string, PerfEntry> peThis = _perfStack.Peek();
                peThis.Value.StopWatch.Stop();
                result = InternalLog(peThis, "Stop", logMessage);
                _perfStack.Pop();
            }

            //
            return (result);
        }

        public String LogMeasure(String logMessage = "")
        {
            String result = "";
            //
            if (_perfStack.Count > 0)
            {
                KeyValuePair<string, PerfEntry> peThis = _perfStack.Peek();
                result = InternalLog(peThis, "Log", logMessage);
            }

            //
            return (result);
        }

        public String LogHeaders()
        {
            return String.Join(_config.Delimiter,
                new String[]
                    {"TimeStamp", "Level Indicator", "Level", "Key", "Action", "Elapsed Time", "Log Message", "Data"});
        }

        public static void Test()
        {
            NestedPerfLogger perfLogger = new NestedPerfLogger();
            //
            Console.Out.WriteLine(perfLogger.LogHeaders());
            Console.Out.WriteLine(new String('=', 80));
            // Simplest measure - start, stop & log
            Console.Out.WriteLine(perfLogger.StartMeasure(), "Testing for 100 ms");
            Console.Out.WriteLine(perfLogger.LogMeasure("Intermediate Log Message #1"));
            Thread.Sleep(100);
            Console.Out.WriteLine(perfLogger.LogMeasure("Intermediate Log Message #2"));
            Console.Out.WriteLine(perfLogger.StopMeasure("Testing for 100 ms"));
            Console.Out.WriteLine(new String('=', 80));

            // Simplest measure - start, stop - w custom key & log message
            Console.Out.WriteLine(perfLogger.StartMeasure("Key", "Testing 50 ms w Key"));
            Thread.Sleep(50);
            Console.Out.WriteLine(perfLogger.StopMeasure("Testing 50 ms w Key"));
            Console.Out.WriteLine(new String('=', 80));

            // Simplest measure - start, stop - w custom key, log message & data
            Console.Out.WriteLine(
                perfLogger.StartMeasure("Product", "Testing for 50 ms w Addl. data", 123, "key-value"));
            Thread.Sleep(50);
            Console.Out.WriteLine(perfLogger.StopMeasure("Testing for 50 ms w Addl. data"));
            Console.Out.WriteLine(new String('=', 80));

            // Nested measure - start, stop - w custom key, log message & data
            Console.Out.WriteLine(perfLogger.StartMeasure("Product",
                "Testing for 150 ms w Addl. data and Nested Measurements", "Shoe"));
            Thread.Sleep(50);
            Console.Out.WriteLine(perfLogger.StartMeasure("Item",
                "Testing for 150 ms w Addl. data and Nested Measurements", "Steve Madden Men's Jagwar"));
            Thread.Sleep(50);
            Console.Out.WriteLine(perfLogger.StartMeasure("Node",
                "Testing for 150 ms w Addl. data and Nested Measurements", "Men's Shoes"));
            Console.Out.WriteLine(perfLogger.StartMeasure("", "Node-Nested", 3));
            Thread.Sleep(50);
            Console.Out.WriteLine(perfLogger.LogMeasure("done"));
            Console.Out.WriteLine(perfLogger.StopMeasure("done"));
            Console.Out.WriteLine(perfLogger.StopMeasure("done"));
            Console.Out.WriteLine(perfLogger.StopMeasure("done"));
            Console.Out.WriteLine(new String('=', 80));
        }
    }
}