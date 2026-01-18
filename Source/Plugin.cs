using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Extensions;
using Jotunn.Managers;

namespace unlimitedSeeds
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public class unlimitedSeedsPlugin : BaseUnityPlugin
    {
        private const string ModName = "unlimitedSeeds";
        private const string ModVersion = "1.0.0";
        private const string Author = "warpalicious";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = BepInEx.Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        private readonly Harmony HarmonyInstance = new(ModGUID);

        public static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource(ModName);
        public static ConfigEntry<int> OffsetRange = null!;

        public void Awake()
        {
            OffsetRange = Config.BindConfig(
                "General",
                "OffsetRange",
                100000,
                "Expands where in the infinite terrain noise your world can be positioned. " +
                "Vanilla: 10000 (40k×40k region). Higher values unlock terrain patterns impossible in vanilla. " +
                "50000 = 9× more unique worlds.",
                synced: true,
                acceptableValues: new AcceptableValueRange<int>(10000, 1000000));

            UpdatePatchSettings();
            OffsetRange.SettingChanged += (_, _) => UpdatePatchSettings();
            SynchronizationManager.OnConfigurationSynchronized += (_, _) => UpdatePatchSettings();

            Assembly assembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(assembly);
            SetupWatcher();
        }

        private static void UpdatePatchSettings()
        {
            WorldGeneratorOffsetPatch.OffsetRange = OffsetRange.Value;
            Log.LogInfo($"Offset range: {OffsetRange.Value}");
        }

        private void OnDestroy()
        {
            Config.Save();
        }
        
        private void SetupWatcher()
        {
            _lastReloadTime = DateTime.Now;
            FileSystemWatcher watcher = new(BepInEx.Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        private DateTime _lastReloadTime;
        private const long RELOAD_DELAY = 10000000;

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            var now = DateTime.Now;
            var time = now.Ticks - _lastReloadTime.Ticks;
            if (!File.Exists(ConfigFileFullPath) || time < RELOAD_DELAY) return;

            try
            {
                Log.LogInfo("Attempting to reload configuration...");
                Config.Reload();
                Log.LogInfo("Configuration reloaded successfully!");
            }
            catch
            {
                Log.LogError($"There was an issue loading {ConfigFileName}");
                return;
            }

            _lastReloadTime = now;
            UpdatePatchSettings();
        }
    }
} 