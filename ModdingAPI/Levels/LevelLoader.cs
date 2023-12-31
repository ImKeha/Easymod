﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tools.Level.Actionables;
using Framework.Managers;
using Framework.Inventory;
using Framework.Penitences;
using Framework.Util;

namespace ModdingAPI.Levels
{
    internal class LevelLoader
    {
        private enum ObjectType { Nothing, CollectibleItem, SwordHeartItem, ChestIron, ChestGold, ChestRelic, Spikes, Lantern, PlatformWood, PlatformStone, PlatformBlood,    PrieDieu, Lever, Gate, Ladder, RootWall, Enemy, Trap }

        public bool InLoadProcess { get; private set; }
        private Transform CurrentObjectHolder { get; set; } // Only accessed when adding objects, so always set when loading scene with objects to add

        private Dictionary<string, LevelStructure> LevelModifications { get; set; }
        private Dictionary<ObjectType, GameObject> LoadedObjects { get; set; }

        public void LoadLevelEdits()
        {
            LevelModifications = Main.moddingAPI.fileUtil.loadLevels();
        }

        public void LevelPreLoaded(string level)
        {
            // Apply all level edits from other mods
            if (!LevelModifications.ContainsKey(level))
                return;

            LevelStructure levelModification = LevelModifications[level];
            Main.LogMessage(Main.MOD_NAME, "Applying modifications for " + level);

            if (levelModification.AddedObjects != null)
            {
                CurrentObjectHolder = GameObject.Find("LOGIC").transform;

                foreach (AddedObject obj in levelModification.AddedObjects)
                {
                    // Calculate object type and make sure that object has been loaded
                    if (GetTypeFromObject(obj, out ObjectType objectType) && LoadedObjects.ContainsKey(objectType) && CheckCondition(obj.Condition))
                        CreateNewObject(objectType, obj);
                }
            }
            if (levelModification.DisabledObjects != null)
            {
                // Potentially disable decoration objects
                List<string> decoration = levelModification.DisabledObjects.Decoration;
                if (decoration != null && decoration.Count > 0)
                    DisableObjectGroup(SceneManager.GetSceneByName(level + "_DECO"), levelModification.DisabledObjects.Decoration);
                // Potentially disable layout objects
                List<string> layout = levelModification.DisabledObjects.Layout;
                if (layout != null && layout.Count > 0)
                    DisableObjectGroup(SceneManager.GetSceneByName(level + "_LAYOUT"), levelModification.DisabledObjects.Layout);
                // Potentially disable logic objects
                List<string> logic = levelModification.DisabledObjects.Logic;
                if (logic != null && logic.Count > 0)
                    DisableObjectGroup(SceneManager.GetSceneByName(level + "_LOGIC"), levelModification.DisabledObjects.Logic);
            }
        }

        public void LevelLoaded(string level)
        {
            // When game is first started, load objects
            if (level == "MainMenu" && LoadedObjects == null)
            {
                LoadedObjects = new Dictionary<ObjectType, GameObject>();
                Main.Instance.StartCoroutine(LoadRequiredItems());
            }
        }

        private bool CheckCondition(string condition)
        {
            if (condition == null) return true;
            int colon = condition.IndexOf(':');
            string conditionType = condition.Substring(0, colon);
            string conditionValue = condition.Substring(colon + 1);

            if (conditionType == "flag")
            {
                return Core.Events.GetFlag(conditionValue);
            }
            if (conditionType == "penitence")
            {
                IPenitence penitence = Core.PenitenceManager.GetCurrentPenitence();
                return penitence != null && penitence.Id == conditionValue;
            }
            return true;
        }

        #region Loading Objects

