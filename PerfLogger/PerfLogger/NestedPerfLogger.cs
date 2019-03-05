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
        /// </summary>
        private readonly PerfConfig _config;

        /// <summary>
        /// </summary>
        private readonly Stack<KeyValuePair<string, PerfEntry>> _perfStack;

        // --Public Methods-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        public NestedPerfLogger()
        {
            _perfStack = new Stack<KeyValuePair<string, PerfEntry>>();
            _config = new PerfConfig();
            _config.Configure(true, true, true, true, true, true, true);
        }

        // --IDisposable.Dispose----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        public void Dispose()
        {
            foreach (var peThis in _perfStack) peThis.Value.StopWatch.Stop();

            _perfStack.Clear();
        }

        // --Private Methods-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <param name="msgData"></param>
        /// <param name="conditionalExpression"></param>
        /// <returns></returns>
        private string InternalGetStringOnCondition(string msgData, bool conditionalExpression = true)
        {
            // If we want the log to be printed in fixed columns, don't do null/empty checks
            var fixedExpression = _config.FixedColumns ? true : !string.IsNullOrEmpty(msgData);
            var result = fixedExpression && conditionalExpression ? msgData : "";
            return result;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private string TicksToMS()
        {
            return new TimeSpan(DateTime.UtcNow.Ticks).TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// </summary>
        /// <param name="peThis"></param>
        /// <param name="logAction"></param>
        /// <param name="logMessage"></param>
        /// <returns></returns>
        private string InternalLog(KeyValuePair<string, PerfEntry> peThis, string logAction = "info",
            string logMessage = "")
        {
            var result = "";
            if (_perfStack.Count > 0)
            {
                var msgPrefix = InternalGetPrefix();
                //
                var msgOutput = string.Join(_config.Delimiter,
                    InternalGetStringOnCondition(msgPrefix),
                    InternalGetStringOnCondition(peThis.Key),
                    InternalGetStringOnCondition(logAction, _config.DoLogAction),
                    InternalGetStringOnCondition(peThis.Value.StopWatch.ElapsedMilliseconds.ToString(),
                        _config.DoLogMeasure),
                    // Log message and data are always enclosed in quotes to help better parsing
                    InternalGetStringOnCondition(logMessage, _config.DoLogMessage),
                    InternalGetStringOnCondition("\"" + string.Join(_config.DataDelimiter, peThis.Value.Data) + "\"",
                        _config.DoLogData && peThis.Value.Data != null)
                );
                //
                result = msgOutput;
            }

            return result;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private string InternalGetPrefix()
        {
            return string.Join(_config.Delimiter,
                // Timestamp with milliseconds
                InternalGetStringOnCondition(DateTime.Now.ToString("hh.mm.ss.fff")),
                // Nested Levels as a visual form : ex-> "->, -->, --->"
                InternalGetStringOnCondition(new string('-', _perfStack.Count) + ">"),
                // Nested level in number : ex: 1, 2, 3 ...
                InternalGetStringOnCondition($"{_perfStack.Count:D4}", _perfStack.Count > 0)
            );
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="logMessage"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public string StartMeasure(string key = "", string logMessage = "", params object[] args)
        {
            // Create a unique key concatinating the parent key using a namespaced
            var result =
                _perfStack.Count > 0
                    ? InternalGetStringOnCondition(_perfStack.Peek().Key + _config.NsDelimiter + key)
                    : string.IsNullOrEmpty(key)
                        ? TicksToMS()
                        : key;

            var peThis = new KeyValuePair<string, PerfEntry>(result, new PerfEntry(args));
            //
            _perfStack.Push(peThis);
            result = InternalLog(peThis, "Start", logMessage);
            //
            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="logMessage"></param>
        /// <returns></returns>
        public string StopMeasure(string logMessage = "")
        {
            var result = "";
            //
            if (_perfStack.Count > 0)
            {
                var peThis = _perfStack.Peek();
                peThis.Value.StopWatch.Stop();
                result = InternalLog(peThis, "Stop", logMessage);
                _perfStack.Pop();
            }

            //
            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="logMessage"></param>
        /// <returns></returns>
        public string LogMeasure(string logMessage = "")
        {
            var result = "";
            //
            if (_perfStack.Count > 0)
            {
                var peThis = _perfStack.Peek();
                result = InternalLog(peThis, "Log", logMessage);
            }

            //
            return result;
        }

        /// <summary>
        /// LogHeaders is used to identify the names of columns used in the log.
        /// Todo: @Developers : Whenever a new log column is added, make sure to include the column name in the right position here!
        /// </summary>
        /// <returns>Delimeter (",") separated names</returns>
        public string LogHeaders()
        {
            return string.Join(_config.Delimiter, "TimeStamp", "Level Indicator", "Level", "Key", "Action",
                "Elapsed Time", "Log Message", "Data");
        }

        /// <summary>
        /// Test method/playground, to validate if NestedPerfLogger is working as expected.
        /// </summary>
        public static void Test()
        {
            NestedPerfLogger perfLogger = new NestedPerfLogger();
            //
            Console.Out.WriteLine(perfLogger.LogHeaders());
            Console.Out.WriteLine(new string('=', 80));
            // Simplest measure - start, stop & log
            Console.Out.WriteLine(perfLogger.StartMeasure(), "Testing for 100 ms");
            Console.Out.WriteLine(perfLogger.LogMeasure("Intermediate Log Message #1"));
            Thread.Sleep(100);
            Console.Out.WriteLine(perfLogger.LogMeasure("Intermediate Log Message #2"));
            Console.Out.WriteLine(perfLogger.StopMeasure("Testing for 100 ms"));
            Console.Out.WriteLine(new string('=', 80));

            // Simplest measure - start, stop - w custom key & log message
            Console.Out.WriteLine(perfLogger.StartMeasure("Key", "Testing 50 ms w Key"));
            Thread.Sleep(50);
            Console.Out.WriteLine(perfLogger.StopMeasure("Testing 50 ms w Key"));
            Console.Out.WriteLine(new string('=', 80));

            // Simplest measure - start, stop - w custom key, log message & data
            Console.Out.WriteLine(
                perfLogger.StartMeasure("Product", "Testing for 50 ms w Addl. data", 123, "key-value"));
            Thread.Sleep(50);
            Console.Out.WriteLine(perfLogger.StopMeasure("Testing for 50 ms w Addl. data"));
            Console.Out.WriteLine(new string('=', 80));

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
            Console.Out.WriteLine(perfLogger.StopMeasure("done"));
            Console.Out.WriteLine(perfLogger.StopMeasure("done"));
            Console.Out.WriteLine(perfLogger.StopMeasure("done"));
            Console.Out.WriteLine(perfLogger.StopMeasure("done"));
            Console.Out.WriteLine(new string('=', 80));
        }

        // --Internal Perf. Config Entity----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        private class PerfConfig
        {
            /// <summary>
            /// </summary>
            public PerfConfig()
            {
                Configure();
            }

            /// <summary>
            /// </summary>
            public bool DoLogTime { get; set; }

            /// <summary>
            /// </summary>
            public bool DoLogLevel { get; set; }

            /// <summary>
            /// </summary>
            public bool DoLogIndicator { get; set; }

            /// <summary>
            /// </summary>
            public bool DoLogAction { get; set; }

            /// <summary>
            /// </summary>
            public bool DoLogMeasure { get; set; }

            /// <summary>
            /// </summary>
            public bool DoLogMessage { get; set; }

            /// <summary>
            /// </summary>
            public bool DoLogData { get; set; }

            /// <summary>
            /// </summary>
            public string Delimiter { get; set; }

            /// <summary>
            /// </summary>
            public string DataDelimiter { get; set; }

            /// <summary>
            /// </summary>
            public string NsDelimiter { get; set; }

            /// <summary>
            /// </summary>
            public bool FixedColumns { get; set; }

            /// <summary>
            /// </summary>
            /// <param name="doLogTime"></param>
            /// <param name="doLogLevel"></param>
            /// <param name="doLogIndicator"></param>
            /// <param name="doLogAction"></param>
            /// <param name="doLogMeasure"></param>
            /// <param name="doLogMessage"></param>
            /// <param name="doLogData"></param>
            /// <param name="deLimiter"></param>
            /// <param name="dataDelimiter"></param>
            /// <param name="nsDelimiter"></param>
            /// <param name="fixedColumns"></param>
            public void Configure(bool doLogTime = false, bool doLogLevel = false, bool doLogIndicator = false,
                bool doLogAction = false, bool doLogMeasure = false, bool doLogMessage = false,
                bool doLogData = false, string deLimiter = ",", string dataDelimiter = "|", string nsDelimiter = ".",
                bool fixedColumns = false)
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
        /// <summary>
        /// </summary>
        private class PerfEntry
        {
            /// <summary>
            /// </summary>
            /// <param name="args"></param>
            public PerfEntry(params object[] args)
            {
                Data = args;
                StopWatch = Stopwatch.StartNew();
            }

            /// <summary>
            /// </summary>
            public object[] Data { get; }

            /// <summary>
            /// </summary>
            public Stopwatch StopWatch { get; }
        }
    }
}