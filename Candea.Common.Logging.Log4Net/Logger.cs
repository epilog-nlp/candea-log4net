/*
This source file is under MIT License (MIT)
Copyright (c) 2017 Mihaela Iridon
https://opensource.org/licenses/MIT
*/

using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Candea.Common.Logging
{

    /// <summary>
    /// The logging implementation class which only redirects the log calls to the log4net logger.
    /// This was added in order to separate all projects (Except this one) from the logging technology used (log4net).
    /// Note: all logging takes place on separate threads, to improve performance (configuration-based option)
    /// </summary>
    public class Logger : ILogger
    {
        /// <summary>
        /// The backing log4net logger implementation.
        /// </summary>
        protected ILog Logr = null;

        /// <summary>
        /// Instantiates a new <see cref="Logger"/> with the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name"></param>
        public Logger(string name)
        {
            Logr = LogManager.GetLogger(name);
        }

        /// <summary>
        /// Instantiates a new <see cref="Logger"/> identified by the provided <see cref="Type"/>.
        /// </summary>
        /// <param name="t"></param>
        public Logger(Type t)
        {
            Logr = LogManager.GetLogger(t);
        }

        /// <summary>
        /// Logs a Trace <paramref name="message"/> with an optional <paramref name="exception"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to log details from.</param>
        public void Trace(object message, Exception exception = null) =>
            Execute(() => Logr.Debug(message, exception));

        /// <summary>
        /// Logs a Debug <paramref name="message"/> with an optional <paramref name="exception"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to log details from.</param>
        public void Debug(object message, Exception exception = null) =>
            Execute(() => Logr.Debug(message, exception));

        /// <summary>
        /// Logs an Info <paramref name="message"/> with an optional <paramref name="exception"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to log details from.</param>
        public void Info(object message, Exception exception = null) =>
            Execute(() => Logr.Info(message, exception));

        /// <summary>
        /// Logs a Warn <paramref name="message"/> with an optional <paramref name="exception"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to log details from.</param>
        public void Warn(object message, Exception exception = null) =>
            Execute(() => Logr.Warn(message, exception));

        /// <summary>
        /// Logs an Error <paramref name="message"/> with an optional <paramref name="exception"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to log details from.</param>
        public void Error(object message, Exception exception = null) =>
            Execute(() => Logr.Error(message, exception));

        /// <summary>
        /// Logs a Fatal <paramref name="message"/> with an optional <paramref name="exception"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to log details from.</param>
        public void Fatal(object message, Exception exception = null) =>
            Execute(() => Logr.Fatal(message, exception));

        /// <summary>
        /// Logs a general <paramref name="message"/> with an optional <paramref name="exception"/> using the provided level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to log details from.</param>
        /// <param name="level">The logging level. <see cref="LogLevel.Trace"/> by default.</param>
        public void Log(object message, Exception exception = null, LogLevel level = LogLevel.Trace) =>
            Execute(() => _delegates[level](Logr, message, exception));


        private static void Execute(Action a)
        {
            if (LoggingManager.RunOnSeparateThread)
            { 
                Task.Factory.StartNew(a);
            }
            else
            {
                a();
            }
        }

        private static readonly IDictionary<LogLevel, Action<ILog, object, Exception>> _delegates = 
            new ReadOnlyDictionary<LogLevel, Action<ILog, object, Exception>>(
                new Dictionary<LogLevel, Action<ILog, object, Exception>>
                {
                    [LogLevel.Trace] = (l,m,e) => l.Debug(m,e),
                    [LogLevel.Debug] = (l, m, e) => l.Debug(m, e),
                    [LogLevel.Info] = (l, m, e) => l.Info(m, e),
                    [LogLevel.Warn] = (l, m, e) => l.Warn(m, e),
                    [LogLevel.Error] = (l, m, e) => l.Error(m, e),
                    [LogLevel.Fatal] = (l, m, e) => l.Fatal(m, e)
                });
    }
}
