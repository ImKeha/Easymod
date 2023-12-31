﻿using Framework.Managers;
using Framework.FrameworkCore;

namespace ModdingAPI
{
    internal class ModPersistentSystem : PersistentInterface
    {
        private PersistentMod persistentMod;

        public ModPersistentSystem(PersistentMod persistentMod)
        {
            this.persistentMod = persistentMod;
        }

        public PersistentManager.PersistentData GetCurrentPersistentState(string dataPath, bool fullSave)
        {
            return persistentMod.SaveGame();
        }

        public void SetCurrentPersistentState(PersistentManager.PersistentData data, bool isloading, string dataPath)
        {
            persistentMod.LoadGame((ModPersistentData)data);
        }

        public void ResetPersistence()
        {
            persistentMod.ResetGame();
        }

        public int GetOrder()
        {
            return 0;
        }

        public string GetPersistenID()
        {
            return persistentMod.PersistentID;
        }
    }
}
