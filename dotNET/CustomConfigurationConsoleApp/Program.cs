using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CustomConfiguration
{
    class Program
    {
        static void Main(string[] args)
        {
            Sample_01_Basic_access();
            Sample_02_Binding_to_configuration();
            Sample_03_JSON();
            Sample_04_JSON_ENV();
            Sample_05_Custom_provider();
        }

        /// <summary>
        /// Initial sample how to configure and access configuration values as string
        /// </summary>
        private static void Sample_01_Basic_access()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                {"MySetting", "Setting from in memory"},
                {"Subsection:MyIntValue", "4"}
            };
            
            var configBuilder = new ConfigurationBuilder()
                    .AddInMemoryCollection(inMemorySettings)
                    //Prefix is used to prevent name collisions between environment variables from different sources.
                    //DOTNET_ is used as prefix in default configuration for WebHost and Generic Host
                    .AddEnvironmentVariables("DOTNET_");

            var configurationRoot = configBuilder.Build();
            //Don't forget to dispose configuration root, if it is a disposable.
            using var configDispose = configurationRoot as IDisposable;

            //This will be stored in memory only
            configurationRoot["MySetting"] = "Setting from code";

            var mySetting = configurationRoot["MySetting"];
            var mySubsectionSetting = configurationRoot["Subsection:MyIntValue"];
            int.TryParse(mySubsectionSetting,
                out var myIntValue);
            
            //var mySubsection = configurationRoot.GetSection("Subsection");
            //var mySubsectionSetting = mySubsection["MyIntValue"];
            //int.TryParse(mySubsectionSetting,
            //    out var myIntValue);

            Console.WriteLine($"Using my setting: {mySetting}");
            Console.WriteLine($"Using my subsection setting: {mySubsectionSetting} => parsed value {myIntValue}");
        }

        /// <summary>
        /// Improving on the basic sample to add binding to an object
        /// </summary>
        private static void Sample_02_Binding_to_configuration()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                {"MySetting", "Setting from in memory"},
                {"Subsection:MyIntValue", "4"}
            };

            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .AddEnvironmentVariables("DOTNET_");

            var configurationRoot = configBuilder.Build();
            using var configDispose = configurationRoot as IDisposable;

            //var settings = new MySettings();
            //configurationRoot.Bind(settings); //You can provide key to Bind in order to bind to a section
            var settings = configurationRoot.Get<MySettings>();

            Console.WriteLine($"Using my setting: {settings.MySetting}");
            Console.WriteLine($"Using my subsection setting: parsed value {settings.Subsection.MyIntValue}");
        }

        /// <summary>
        /// Initial sample for reading configuration out of JSON file
        /// </summary>
        private static void Sample_03_JSON()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var configurationRoot = configBuilder.Build();
            using var configDispose = configurationRoot as IDisposable;
            
            var settings = configurationRoot.Get<MySettings>();

            Console.WriteLine($"Using my setting: {settings.MySetting}");
            Console.WriteLine($"Using my subsection setting: parsed value {settings.Subsection.MyIntValue}");
        }

        /// <summary>
        /// Improving on the JSON sample to replicate the staged read as is found in default configuration for
        /// WebHost in ASP.NET Core
        /// </summary>
        private static void Sample_04_JSON_ENV()
        {
            var environmentConfigBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables("DOTNET_");
            var environmentConfigRoot = environmentConfigBuilder.Build();
            using var environmentConfigDispose = environmentConfigRoot as IDisposable;
            //Could use plain string instead of HostDefaults.EnvironmentKey - but this keeps it consistent with hosting
            //abstractions in Microsoft.Extensions.Hosting.Abstractions
            var environmentName = environmentConfigRoot[HostDefaults.EnvironmentKey];

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

            var configurationRoot = configBuilder.Build();
            using var configDispose = configurationRoot as IDisposable;

           var settings = configurationRoot.Get<MySettings>();

            Console.WriteLine($"Using my setting: {settings.MySetting}");
            Console.WriteLine($"Using my subsection setting: parsed value {settings.Subsection.MyIntValue}");
        }

        /// <summary>
        /// Configuring and using custom provider for command line arguments
        /// </summary>
        private static void Sample_05_Custom_provider()
        {
            var args = "-s someSetting -i 1 --intArray 12 34 23".Split();
            var result = Parser.Default.ParseArguments<CommandlineOption>(args);
            result.WithParsed(ParseSuccess)
                .WithNotParsed(Console.WriteLine);
        }

        private static void ParseSuccess(CommandlineOption option)
        {
            var configBuilder = new ConfigurationBuilder()
                .Add(new CommandLineSource(option));

            var configurationRoot = configBuilder.Build();
            using var configDispose = configurationRoot as IDisposable;

            var settings = configurationRoot.Get<MySettings>();

            Console.WriteLine($"Using my setting: {settings.MySetting}");
            Console.WriteLine($"Using my subsection setting: parsed value {settings.Subsection.MyIntValue}");
        }
    }

    public class MySettings
    {
        public string MySetting { get; set; }

        public MySubsection Subsection { get; set; }
    }

    public class MySubsection
    {
        public int MyIntValue { get; set; }

        public int[] IntArray { get; set; }
    }

    public class CommandlineOption
    {
        [Option('s', "setting")]
        public string MySetting { get; set; }

        [Option('i', "intValue")]
        public string MyIntValue { get; set; }

        [Option("intArray")]
        public IEnumerable<int> MyIntArray { get; set; } = Array.Empty<int>();
    }

    public class CommandLineSource : IConfigurationSource
    {
        private readonly CommandlineOption _option;

        public CommandLineSource(CommandlineOption option)
        {
            _option = option;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new CommandLineProvider(_option);
        }
    }

    public class CommandLineProvider : ConfigurationProvider
    {
        private readonly CommandlineOption _option;

        public CommandLineProvider(CommandlineOption option)
        {
            _option = option;
        }

        public override void Load()
        {
            var data = new Dictionary<string, string>
            {
                [nameof(MySettings.MySetting)] = _option.MySetting,
                [$"{nameof(MySettings.Subsection)}:{nameof(MySubsection.MyIntValue)}"] = _option.MyIntValue
            };


            var subsectionArray = _option.MyIntArray.ToArray();
            for (var i = 0; i < subsectionArray.Length; i++)
            {
                data[$"{nameof(MySettings.Subsection)}:{nameof(MySubsection.IntArray)}:{i}"] = subsectionArray[i].ToString();
            }

            Data = data.Where(kv => !string.IsNullOrEmpty(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}
