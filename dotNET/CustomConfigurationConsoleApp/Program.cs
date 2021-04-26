using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CustomConfiguration
{
    class Program
    {
        static void Main(string[] args)
        {
            Sample_01();
            Sample_02();
            Sample_03_JSON();
            Sample_04_JSON_ENV();
        }

        //TODO: description
        private static void Sample_01()
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

        private static void Sample_02()
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
}
