using BepInEx;
using HarmonyLib;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ModdingAPI
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Blasphemous.exe")]
    internal class Main : BaseUnityPlugin
    {
        public static ModLoader ModLoader { get; private set; }
        public static ModdingAPI ModdingAPI { get; private set; }
        
        private static readonly Dictionary<string, BepInEx.Logging.ManualLogSource> loggers = new();

        private static Main _instance;

        private void Awake()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadMissingAssemblies);
            _instance ??= this;

            ModLoader = new ModLoader();
            ModdingAPI = new ModdingAPI(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
        }

        private void Update() => ModLoader.Update();

        private void LateUpdate() => ModLoader.LateUpdate();

        public static void AddLogger(string name)
        {
            if (!loggers.ContainsKey(name))
                loggers.Add(name, BepInEx.Logging.Logger.CreateLogSource(name));
        }

        // Log messages to unity console
        internal static void LogMessage(string mod, string message)
        {
            if (loggers.ContainsKey(mod))
                loggers[mod].LogMessage(message);
        }

        // Log warnings to unity console
        internal static void LogWarning(string mod, string message)
        {
            if (loggers.ContainsKey(mod))
                loggers[mod].LogWarning(message);
        }

        // Log errors to unity console
        internal static void LogError(string mod, string message)
        {
            if (loggers.ContainsKey(mod))
                loggers[mod].LogError(message);
        }

        // Logs special message to unity console
        internal static void LogSpecial(string message)
        {
            // Create line text
            var sb = new StringBuilder();
            int length = message.Length;
            while (length > 0)
            {
                sb.Append('=');
                length--;
            }
            string line = sb.ToString();

            // Create final message
            BepInEx.Logging.ManualLogSource logger = loggers[MyPluginInfo.PLUGIN_NAME];
            logger.LogMessage("");
            logger.LogMessage(line);
            logger.LogMessage(message);
            logger.LogMessage(line);
            logger.LogMessage("");
        }

        private Assembly LoadMissingAssemblies(object send, ResolveEventArgs args)
        {
            string assemblyPath = Path.GetFullPath($"Modding\\data\\{args.Name.Substring(0, args.Name.IndexOf(","))}.dll");
            LogMessage(MyPluginInfo.PLUGIN_NAME, "Loading missing assembly from " + assemblyPath);
            return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
        }
    }
}
