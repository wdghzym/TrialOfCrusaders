﻿using HutongGames.PlayMaker.Actions;
using KorzUtils.Helper;
using Modding;
using SFCore;
using System.Collections.Generic;
using System.Reflection;
using TrialOfCrusaders.Controller;
using TrialOfCrusaders.Manager;
using TrialOfCrusaders.ModInterop;
using TrialOfCrusaders.Powers.Common;
using TrialOfCrusaders.SaveData;
using TrialOfCrusaders.UnityComponents.Debuffs;
using TrialOfCrusaders.UnityComponents.Other;
using TrialOfCrusaders.UnityComponents.PowerElements;
using TrialOfCrusaders.UnityComponents.StageElements;
using UnityEngine;
using Caching = TrialOfCrusaders.Powers.Common.Caching;

namespace TrialOfCrusaders;

public class TrialOfCrusaders : Mod, ILocalSettings<LocalSaveData>, IGlobalSettings<GlobalSaveData>, IMenuMod
{
    private Dummy _coroutineHolder;

    #region Mod Setup

    public static TrialOfCrusaders Instance { get; set; }

    internal static Dummy Holder => Instance._coroutineHolder;

    public bool ToggleButtonInsideMenu => throw new System.NotImplementedException();

    public override string GetVersion() => "0.2.3.0-beta";

    public override List<(string, string)> GetPreloadNames() =>
    [
        ("Tutorial_01", "_Props/Chest"),
        ("Deepnest_43", "Mantis Heavy Flyer"),
        ("Crossroads_ShamanTemple", "_Enemies/Zombie Runner"),
        ("Ruins1_24_boss", "Mage Lord"),
        ("Ruins1_23", "Mage"),
        ("Ruins1_23", "Glow Response Mage Computer"),
        ("Ruins1_23", "Inspect Region"),
        ("Ruins1_23", "Ruins Vial Empty (2)/Active/soul_cache (1)"),
        ("GG_Workshop", "GG_Statue_Vengefly/Inspect"),
        ("Deepnest_East_10", "Dream Gate"),
        ("GG_Hollow_Knight", "Battle Scene/HK Prime/Focus Blast/focus_ring"),
        ("GG_Atrium", "GG_Challenge_Door (1)/Door/Unlocked Set/Inspect"),
        ("Room_Fungus_Shaman", "Scream Control/Scream Item"),
        ("Ruins_Bathhouse", "Ghost NPC/Idle Pt")
    ];

    public TrialOfCrusaders()
    {
        InventoryHelper.AddInventoryPage(InventoryPageType.Empty, "Trial Power", "ToCPowers", "ToCInventory", "ToCInventoryAvailable", InventoryController.CreateInventoryPage);
    }

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        Instance = this;
        base.Initialize(preloadedObjects);

        // Create a global coroutine handler that all functions in the mod can use (to keep it independend of other behaviors)
        if (_coroutineHolder != null)
            GameObject.Destroy(_coroutineHolder.gameObject);
        _coroutineHolder = new GameObject("Coroutine Helper").AddComponent<Dummy>();

        // Handle the preloaded objects.
        OrganizePrefabs(preloadedObjects);

        // Hook other mods for interops
        if (ModHooks.GetMod("DebugMod") is Mod)
            HookDebug();

