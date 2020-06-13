using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Common.Configuration;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Migration to initialize the user logging configuration file "logging.user.json".
    /// If the deprecated logging.json file exists and has a custom config, it will be used as logging.user.json,
    /// otherwise a blank file will be created.
    /// </summary>
    internal class CreateUserLoggingConfigFile : IMigrationRoutine
    {
        /// <summary>
        /// File history for logging.json as existed during this migration creation. The contents for each has been minified.
        /// </summary>
        private readonly List<string> _defaultConfigHistory = new List<string>
        {
            // 9a6c27947353585391e211aa88b925f81e8cd7b9
            @"{""Serilog"":{""MinimumLevel"":{""Default"":""Information"",""Override"":{""Microsoft"":""Warning"",""System"":""Warning""}},""WriteTo"":[{""Name"":""Console"",""Args"":{""outputTemplate"":""[{Timestamp:HH:mm:ss}] [{Level:u3}] [{ThreadId}] {SourceContext}: {Message:lj}{NewLine}{Exception}""}},{""Name"":""Async"",""Args"":{""configure"":[{""Name"":""File"",""Args"":{""path"":""%JELLYFIN_LOG_DIR%//log_.log"",""rollingInterval"":""Day"",""retainedFileCountLimit"":3,""rollOnFileSizeLimit"":true,""fileSizeLimitBytes"":100000000,""outputTemplate"":""[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{ThreadId}] {SourceContext}: {Message}{NewLine}{Exception}""}}]}}],""Enrich"":[""FromLogContext"",""WithThreadId""]}}",
            // 71bdcd730705a714ee208eaad7290b7c68df3885
            @"{""Serilog"":{""MinimumLevel"":""Information"",""WriteTo"":[{""Name"":""Console"",""Args"":{""outputTemplate"":""[{Timestamp:HH:mm:ss}] [{Level:u3}] [{ThreadId}] {SourceContext}: {Message:lj}{NewLine}{Exception}""}},{""Name"":""Async"",""Args"":{""configure"":[{""Name"":""File"",""Args"":{""path"":""%JELLYFIN_LOG_DIR%//log_.log"",""rollingInterval"":""Day"",""retainedFileCountLimit"":3,""rollOnFileSizeLimit"":true,""fileSizeLimitBytes"":100000000,""outputTemplate"":""[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{ThreadId}] {SourceContext}: {Message}{NewLine}{Exception}""}}]}}],""Enrich"":[""FromLogContext"",""WithThreadId""]}}",
            // a44936f97f8afc2817d3491615a7cfe1e31c251c
            @"{""Serilog"":{""MinimumLevel"":""Information"",""WriteTo"":[{""Name"":""Console"",""Args"":{""outputTemplate"":""[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}""}},{""Name"":""File"",""Args"":{""path"":""%JELLYFIN_LOG_DIR%//log_.log"",""rollingInterval"":""Day"",""outputTemplate"":""[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message}{NewLine}{Exception}""}}]}}",
            // 7af3754a11ad5a4284f107997fb5419a010ce6f3
            @"{""Serilog"":{""MinimumLevel"":""Information"",""WriteTo"":[{""Name"":""Console"",""Args"":{""outputTemplate"":""[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}""}},{""Name"":""Async"",""Args"":{""configure"":[{""Name"":""File"",""Args"":{""path"":""%JELLYFIN_LOG_DIR%//log_.log"",""rollingInterval"":""Day"",""outputTemplate"":""[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message}{NewLine}{Exception}""}}]}}]}}",
            // 60691349a11f541958e0b2247c9abc13cb40c9fb
            @"{""Serilog"":{""MinimumLevel"":""Information"",""WriteTo"":[{""Name"":""Console"",""Args"":{""outputTemplate"":""[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}""}},{""Name"":""Async"",""Args"":{""configure"":[{""Name"":""File"",""Args"":{""path"":""%JELLYFIN_LOG_DIR%//log_.log"",""rollingInterval"":""Day"",""retainedFileCountLimit"":3,""rollOnFileSizeLimit"":true,""fileSizeLimitBytes"":100000000,""outputTemplate"":""[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message}{NewLine}{Exception}""}}]}}]}}",
            // 65fe243afbcc4b596cf8726708c1965cd34b5f68
            @"{""Serilog"":{""MinimumLevel"":""Information"",""WriteTo"":[{""Name"":""Console"",""Args"":{""outputTemplate"":""[{Timestamp:HH:mm:ss}] [{Level:u3}] {ThreadId} {SourceContext}: {Message:lj} {NewLine}{Exception}""}},{""Name"":""Async"",""Args"":{""configure"":[{""Name"":""File"",""Args"":{""path"":""%JELLYFIN_LOG_DIR%//log_.log"",""rollingInterval"":""Day"",""retainedFileCountLimit"":3,""rollOnFileSizeLimit"":true,""fileSizeLimitBytes"":100000000,""outputTemplate"":""[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {ThreadId} {SourceContext}:{Message} {NewLine}{Exception}""}}]}}],""Enrich"":[""FromLogContext"",""WithThreadId""]}}",
            // 96c9af590494aa8137d5a061aaf1e68feee60b67
            @"{""Serilog"":{""MinimumLevel"":""Information"",""WriteTo"":[{""Name"":""Console"",""Args"":{""outputTemplate"":""[{Timestamp:HH:mm:ss}] [{Level:u3}] [{ThreadId}] {SourceContext}: {Message:lj}{NewLine}{Exception}""}},{""Name"":""Async"",""Args"":{""configure"":[{""Name"":""File"",""Args"":{""path"":""%JELLYFIN_LOG_DIR%//log_.log"",""rollingInterval"":""Day"",""retainedFileCountLimit"":3,""rollOnFileSizeLimit"":true,""fileSizeLimitBytes"":100000000,""outputTemplate"":""[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{ThreadId}] {SourceContext}:{Message}{NewLine}{Exception}""}}]}}],""Enrich"":[""FromLogContext"",""WithThreadId""]}}",
        };

        private readonly IApplicationPaths _appPaths;

        public CreateUserLoggingConfigFile(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("{EF103419-8451-40D8-9F34-D1A8E93A1679}");

        /// <inheritdoc/>
        public string Name => "CreateLoggingConfigHeirarchy";

        /// <inheritdoc/>
        public void Perform()
        {
            var logDirectory = _appPaths.ConfigurationDirectoryPath;
            var existingConfigPath = Path.Combine(logDirectory, "logging.json");

            // If the existing logging.json config file is unmodified, then 'reset' it by moving it to 'logging.old.json'
            // NOTE: This config file has 'reloadOnChange: true', so this change will take effect immediately even though it has already been loaded
            if (File.Exists(existingConfigPath) && ExistingConfigUnmodified(existingConfigPath))
            {
                File.Move(existingConfigPath, Path.Combine(logDirectory, "logging.old.json"));
            }
        }

        /// <summary>
        /// Check if the existing logging.json file has not been modified by the user by comparing it to all the
        /// versions in our git history. Until now, the file has never been migrated after first creation so users
        /// could have any version from the git history.
        /// </summary>
        /// <exception cref="IOException"><paramref name="oldConfigPath"/> does not exist or could not be read.</exception>
        private bool ExistingConfigUnmodified(string oldConfigPath)
        {
            var existingConfigJson = JToken.Parse(File.ReadAllText(oldConfigPath));
            return _defaultConfigHistory
                .Select(historicalConfigText => JToken.Parse(historicalConfigText))
                .Any(historicalConfigJson => JToken.DeepEquals(existingConfigJson, historicalConfigJson));
        }
    }
}
