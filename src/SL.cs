﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using SideLoader.Model;

namespace SideLoader
{
    public class SL
    {
        public static SL Instance { get; private set; }

        public SL()
        {
            Instance = this;
        }

        // Folders
        public const string SL_FOLDER = @"Mods\SideLoader";
        public static string PLUGINS_FOLDER => Paths.PluginPath;
        public static string GENERATED_FOLDER => $@"{SL_FOLDER}\{_GENERATED}";
        public static string INTERNAL_FOLDER => $@"{SL_FOLDER}\{_INTERNAL}";

        public const string _GENERATED = "_GENERATED";
        public const string _INTERNAL = "_INTERNAL";

        // SL Packs
        internal static Dictionary<string, SLPack> Packs = new Dictionary<string, SLPack>();

        /// <summary>
        /// Get an SLPack from a provided SLPack folder name.
        /// </summary>
        /// <param name="name">The folder name, either `Mods\SideLoader\{Name}\` or `BepInEx\plugins\{Name}\SideLoader\`</param>
        /// <returns>The SLPack instance, if one was loaded with that name.</returns>
        public static SLPack GetSLPack(string name)
        {
            Packs.TryGetValue(name, out SLPack pack);
            return pack;
        }

        /// <summary>Have SL Packs been loaded yet?</summary>
        public static bool PacksLoaded { get; internal set; } = false;

        /// <summary>Invoked before packs are loaded and applied, but after ResouresPrefabManager 
        /// is loaded.</summary>
        public static event Action BeforePacksLoaded;

        /// <summary>Only called once on startup. This will be after ResourcesPrefabManager is 
        /// loaded, and all SLPacks are loaded and applied.</summary>
        public static event Action OnPacksLoaded;

        /// <summary>Use this to safely make changes to a scene when it is truly loaded. (All players 
        /// loaded, gameplay may not yet be resumed).</summary>
        public static event Action OnSceneLoaded;

        // custom template lists
        internal static readonly List<SL_Item> PendingItems = new List<SL_Item>();
        internal static readonly List<SL_StatusEffect> PendingStatuses = new List<SL_StatusEffect>();
        internal static readonly List<SL_ImbueEffect> PendingImbues = new List<SL_ImbueEffect>();
        internal static readonly List<SL_Recipe> PendingRecipes = new List<SL_Recipe>();
        internal static readonly List<SL_EnchantmentRecipe> PendingEnchantments = new List<SL_EnchantmentRecipe>();
        internal static readonly List<SL_Item> PendingLateItems = new List<SL_Item>();
        internal static readonly List<SL_Character> PendingCharacters = new List<SL_Character>();

        // ======== Scene Change Event ========

        // This is called when Unity says the scene is done loading, but we still want to wait for Outward to be done.
        internal void SceneLoaded(Scene scene, LoadSceneMode _)
        {
            if (!scene.name.ToLower().Contains("lowmemory") && !scene.name.ToLower().Contains("mainmenu"))
                SLPlugin.Instance.StartCoroutine(WaitForSceneReady());
        }

        private IEnumerator WaitForSceneReady()
        {
            while (!NetworkLevelLoader.Instance.IsOverallLoadingDone || !NetworkLevelLoader.Instance.AllPlayerDoneLoading)
                yield return null;

            TryInvoke(OnSceneLoaded);
        }

        // ======== SL Setup ========

        internal static void Setup(bool firstSetup = true)
        {
            if (firstSetup)
            {
                SLRPCManager.Setup();

                CheckPrefabDictionaries();

                TryInvoke(BeforePacksLoaded);
            }
            else
                Reset();

            SLPack.LoadAllSLPacks(firstSetup);

            PacksLoaded = true;
            Log("SideLoader Setup Finished");
            Log("-------------------------");

            if (firstSetup && OnPacksLoaded != null)
            {
                TryInvoke(OnPacksLoaded);
                Log("Finished invoking OnPacksLoaded.");
            }

            PendingImbues.Clear();
            PendingItems.Clear();
            PendingLateItems.Clear();
            PendingRecipes.Clear();
            PendingStatuses.Clear();
        }

        internal static void CheckPrefabDictionaries()
        {
            //// This debug is for when we need to dump prefab names for enums.
            //// Only needs to be run on updates.

            //SL_ShootBlast.DebugBlastNames();
            //SL_ShootProjectile.DebugProjectileNames();
            //SL_PlayVFX.DebugVfxNames();

            // Once names have been dumped after an update and enums built, we only need this.

            SL_ShootBlast.BuildBlastsDictionary();
            SL_ShootProjectile.BuildProjectileDictionary();
            SL_PlayVFX.BuildPrefabDictionary();
        }

        internal static void Reset()
        {
            // Reset packs
            Packs.Clear();
            SL_Skill.s_customSkills.Clear();

            // Clear textures dictionary
            CustomTextures.Textures.Clear();
        }

        // ==================== Helpers ==================== //

        public static void TryInvoke(MulticastDelegate _delegate, params object[] args)
        {
            if (_delegate == null)
                return;

            foreach (var action in _delegate.GetInvocationList())
            {
                try
                {
                    action.DynamicInvoke(args);
                }
                catch (Exception e)
                {
                    LogWarning("Exception invoking callback! Checking for InnerException...");
                    LogInnerException(e.InnerException);
                }
            }
        }

        // ==================== Logging ==================== //

        public enum LogLevel
        {
            Message = 8,
            Warning = 4,
            Error = 2
        }

        public static void Log(string log) => SLPlugin.Log(log, LogLevel.Message);
        public static void LogWarning(string log) => SLPlugin.Log(log, LogLevel.Warning);
        public static void LogError(string log) => SLPlugin.Log(log, LogLevel.Error);

        public static void LogInnerException(Exception ex)
        {
            if (ex == null)
                return;

            var inner = ex.InnerException;

            if (inner == null)
                LogWarning("---- Inner-most exception: ----");

            Log($"{ex.GetType().Name}: {ex.Message}");

            if (inner == null)
                Log(ex.StackTrace);
            else
                LogInnerException(inner);
        }
    }
}