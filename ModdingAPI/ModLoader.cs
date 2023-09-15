using Framework.FrameworkCore;
using Framework.Managers;
using ModdingAPI.Commands;
using ModdingAPI.Items;
using ModdingAPI.Penitences;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ModdingAPI
{
    internal class ModLoader
    {
        private readonly List<Mod> mods = new();
        private readonly List<ModCommand> modCommands = new();
        private readonly List<ModPenitence> modPenitences = new();
        private readonly List<ModItem> modItems = new();

        private bool initialized = false;

        public void Update()
        {
            if (!initialized) return;

            for (int i = 0; i < mods.Count; i++)
            {
                mods[i].Update();
            }
        }

        public void LateUpdate()
        {
            if (!initialized) return;

            for (int i = 0; i < mods.Count; i++)
            {
                mods[i].LateUpdate();
            }
        }

        public void Initialize()
        {
            initialized = true;
            LevelManager.OnLevelPreLoaded += LevelPreLoaded;
            LevelManager.OnLevelLoaded += LevelLoaded;
            LevelManager.OnBeforeLevelLoad += LevelUnloaded;

            Main.LogSpecial("Initialization");
            for (int i = 0; i < mods.Count; i++)
            {
                mods[i].Initialize();
                if (mods[i] is PersistentMod mod)
                    Core.Persistence.AddPersistentManager(new ModPersistentSystem(mod));
            }

            if (modPenitences.Count > 0)
                Core.PenitenceManager.ResetPersistence();
        }

        public void Dispose()
        {
            for (int i = 0; i < mods.Count; i++)
            {
                mods[i].Dispose();
            }

            LevelManager.OnLevelPreLoaded -= LevelPreLoaded;
            LevelManager.OnLevelLoaded -= LevelLoaded;
            LevelManager.OnBeforeLevelLoad -= LevelUnloaded;
            initialized = false;
        }

        public void LevelPreLoaded(Level oldLevel, Level newLevel)
        {
            string oLevel = oldLevel == null ? string.Empty : oldLevel.LevelName, nLevel = newLevel == null ? string.Empty : newLevel.LevelName;
            for (int i = 0; i < mods.Count; i++)
            {
                mods[i].LevelPreloaded(oLevel, nLevel);
            }
        }

        public void LevelLoaded(Level oldLevel, Level newLevel)
        {
            string oLevel = oldLevel == null ? string.Empty : oldLevel.LevelName, nLevel = newLevel == null ? string.Empty : newLevel.LevelName;

            Main.LogSpecial("Loaded level " + nLevel);
            for (int i = 0; i < mods.Count; i++)
            {
                mods[i].LevelLoaded(oLevel, nLevel);
            }
        }

        public void LevelUnloaded(Level oldLevel, Level newLevel)
        {
            string oLevel = oldLevel == null ? string.Empty : oldLevel.LevelName, nLevel = newLevel == null ? string.Empty : newLevel.LevelName;
            for (int i = 0; i < mods.Count; i++)
            {
                mods[i].LevelUnloaded(oLevel, nLevel);
            }
        }

        public void NewGame(bool NGPlus)
        {
            foreach (ModItem item in modItems)
            {
                if (item.CarryOnStart)
                    item.GiveItem();
            }

            for (int i = 0; i < mods.Count; i++)
            {
                if (mods[i] is PersistentMod mod)
                    mod.NewGame(NGPlus);
            }
            Core.Persistence.SaveGame(true);
        }

        public void RegisterMod(Mod mod)
        {
            foreach (Mod modMod in mods)
            {
                if (modMod.ModId == mod.ModId)
                    return;
            }
            Main.LogMessage(MyPluginInfo.PLUGIN_NAME, $"Registering mod: {mod.ModName} ({mod.ModVersion})");
            Main.AddLogger(mod.ModName);
            mods.Add(mod);
        }

        public void RegisterCommand(ModCommand command)
        {
            foreach (ModCommand modCommand in modCommands)
            {
                if (modCommand.CommandName == command.CommandName)
                    return;
            }
            modCommands.Add(command);
        }

        public void RegisterPenitence(ModPenitence penitence)
        {
            foreach (ModPenitence modPenitence in modPenitences)
            {
                if (modPenitence.Id == penitence.Id)
                    return;
            }
            modPenitences.Add(penitence);
            Main.LogMessage(MyPluginInfo.PLUGIN_NAME, $"Registering custom penitence: {penitence.Name} ({penitence.Id})");
        }

        public void RegisterItem(ModItem item)
        {
            foreach (ModItem modItem in modItems)
            {
                if (modItem.Id == item.Id)
                    return;
            }
            modItems.Add(item);
            //itemLoader.AddItem(item);
            Main.LogMessage(MyPluginInfo.PLUGIN_NAME, $"Registering custom item: {item.Name} ({item.Id})");
        }

        public ReadOnlyCollection<Mod> GetAllMods() => mods.AsReadOnly();

        public ReadOnlyCollection<ModCommand> GetAllModCommands() => modCommands.AsReadOnly();

        public ReadOnlyCollection<ModPenitence> GetAllModPenitences() => modPenitences.AsReadOnly();

        public ReadOnlyCollection<ModItem> GetAllModItems() => modItems.AsReadOnly();
    }
}