        private IEnumerator LoadRequiredItems()
        {
            // Get list of all objects that are needed by mods
            List<ObjectType> necessaryObjects = new List<ObjectType>();
            foreach (LevelStructure level in LevelModifications.Values)
            {
                if (level.AddedObjects != null)
                {
                    foreach (AddedObject obj in level.AddedObjects)
                    {
                        if (GetTypeFromObject(obj, out ObjectType objectType) && !necessaryObjects.Contains(objectType))
                            necessaryObjects.Add(objectType);
                    }
                }
            }

            // Load any necessary objects from various scenes

            // Items
            if (necessaryObjects.Contains(ObjectType.CollectibleItem))
                yield return Main.Instance.StartCoroutine(LoadSceneForObject(ObjectType.CollectibleItem, "D02Z02S14_LOGIC", "LOGIC/INTERACTABLES/ACT_Collectible"));
            if (necessaryObjects.Contains(ObjectType.SwordHeartItem))
                yield return Main.Instance.StartCoroutine(LoadSceneForObject(ObjectType.SwordHeartItem, "D06Z01S22_LOGIC", "LOGIC/INTERACTABLES"));

            // Chests
            if (necessaryObjects.Contains(ObjectType.ChestIron))
                yield return Main.Instance.StartCoroutine(LoadSceneForObject(ObjectType.ChestIron, "D01Z05S11_LOGIC", "LOGIC/INTERACTABLES/ACT_Iron Chest"));
            if (necessaryObjects.Contains(ObjectType.ChestGold))
                yield return Main.Instance.StartCoroutine(LoadSceneForObject(ObjectType.ChestGold, "D20Z02S02_LOGIC", "ACT_Wooden Chest"));
            if (necessaryObjects.Contains(ObjectType.ChestRelic))
                yield return Main.Instance.StartCoroutine(LoadSceneForObject(ObjectType.ChestRelic, "D17BZ01S01_LOGIC", "LOGIC/INTERACTABLES/ACT_Relicarium"));

            // Platforms
            if (necessaryObjects.Contains(ObjectType.PlatformWood))
                yield return Main.Instance.StartCoroutine(LoadSceneForObject(ObjectType.PlatformWood, "D05Z02S12_LOGIC", "LOGIC/INTERACTABLES/{0}"));
            if (necessaryObjects.Contains(ObjectType.PlatformBlood))
                yield return Main.Instance.StartCoroutine(LoadSceneForObject(ObjectType.PlatformBlood, "D17Z01S10_LOGIC", "LOGIC/INTERACTABLES/{0}"));

            // Room objects
            if (necessaryObjects.Contains(ObjectType.Lantern))
                yield return Main.Instance.StartCoroutine(LoadSceneForObject(ObjectType.Lantern, "D20Z01S02_LOGIC", "LOGIC/INTERACTABLES/Chain Hook"));
            if (necessaryObjects.Contains(ObjectType.Spikes))
            {
                yield return Main.Instance.StartCoroutine(LoadSceneForObject(ObjectType.Spikes, "D01Z03S01_DECO", "MIDDLEGROUND/AfterPlayer/Spikes/{0}"));
                if (LoadedObjects.ContainsKey(ObjectType.Spikes))
                {
                    GameObject spikes = LoadedObjects[ObjectType.Spikes];
                    spikes.gameObject.tag = "SpikeTrap";
                    spikes.layer = LayerMask.NameToLayer("Trap");
                    BoxCollider2D collider = spikes.AddComponent<BoxCollider2D>();
                    collider.isTrigger = true;
                    collider.size = new Vector2(1.8f, 0.8f);
                }
            }

            // Fix camera after scene loads
            Camera.main.transform.position = new Vector3(0, 0, -10);
            Camera.main.backgroundColor = new Color(0, 0, 0, 1);
            yield return null;
        }

