/*
This source file is under MIT License (MIT)
Copyright (c) 2017 Mihaela Iridon
https://opensource.org/licenses/MIT
*/

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using static Candea.Common.Util.auxfunc;
using static log4net.Config.XmlConfigurator;
using static System.Diagnostics.Debug;

namespace Candea.Common.Logging
{
    /// <summary>
    /// An implementation of the ILogManager to deal with the log4net logging framework; specifically the initialization and shutting down of the logger
    /// </summary>
    [Export(typeof(ILogManager))]
    public class LoggingManager : ILogManager, IDisposable
    {
        internal static readonly bool RunOnSeparateThread = false;
        private const string LogConfig = "log4net.config";

        static LoggingManager()
        {
            var crtConfigFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            var crtConfigFilePath = Path.GetDirectoryName(crtConfigFile);
            var crtConfigFileName = Path.GetFileName(crtConfigFile);
            var l4nc = crtConfigFileName.Replace("dll", "log4net");
            var asm = Assembly.GetEntryAssembly();
            var asmName = asm?.GetName().Name ?? l4nc;

            var headerInsert = asmName.Equals("web.config", StringComparison.InvariantCultureIgnoreCase)
                ? "Web Application" : asmName;

            var header = appSetting("Logging.Header", $"_____________ {headerInsert} _____________");
            var configFileName = CheckLog4NetFileOrCreate(asmName, crtConfigFilePath);

            RunOnSeparateThread = appSetting("Logging.RunOnSeparateThread", true);
            try
            {
                if (!string.IsNullOrEmpty(configFileName))
                {
                    var configFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, configFileName));
                    if (configFile.Exists)
                        Configure(configFile);
                }
                else
                    Configure();
            }
            catch(Exception e)
            {
                WriteLine($"ERROR: Could not initialize logging: {e.Message}", e);
                return;
            }

            _localLogger = log4net.LogManager.GetLogger(typeof(LoggingManager));
            _localLogger.Info(header);
            _localLogger.Debug($"Logging messages on separate thread={RunOnSeparateThread}");
        }

        /// <summary>
        /// Produces a new <see cref="ILogger"/> implementation with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the logger to create.</param>
        /// <returns>An <see cref="ILogger"/> implementation with the given <paramref name="name"/>.</returns>
        public static ILogger Logger(string name) => new Logger(name) as ILogger;

        /// <summary>
        /// Produces a new <see cref="ILogger"/> implementation with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the logger to create.</param>
        /// <returns>An <see cref="ILogger"/> implementation with the given <paramref name="name"/>.</returns>
        public ILogger GetLogger(string name) => Logger(name);
        
        /// <summary>
        /// Produces a new <see cref="ILogger"/> implementation identified by the <see cref="Type"/> of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Used to identify the logging source.</typeparam>
        /// <returns>An <see cref="ILogger"/> implementation.</returns>
        public static ILogger Logger<T>() where T : class => new Logger(typeof(T)) as ILogger;

        /// <summary>
        /// Produces a new <see cref="ILogger"/> implementation identified by the <see cref="Type"/> of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Used to identify the logging source.</typeparam>
        /// <returns>An <see cref="ILogger"/> implementation.</returns>
        public ILogger GetLogger<T>() where T : class => Logger<T>();

        /// <summary>
        /// Produces a new <see cref="ILogger"/> implementation identified by the provided <paramref name="type"/>.
        /// </summary>
        /// <returns>An <see cref="ILogger"/> implementation.</returns>
        public static ILogger Logger(Type type) => new Logger(type) as ILogger;

        /// <summary>
        /// Produces a new <see cref="ILogger"/> implementation identified by the provided <paramref name="type"/>.
        /// </summary>
        /// <returns>An <see cref="ILogger"/> implementation.</returns>
        public ILogger GetLogger(Type type) => Logger(type);

        /// <summary>
        /// Closes the backing logging implementation.
        /// </summary>
        public static void ShutdownLogger() => LogAndShutdownLogger();

        /// <summary>
        /// Closes the backing logging implementation.
        /// </summary>
        public void Shutdown() => ShutdownLogger();

        /// <summary>
        /// Closes the backing logging implementation.
        /// </summary>
        public void Dispose() => ShutdownLogger();


        private static readonly log4net.ILog _localLogger;

        private static void LogAndShutdownLogger()
        {
            _localLogger.Info(">>>>> Shutting down log4net Logger. BYE.");
            log4net.LogManager.Shutdown();
        }

        private static string ReadLog4NetConfigEmbeddedResourceFile()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName =  $"Shared.Frameworks.Logging.Log4Net.{LogConfig}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }

        private static string CheckLog4NetFileOrCreate(string execAsm, string path)
        {
            bool isWebConfig = execAsm.Equals("web.config", StringComparison.InvariantCultureIgnoreCase);

            var configFileName = appSetting<string>("Logging.ConfigFilePath");
            if (configFileName == null || !File.Exists(configFileName))
            {
                configFileName = Path.Combine(path, isWebConfig ? LogConfig : execAsm);

                if (!File.Exists(configFileName))
                {
                    //replace "DefaultLoggingService" in the local file with execAsm.log4net.config and create a copy naming it like that
                    var s = ReadLog4NetConfigEmbeddedResourceFile();
                    if (s == null)
                    {
                        return null;
                    }
                    var r = s.Replace("DefaultLoggingService.", isWebConfig ? "WebAppLogger." : execAsm.Replace(LogConfig, string.Empty));
                    File.WriteAllText(configFileName, r);
                }
            }
            return configFileName;
        }
    }

}
