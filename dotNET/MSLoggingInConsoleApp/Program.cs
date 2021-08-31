using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MSLoggingInConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Sample_01();
            Sample_02_unfiltered();
            Sample_02_filter_rules_in_code();
            Sample_02_filter_rules_in_config_file();
            Sample_03_different_ways_to_log();
            Sample_04_Adding_file_logging_with_Serilog();
            Sample_05_Using_custom_logger();
        }

        private static void Sample_01()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            var logger = loggerFactory.CreateLogger("MyCategoryName");
            logger.LogInformation("Hello, world!");
            
            //Alternative to above extension method.
            //logger.Log(LogLevel.Information, "Hello World!");
        }

        private static void Sample_02_unfiltered()
        {
            const string categoryName = "MyCategoryName";
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            var logger = loggerFactory.CreateLogger(categoryName);
            
            logger.LogInformation("Some log information output");
            logger.LogWarning("Some log warning output");
            logger.LogError("Some log error output");
        }

        private static void Sample_02_filter_rules_in_code()
        {
            const string categoryName = "MyCategoryName";
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                //Default rule
                builder.AddFilter(l => l >= LogLevel.Warning);
                //A specific rule for our category - this will override the default rule.
                //builder.AddFilter(categoryName, LogLevel.Information);
                //This overrides the category filter for console logger, since it is even more specific
                //builder.AddFilter<ConsoleLoggerProvider>(categoryName, LogLevel.Error);
            });

            var logger = loggerFactory.CreateLogger(categoryName);

            logger.LogInformation("Some log information output");
            logger.LogWarning("Some log warning output");
            logger.LogError("Some log error output");
        }

        private static void Sample_02_filter_rules_in_config_file()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var configurationRoot = configBuilder.Build();
            using var configDispose = configurationRoot as IDisposable;

            const string categoryName = "MyCategoryName";
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                var loggingConfig = configurationRoot.GetSection("Logging");
                builder.AddConfiguration(loggingConfig);
                builder.AddConsole();
                builder.AddDebug();
            });

            var logger = loggerFactory.CreateLogger(categoryName);

            logger.LogInformation("Some log information output");
            logger.LogWarning("Some log warning output");
            logger.LogError("Some log error output");
        }

        private static void Sample_03_different_ways_to_log()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(co =>
                {
                    //To display scopes, they need to be enabled
                    co.IncludeScopes = true;
                    co.TimestampFormat = "[HH:mm:ss] ";
                });
                //Use JSON console to showcase structured logging 
                //builder.AddJsonConsole();
            });
            
            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogInformation("Log info with time stamp");

            try
            {
                ThrowTestException();
            }
            catch (Exception exception)
            {
                //For exceptions use exception overload
                logger.LogInformation(exception, "Writing an exception");
            }

            logger.LogInformation(MyEventIds.HelloWorld, "Writing with event ID");

            //using scopes - note that scopes must be enabled in configuration. In this sample it is done in appsettings.
            using (logger.BeginScope("scope message"))
            {
                logger.LogInformation("My scope log entry");
            }

            //structured logging
            var someId = 10;
            var someType = "structured";
            logger.LogInformation("Using structured logging with id {id} and {type}", someId, someType);
        }

        //Using this method, so that there is an exception stack that can be displayed in the log.
        private static void ThrowTestException()
        {
            throw new Exception("My exception message");
        }

        private static void Sample_04_Adding_file_logging_with_Serilog()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var configurationRoot = configBuilder.Build();
            using var configDispose = configurationRoot as IDisposable;

            using var loggerFactory = LoggerFactory.Create(lb =>
            {
                var loggingConfig = configurationRoot.GetSection("Logging");
                lb.AddConfiguration(loggingConfig);
                lb.AddFile(loggingConfig);
            });

            var logger = loggerFactory.CreateLogger<Program>();
            
            logger.LogError("Hello, world!"); //Disposes too quickly in order to write something?

            Task.Delay(1000).Wait();
        }

        private static void Sample_05_Using_custom_logger()
        {
            var sb = new StringBuilder();
            using var loggerFactory = LoggerFactory.Create(lb =>
            {
                var provider = new WriteActionLoggerProvider(m => sb.AppendLine(m));
                lb.AddProvider(provider);
            });

            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogError("Hello, world!");

            Console.Write(sb.ToString());
        }
    }

    public static class MyEventIds
    {
        public const int HelloWorld = 10;
    }

    public class WriteActionLoggerProvider : ILoggerProvider
    {
        private readonly Action<string> _writeTarget;

        public WriteActionLoggerProvider(Action<string> writeTarget)
        {
            _writeTarget = writeTarget;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new WriteActionLogger(categoryName, _writeTarget);
        }

        public void Dispose()
        {
            //Empty
        }
    }

    public class WriteActionLogger : ILogger
    {
        private readonly string _name;
        private readonly Action<string> _writeTarget;

        public WriteActionLogger(string name, Action<string> writeTarget)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _writeTarget = writeTarget ?? throw new ArgumentNullException(nameof(writeTarget));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (message != null)
            {
                if (exception != null)
                {
                    message += Environment.NewLine + "Exception message: " + exception.Message + Environment.NewLine + exception.StackTrace;
                }

                message = $"[{_name}] {message}";

                _writeTarget(message);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
    }

    public class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope()
        {
        }

        public void Dispose()
        {
        }
    }
}