        MenuController.AddMode();
        PhaseController.Initialize();
        SecretController.Initialize();
        On.GameManager.GetStatusRecordInt += EnsureSteelSoul;
    }

    #endregion

    #region Save management

    void ILocalSettings<LocalSaveData>.OnLoadLocal(LocalSaveData saveData)
    {
        HistoryController.SetupList(saveData);
        SecretController.UnlockedSecretArchive = saveData?.UnlockedSecretArchive ?? false;
        SecretController.UnlockedToughness = saveData?.UnlockedToughness ?? false;
        SecretController.UnlockedStashedContraband = saveData?.UnlockedContraband ?? false;
        SecretController.UnlockedHighRoller = saveData?.UnlockedHighRoller ?? false;
        if (PhaseController.CurrentPhase == Enums.Phase.Listening)
        {
            if (saveData != null)
                PhaseController.TransitionTo(Enums.Phase.Initialize);
            else
                PhaseController.TransitionTo(Enums.Phase.Inactive);
        }
    }

    LocalSaveData ILocalSettings<LocalSaveData>.OnSaveLocal()
    {
        if (PhaseController.CurrentPhase == Enums.Phase.Inactive || PhaseController.CurrentPhase == Enums.Phase.Listening)
            return null;
        LocalSaveData saveData = new() 
        { 
            OldRunData = HistoryController.History,
            Archive = HistoryController.Archive,
            UnlockedContraband = SecretController.UnlockedStashedContraband,
            UnlockedHighRoller = SecretController.UnlockedHighRoller,
            UnlockedToughness = SecretController.UnlockedToughness,
            UnlockedSecretArchive = SecretController.UnlockedSecretArchive,
        };
        if (PhaseController.CurrentPhase == Enums.Phase.WaitForSave)
            PhaseController.TransitionTo(Enums.Phase.Inactive);
        return saveData;
    }

    #endregion

    #region Prefab handling

    private void OrganizePrefabs(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        GameObject container = GameObject.Find("Trial of Crusaders Objects");
        if (container != null)
            GameObject.Destroy(container);
        container = new("Trial of Crusaders Objects");
        GameObject.DontDestroyOnLoad(container);

        // Setup prefabs
        SetupPowerPrefabs(preloadedObjects);
        SetupDebuffs(preloadedObjects);
        TreasureManager.SetupShiny(preloadedObjects["Tutorial_01"]["_Props/Chest"]);
        ScoreController.SetupScoreboard(preloadedObjects["GG_Atrium"]["GG_Challenge_Door (1)/Door/Unlocked Set/Inspect"]);
        SpecialTransition.SetupPrefab(preloadedObjects["GG_Workshop"]["GG_Statue_Vengefly/Inspect"]);
        ScoreController.SetupResultInspect(preloadedObjects["GG_Workshop"]["GG_Statue_Vengefly/Inspect"]);
        HubController.Tink = preloadedObjects["Deepnest_43"]["Mantis Heavy Flyer"].GetComponent<PersonalObjectPool>().startupPool[0].prefab.GetComponent<TinkEffect>().blockEffect;
        HubController.Tink.name = "Tink Effect";
        HubController.InspectPrefab = preloadedObjects["Ruins1_23"]["Inspect Region"];
        Gate.Prefab = preloadedObjects["Deepnest_East_10"]["Dream Gate"];
        Gate.Prefab.name = "Gate";
        HistoryController.ArchiveSprite = GameObject.Instantiate(preloadedObjects["Ruins1_23"]["Glow Response Mage Computer"]);

        GameObject[] preloads =
        [
            // Power prefabs
            GroundSlam.Shockwave,
            GreaterMind.Orb,
            Caching.SoulCache,
            VoidZone.Ring,
            // Debuffs
            ConcussionEffect.Prefab,
            WeakenedEffect.Prefab,
            ShatteredMindEffect.Prefab,
            BleedEffect.Prefab,
            BurnEffect.Prefab,
            // Other
            HubController.Tink,
            HubController.InspectPrefab,
            Gate.Prefab,
            TreasureManager.Shiny,
            ScoreController.ResultSequencePrefab,
            ScoreController.ScoreboardPrefab,
            SpecialTransition.TransitionPrefab,
            _coroutineHolder.gameObject,
            HistoryController.ArchiveSprite
        ];
        foreach (GameObject gameObject in preloads)
        {
            gameObject.transform.SetParent(container.transform);
            GameObject.DontDestroyOnLoad(gameObject);
            gameObject.SetActive(false);
        }
        _coroutineHolder.gameObject.SetActive(true);
    }

    private void SetupPowerPrefabs(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        GroundSlam.Shockwave = preloadedObjects["Ruins1_24_boss"]["Mage Lord"].LocateMyFSM("Mage Lord")
            .GetState("Quake Waves")
            .GetFirstAction<SpawnObjectFromGlobalPool>()
            .gameObject.Value;
        GroundSlam.Shockwave.name = "Shockwave";
        GameObject.DontDestroyOnLoad(GroundSlam.Shockwave);

        GreaterMind.Orb = preloadedObjects["Ruins1_23"]["Mage"].GetComponent<PersonalObjectPool>().startupPool[0].prefab;
        GreaterMind.Orb.name = "Greater Mind Orb";
        GameObject.DontDestroyOnLoad(GreaterMind.Orb);

        Caching.SoulCache = preloadedObjects["Ruins1_23"]["Ruins Vial Empty (2)/Active/soul_cache (1)"];
        Caching.SoulCache.name = "Soul Cache";
        GameObject.DontDestroyOnLoad(Caching.SoulCache);

        VoidZone.Ring = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/HK Prime/Focus Blast/focus_ring"];
        VoidZone.Ring.name = "Void Ring";
        GameObject.DontDestroyOnLoad(VoidZone.Ring);
    }

    private void SetupDebuffs(Dictionary<string, Dictionary<string, GameObject>> objects)
    {
        ConcussionEffect.PreparePrefab(objects["Deepnest_43"]["Mantis Heavy Flyer"].GetComponent<PersonalObjectPool>().startupPool[0].prefab);
        WeakenedEffect.PreparePrefab(objects["Room_Fungus_Shaman"]["Scream Control/Scream Item"]);
        ShatteredMindEffect.PreparePrefab(objects["Ruins_Bathhouse"]["Ghost NPC/Idle Pt"]);
        // We love carl <3
        GameObject carl = objects["Crossroads_ShamanTemple"]["_Enemies/Zombie Runner"];
        BleedEffect.PreparePrefab(typeof(InfectedEnemyEffects).GetField("spatterOrangePrefab", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(carl.GetComponent<InfectedEnemyEffects>()) as GameObject);

        GameObject corpse = typeof(EnemyDeathEffects).GetField("corpsePrefab", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(carl.GetComponent<EnemyDeathEffects>()) as GameObject;
        BurnEffect.PreparePrefab(corpse.transform.Find("Corpse Flame").gameObject);
    }

    #endregion

    // If the selection mode menu doesn't appear, a few unity errors are thrown. Therefore we force it to appear.
    private int EnsureSteelSoul(On.GameManager.orig_GetStatusRecordInt orig, GameManager self, string key)
    {
        int vanillaValue = orig(self, key);

        if (key == "RecPermadeathMode")
            return 1;
        return vanillaValue;
    }

    private void HookDebug()
    {
        DebugModInterop.Initialize();
    }

    public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
    {
        return new()
        {
            new(){ Name = "Track failed runs", Description = "If on, failed runs will appear in the history.", Values = ["On", "Off"],
                Saver = x => HistoryController.HistorySettings.TrackFailedRuns = x == 0,
                Loader = () => HistoryController.HistorySettings.TrackFailedRuns ? 0 : 1},
            new(){ Name = "Track forfeited runs", Description = "If on, forfeited runs will appear in the history", Values = ["On", "Off"],
                Saver = x => HistoryController.HistorySettings.TrackForfeitedRuns = x == 0,
                Loader = () => HistoryController.HistorySettings.TrackForfeitedRuns ? 0 : 1},
            new(){ Name = "History amount", Description = "Determine the amount of previous runs saved.", Values = ["1", "5", "10", "50", "100", "All"],
                Saver = x =>
                    {
                        HistoryController.HistorySettings.HistoryAmount = x switch
                        {
                            0 => 1,
                            1 => 5,
                            2 => 10,
                            3 => 50,
                            4 => 100,
                            _ => int.MaxValue
                        };
                    },
                Loader = () =>
                    HistoryController.HistorySettings.HistoryAmount switch
                    {
                        1 => 0,
                        5 => 1,
                        10 => 2,
                        50 => 3,
                        100 => 4,
                        _ => 6
                    }},
        };
    }

    public void OnLoadGlobal(GlobalSaveData saveData) => HistoryController.HistorySettings = saveData;

    public GlobalSaveData OnSaveGlobal() => HistoryController.HistorySettings;
}
