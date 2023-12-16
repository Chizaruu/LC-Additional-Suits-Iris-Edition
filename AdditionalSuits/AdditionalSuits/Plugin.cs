using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace IrisChansAdditionalSuits
{
    [Serializable]
    public class UnlockableSuitDef
    {
        public string suitID;
        public string suitName;
        public string suitTexture;
    }

    public class UnlockableSuitDefListing
    {
        public List<UnlockableSuitDef> unlockableSuits = new List<UnlockableSuitDef>();
    }

    [BepInPlugin(modGUID, modName, modVersion)]
    public class AdditionalSuitsBase : BaseUnityPlugin
    {
        private static AdditionalSuitsBase Instance;

        private const string modGUID = "TSK.IrisChansAdditionalSuits";
        private const string modName = "IrisChan's AdditionalSuits";
        private const string modVersion = "1.1.1";

        private readonly Harmony harmony = new Harmony(modGUID);
        public static ManualLogSource mls;
        public static bool SuitsLoaded;
        public static string ModResourceFolder;
        public static UnlockableSuitDefListing SuitDefManifest;

        void Awake()
        {
            if (Instance == null) { Instance = this; }
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo($"{modName} - initializing...");

            ModResourceFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "res" + modName);
            harmony.PatchAll();
            mls.LogInfo($"{modName} - initialized!");
        }

        [HarmonyPatch(typeof(StartOfRound))]
        internal class StartOfRoundPatch
        {
            [HarmonyPatch("Start")]
            [HarmonyPrefix]
            private static void StartPatch(ref StartOfRound __instance)
            {
                if (SuitsLoaded) return;

                UnlockableItem suitPrefab = FindDefaultSuit(__instance.unlockablesList.unlockables);
                if (suitPrefab == null)
                {
                    mls.LogInfo($"{modName} - ERROR: Default suit not found");
                    return;
                }

                LoadSuitDefinitions();
                if (SuitDefManifest?.unlockableSuits == null || !SuitDefManifest.unlockableSuits.Any())
                {
                    return;
                }

                AddCustomSuits(__instance, suitPrefab);
                SuitsLoaded = true;
            }

            private static UnlockableItem FindDefaultSuit(List<UnlockableItem> unlockables)
            {
                return unlockables.FirstOrDefault(u => u.suitMaterial != null && u.alreadyUnlocked);
            }

            private static void LoadSuitDefinitions()
            {
                string jsonPath = Path.Combine(ModResourceFolder, "suit-defs.json");
                mls.LogInfo($"{modName} - Attempting to parse JSON file: {jsonPath}");

                try
                {
                    string jsonText = File.ReadAllText(jsonPath);
                    SuitDefManifest = ParseSuitDefs(jsonText);
                    if (SuitDefManifest?.unlockableSuits == null || !SuitDefManifest.unlockableSuits.Any())
                    {
                        mls.LogInfo($"{modName} - ERROR: Failed to convert JSON file to manifest");
                    }
                }
                catch (Exception ex)
                {
                    mls.LogInfo($"{modName} - ERROR: {ex.Message}");
                }
            }

            private static void AddCustomSuits(StartOfRound __instance, UnlockableItem suitPrefab)
            {
                foreach (var unlockableSuitDef in SuitDefManifest.unlockableSuits)
                {
                    var newUnlockableItem = JsonUtility.FromJson<UnlockableItem>(JsonUtility.ToJson(suitPrefab));
                    var suitTexture = LoadTexture(Path.Combine(ModResourceFolder, unlockableSuitDef.suitTexture));
                    var suitMaterial = new Material(newUnlockableItem.suitMaterial) { mainTexture = suitTexture };

                    newUnlockableItem.suitMaterial = suitMaterial;
                    newUnlockableItem.unlockableName = unlockableSuitDef.suitName;

                    __instance.unlockablesList.unlockables.Add(newUnlockableItem);
                }
            }

            private static Texture2D LoadTexture(string filePath)
            {
                var texture = new Texture2D(2, 2);
                ImageConversion.LoadImage(texture, File.ReadAllBytes(filePath));
                return texture;
            }

            private static UnlockableSuitDefListing ParseSuitDefs(string jsonText)
            {
                var suitDefList = new UnlockableSuitDefListing { unlockableSuits = new List<UnlockableSuitDef>() };

                var suitDefs = jsonText.Split(new[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string def in suitDefs.Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    string defJson = "{" + def.Trim(',') + "}";
                    var suitDef = JsonUtility.FromJson<UnlockableSuitDef>(defJson);
                    if (suitDef != null)
                    {
                        suitDefList.unlockableSuits.Add(suitDef);
                    }
                }

                return suitDefList;
            }
        }
    }
}