        private IEnumerator LoadSceneForObject(ObjectType objectType, string sceneName, string objectPath)
        {
            InLoadProcess = true;

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Load the item from this scene
            Scene tempScene = SceneManager.GetSceneByName(sceneName);
            GameObject sceneObject = FindObjectInScene(tempScene, objectPath, true);
            if (sceneObject == null)
            {
                Main.LogError(Main.MOD_NAME, $"Failed to load {objectPath} from {sceneName}");
            }
            else
            {
                GameObject obj = Object.Instantiate(sceneObject, Main.Instance.transform);
                obj.name = objectType.ToString();
                obj.transform.position = Vector3.zero;
                obj.SetActive(false);
                LoadedObjects.Add(objectType, obj);
                Main.LogMessage(Main.MOD_NAME, $"Loaded {objectPath} from {sceneName}");
            }

            yield return null;

            asyncLoad = SceneManager.UnloadSceneAsync(tempScene);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            InLoadProcess = false;
        }

        private void DisableObjectGroup(Scene scene, List<string> disabledObjects)
        {
            // Store dictionary of root objects
            Dictionary<string, Transform> rootObjects = FindRootObjectsInScene(scene, false);

            // Loop through disabled objects and locate & disable them
            foreach (string disabledObject in disabledObjects)
            {
                GameObject obj = FindObjectInScene(rootObjects, disabledObject);
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        #endregion Loading Objects

        #region Finding Objects

        private GameObject FindObjectInScene(Scene scene, string objectPath, bool disableRoots)
        {
            return FindObjectInScene(FindRootObjectsInScene(scene, disableRoots), objectPath);
        }

        private GameObject FindObjectInScene(Dictionary<string, Transform> rootObjects, string objectPath)
        {
            string[] transformPath = objectPath.Split('/');

            Transform currTransform = null;
            for (int i = 0; i < transformPath.Length; i++)
            {
                string finder = transformPath[i];
                if (i == 0)
                {
                    currTransform = rootObjects.ContainsKey(finder) ? rootObjects[finder] : null;
                }
                else if (finder.Length >= 3 && finder[0] == '{' && finder[finder.Length - 1] == '}')
                {
                    int childIdx = int.Parse(finder.Substring(1, finder.Length - 2));
                    currTransform = currTransform.childCount > childIdx ? currTransform.GetChild(childIdx) : null;
                }
                else
                {
                    currTransform = currTransform.Find(finder);
                }

                if (currTransform == null) break;
            }

            return currTransform?.gameObject;
        }

        private Dictionary<string, Transform> FindRootObjectsInScene(Scene scene, bool disableRoots)
        {
            Dictionary<string, Transform> rootObjects = new Dictionary<string, Transform>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                if (obj.name[0] != '=' && !rootObjects.ContainsKey(obj.name))
                {
                    rootObjects.Add(obj.name, obj.transform);
                    if (disableRoots)
                        obj.SetActive(false);
                }
            }
            return rootObjects;
        }

        #endregion Finding Objects

        #region Creating Objects

        private void CreateNewObject(ObjectType objectType, AddedObject obj)
        {
            switch (objectType)
            {
                case ObjectType.CollectibleItem: CreateCollectibleItem(obj); break;
                case ObjectType.SwordHeartItem: CreateSwordHeartItem(obj); break;
                case ObjectType.ChestIron:
                case ObjectType.ChestGold:
                case ObjectType.ChestRelic:
                    CreateChest(obj, objectType); break;

                case ObjectType.PlatformWood: CreateBaseObject(ObjectType.PlatformWood, obj, "Platform"); break;
                case ObjectType.PlatformBlood: CreateBloodPlatform(obj); break;
                case ObjectType.Lantern: CreateBaseObject(ObjectType.Lantern, obj, "Lantern"); break;
                case ObjectType.Spikes: CreateSpikes(obj); break;
            }
        }

        private GameObject CreateBaseObject(ObjectType objectType, AddedObject obj, string name)
        {
            GameObject baseObject = Object.Instantiate(LoadedObjects[objectType], CurrentObjectHolder);
            baseObject.name = name;
            baseObject.transform.position = new Vector3(obj.XPos, obj.YPos, 0);
            baseObject.transform.eulerAngles = new Vector3(0, 0, obj.Rotation);
            baseObject.SetActive(true);
            return baseObject;
        }

        private void CreateCollectibleItem(AddedObject obj)
        {
            GameObject newItem = CreateBaseObject(ObjectType.CollectibleItem, obj, "Item Pickup " + obj.Id);

            newItem.GetComponent<UniqueId>().uniqueId = "ITEM-PICKUP-" + obj.Id;
            InteractableInvAdd addComponent = newItem.GetComponent<InteractableInvAdd>();
            addComponent.item = obj.Id;
            addComponent.itemType = ItemModder.GetItemTypeFromId(obj.Id);
        }

        private void CreateSwordHeartItem(AddedObject obj)
        {
            GameObject newObj = CreateBaseObject(ObjectType.SwordHeartItem, obj, "SwordHeart Shrine " + obj.Id);

            GameObject item = newObj.transform.GetChild(0).gameObject;
            item.transform.localPosition = new Vector3(obj.FacingDirection ? 1 : -1, 1, 0);
            item.GetComponent<UniqueId>().uniqueId = "Sword-Heart-Shrine-" + obj.Id;
            InteractableInvAdd addComponent = item.GetComponent<InteractableInvAdd>();
            addComponent.item = obj.Id;
            addComponent.itemType = ItemModder.GetItemTypeFromId(obj.Id);

            GameObject shrine = newObj.transform.GetChild(1).gameObject;
            shrine.transform.localPosition = Vector3.zero;
            if (!obj.FacingDirection)
                shrine.GetComponent<SpriteRenderer>().flipX = true;
        }

        private void CreateChest(AddedObject obj, ObjectType type)
        {
            GameObject newItem = CreateBaseObject(type, obj, "Chest " + obj.Id);

            newItem.GetComponent<UniqueId>().uniqueId = "CHEST-" + obj.Id;
            InteractableInvAdd addComponent = newItem.GetComponent<InteractableInvAdd>();
            addComponent.item = obj.Id;
            addComponent.itemType = ItemModder.GetItemTypeFromId(obj.Id);
        }

        private void CreateSpikes(AddedObject obj)
        {
            GameObject spikes = CreateBaseObject(ObjectType.Spikes, obj, "Spikes");
            if (obj.FacingDirection)
                spikes.GetComponent<SpriteRenderer>().flipX = true;
            if (obj.XSize > 0 && obj.YSize > 0)
                spikes.GetComponent<BoxCollider2D>().size = new Vector2(obj.XSize, obj.YSize);
        }

        private void CreateBloodPlatform(AddedObject obj)
        {
            // Must be added in reverse order so that previous platforms can reference their new ones
            GameObject platform = CreateBaseObject(ObjectType.PlatformBlood, obj, obj.Id);
            FaithPlatform faith = platform.GetComponent<FaithPlatform>();
            if (faith == null) return;

            SettingBloodPlatforms = true;
            BloodFirst = obj.FacingDirection;
            string[] faithIds = obj.ExtraData.Split(',');
            BloodObjects = new GameObject[faithIds.Length];
            for (int i = 0; i < faithIds.Length; i++)
            {
                Transform targetPlatform = CurrentObjectHolder.Find(faithIds[i]);
                if (targetPlatform != null)
                    BloodObjects[i] = targetPlatform.gameObject;
            }
            faith.Use();
            SettingBloodPlatforms = false;
        }


        #endregion Creating Objects

        #region Helpers

        private bool GetTypeFromObject(AddedObject obj, out ObjectType type)
        {
            try
            {
                type = (ObjectType)System.Enum.Parse(typeof(ObjectType), obj.Type);
                return true;
            }
            catch (System.ArgumentException)
            {
                Main.LogWarning(Main.MOD_NAME, obj.Type + " is not a valid object type!");
                type = ObjectType.Nothing;
                return false;
            }
        }

        #endregion Helpers

        public static bool SettingBloodPlatforms { get; private set; }
        public static GameObject[] BloodObjects { get; private set; }
        public static bool BloodFirst { get; private set; }
    }
}
