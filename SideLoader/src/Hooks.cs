﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SideLoader.Hooks
{
    #region Name localization

    [HarmonyPatch(typeof(Item), "GetLocalizedName")]
    public class Item_GetLocalizedName
    {
        [HarmonyPostfix]
        public static void Postfix(Item __instance, ref string __result)
        {
            if (CustomItems.s_customLocalizations.ContainsKey(__instance.ItemID))
                __result = CustomItems.s_customLocalizations[__instance.ItemID][0];
        }
    }

    [HarmonyPatch(typeof(Item), "Description", MethodType.Getter)]
    public class Item_get_Description
    {
        [HarmonyPostfix]
        public static void Postfix(Item __instance, ref string __result)
        {
            if (CustomItems.s_customLocalizations.ContainsKey(__instance.ItemID))
                __result = CustomItems.s_customLocalizations[__instance.ItemID][1];
        }
    }

    #endregion

    #region Item Start hook

    [HarmonyPatch(typeof(Item), "Start")]
    public class Item_Start
    {
        public static void Postfix(Item __instance, ref string ___m_toLogString)
        {
            ___m_toLogString = Serializer.ReplaceInvalidChars(___m_toLogString);

            if (SL_Item.s_initCallbacks.TryGetValue(__instance.ItemID, out List<Action<Item>> invocationList))
            {
                foreach (var listener in invocationList)
                {
                    try
                    {
                        listener.Invoke(__instance);
                    }
                    catch (Exception e)
                    {
                        SL.LogWarning($"Exception invoking callback for Item.Start() on {__instance.Name}");
                        SL.LogInnerException(e);
                    }
                }
            }
        }
    }

    #endregion

    #region CORE SETUP PATCH

    /// <summary>
    /// SideLoader's setup is a Finalizer on ResourcesPrefabManager.Load().
    /// </summary>
    [HarmonyPatch(typeof(ResourcesPrefabManager), "Load")]
    public class ResourcesPrefabManager_Load
    {
        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                SL.Log("Exception on ResourcesPrefabManager.Load: " + __exception.GetType().FullName);
                SL.Log(__exception.Message);
                SL.Log(__exception.StackTrace);
            }

            SL.Setup();

            return null;
        }
    }

    #endregion

    #region SL_ITEM PATCHES

    // fix for the recipe menu, which can break from some custom items when they are an ingredient.
    [HarmonyPatch(typeof(ItemListDisplay), "SortBySupport")]
    public class ItemListDisplay_SortBySupport
    {
        [HarmonyFinalizer]
        public static Exception Finalizer(ref int __result, Exception __exception)
        {
            if (__exception != null)
            {
                __result = -1;
            }
            return null;
        }
    }

    // fix for ItemDetailsDisplay. Shouldn't really be needed anymore, but leaving it for now.
    [HarmonyPatch(typeof(ItemDetailsDisplay), "RefreshDetail")]
    public class ItemDetailsDisplay_RefreshDetail
    {
        [HarmonyFinalizer]
        public static Exception Finalizer()
        {
            return null;
        }
    }

    #endregion

    #region SL_ITEM VISUALS PATCHES

    [HarmonyPatch(typeof(Item), "GetItemVisual", new Type[] { typeof(bool) })]
    public class Item_GetItemVisuals
    {
        [HarmonyPrefix]
        public static bool Prefix(Item __instance, bool _special, ref Transform __result)
        {
            try
            {
                if (CustomItemVisuals.GetItemVisualLink(__instance) is CustomItemVisuals.ItemVisualsLink link)
                {
                    if (!_special)
                    {
                        if (link.ItemVisuals)
                        {
                            __result = link.ItemVisuals;
                            return false;
                        }
                    }
                    else
                    {
                        if (__instance.UseSpecialVisualFemale)
                        {
                            if (link.ItemSpecialFemaleVisuals)
                            {
                                __result = link.ItemSpecialFemaleVisuals;
                                return false;
                            }
                        }
                        else if (link.ItemSpecialVisuals)
                        {
                            __result = link.ItemSpecialVisuals;
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                SL.LogInnerException(e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Item), "ItemIcon", MethodType.Getter)]
    public class Item_ItemIcon
    {
        [HarmonyPrefix]
        public static bool Prefix(Item __instance, ref Sprite __result)
        {
            if (CustomItemVisuals.GetItemVisualLink(__instance) is CustomItemVisuals.ItemVisualsLink link
                && link.ItemIcon)
            {
                __result = link.ItemIcon;
                return false;
            }

            return true;
        }
    }

    #endregion

    #region SL_STATUSEFFECT PATCHES

    /// <summary>
    /// This patch is to fix StatusEffect prefab references on AddStatus components, for when editing an existing StatusEffect.
    /// </summary>
    [HarmonyPatch(typeof(Effect), "OnEnable")]
    public class Effect_OnEnable
    {
        [HarmonyPrefix]
        public static void Prefix(Effect __instance)
        {
            if (__instance is AddStatusEffect addStatusEffect && addStatusEffect.Status is StatusEffect _old)
            {
                if (References.RPM_STATUS_EFFECTS.ContainsKey(_old.IdentifierName))
                {
                    if (ResourcesPrefabManager.Instance.GetStatusEffectPrefab(_old.IdentifierName) is StatusEffect _new)
                        addStatusEffect.Status = _new;

                    if (__instance is AddBoonEffect addBoon && addBoon.BoonAmplification is StatusEffect _oldAmp)
                    {
                        if (ResourcesPrefabManager.Instance.GetStatusEffectPrefab(_oldAmp.IdentifierName) is StatusEffect _newAmp)
                            addBoon.BoonAmplification = _newAmp;
                    }
                }
            }
        }
    }

    #endregion

    #region SL_SHOOTER PATCHES

    /// <summary>
    /// This patch fixes some things for Blast and Projectile EffectSynchronizers.
    /// Due to the way we clone and setup these prefabs, we need to fix a few things here.
    /// 1) Set the ParentEffect (the ShootBlast or ShootProjectile)
    /// 2) Re-enable the prefab.
    /// </summary>
    [HarmonyPatch(typeof(Shooter), "Setup", new Type[] { typeof(TargetingSystem), typeof(Transform) })]
    public class ShootProjectile_Setup
    {
        [HarmonyPrefix]
        public static void Prefix(Shooter __instance)
        {
            if (__instance is ShootProjectile shootProjectile && shootProjectile.BaseProjectile is Projectile projectile && !projectile.gameObject.activeSelf)
            {
                At.SetField(projectile as SubEffect, "m_parentEffect", __instance);

                projectile.gameObject.SetActive(true);

            }
            else if (__instance is ShootBlast shootBlast && shootBlast.BaseBlast is Blast blast && !blast.gameObject.activeSelf)
            {
                At.SetField(blast as SubEffect, "m_parentEffect", __instance);

                blast.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Patch for ShootBlastHornetControl to allow them to end based on the Lifespan.
    /// </summary>
    [HarmonyPatch(typeof(ShootBlastHornetControl), "Update")]
    public class ShootBlastHornetControl_Update
    {
        [HarmonyPostfix]
        public static void Postfix(ShootBlastHornetControl __instance, bool ___m_hornetsOut)
        {
            var lifespan = __instance.BlastLifespan;

            if (!___m_hornetsOut || lifespan <= 0)
            {
                // effect is not active. return.
                return;
            }

            var subs = (SubEffect[])At.GetField(__instance as Effect, "m_subEffects");
            if (subs != null && subs.Length > 0)
            {
                var blast = subs[0] as Blast;

                // effect is active, but blast has been disabled. stop effect.
                if (!blast.gameObject.activeSelf)
                {
                    At.Invoke(__instance, "Stop");
                }
            }
        }
    }

    #endregion

    #region SL_CHARACTER PATCHES

    // Save custom characters when game does a save
    [HarmonyPatch(typeof(SaveInstance), nameof(SaveInstance.Save))]
    public class SaveInstance_Save
    {
        [HarmonyPostfix]
        public static void Postfix(SaveInstance __instance)
        {
            var worldhost = CharacterManager.Instance.GetWorldHostCharacter();
            var character = __instance.CharSave.CharacterUID;

            if (character != worldhost.UID)
                return;

            CustomCharacters.SaveCharacters();
        }
    }

    // Just catches a harmless null ref exception, hiding it until I figure out a cleaner fix
    [HarmonyPatch(typeof(Character), "ProcessOnEnable")]
    public class Character_ProcessOnEnable
    {
        [HarmonyFinalizer]
        public static Exception Finalizer()
        {
            return null;
        }
    }

    // This harmony patch is to sneak into when the game applies characters.
    // I figure it's best to do it at the same time.
    [HarmonyPatch(typeof(NetworkLevelLoader), "MidLoadLevel")]
    public class NetworkLevelLoader_MidLoadLevel
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            CustomCharacters.InvokeSpawnCharacters();
        }
    }

    [HarmonyPatch(typeof(NetworkLevelLoader), nameof(NetworkLevelLoader.JoinSequenceDone))]
    public class NetworkLevelLoader_JoinSequenceDone
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (PhotonNetwork.isNonMasterClientInRoom)
                CustomCharacters.RequestSpawnedCharacters();
        }
    }

    // Like the last patch, we sneak into when the game should have destroyed previous scene characters to cleanup there.
    [HarmonyPatch(typeof(CharacterManager), "ClearNonPersitentCharacters")]
    [HarmonyPatch(typeof(NetworkLevelLoader), "StartConnectionCoroutine")]
    [HarmonyPatch(typeof(NetworkLevelLoader), "HostLost")]
    [HarmonyPatch(typeof(NetworkLevelLoader), "StartLoadLevel")]
    public class MultiPatch_CleanupCharacters
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            CustomCharacters.CleanupCharacters();
        }
    }

    #endregion

    #region AUDIO PATCHES

    [HarmonyPatch(typeof(GlobalAudioManager), "CleanUpMusic")]
    public class GAM_CleanupMusic
    {
        [HarmonyPrefix]
        public static bool Prefix(ref DictionaryExt<GlobalAudioManager.Sounds, GlobalAudioManager.MusicSource> ___s_musicSources,
            ref GlobalAudioManager.Sounds ___s_currentMusic)
        {
            string name = SceneManager.GetActiveScene().name;
            for (int i = 0; i < ___s_musicSources.Values.Count; i++)
            {
                var key = ___s_musicSources.Keys[i];
                var value = ___s_musicSources.Values[i];

                if (key != ___s_currentMusic && value.SceneName != name)
                {
                    if (CustomAudio.ReplacedClips.Contains(key))
                    {
                        //SL.Log("Game tried to clean up " + key + ", but we skipped it!");
                        continue;
                    }

                    UnityEngine.Object.Destroy(value.Source.gameObject);
                    ___s_musicSources.Remove(key);
                    i--;
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(GlobalAudioManager), "ReplaceClip")]
    public class GAM_ReplaceClip
    {
        [HarmonyPrefix]
        public static bool Prefix(GlobalAudioManager.Sounds _sound, AudioClip _newCLip)
        {
            if (CustomAudio.ReplacedClips.Contains(_sound))
            {
                SL.Log("Game tried to replace " + _sound + ", but it is already replaced with a custom sound! Skipping...");
                return false;
            }

            return true;
        }
    }

    #endregion


    #region CUSTOM SKILLS F3 MENU FIX

    [HarmonyPatch(typeof(DT_SkillProficiencyCheats), nameof(DT_SkillProficiencyCheats.Show))]
    public class DT_SkillProfiencyCheats_Show
    {
        [HarmonyPrefix]
        public static void Prefix(ref List<Skill> ___m_allSkills)
        {
            if (___m_allSkills == null)
                ___m_allSkills = ResourcesPrefabManager.Instance.EDITOR_GetPlayerSkillPrefabs();

            foreach (var skill in SL.s_customSkills)
            {
                if (!___m_allSkills.Contains(skill.Value))
                {
                    //SL.LogWarning("Adding custom skill to F3 menu: " + skill.Value.Name);
                    ___m_allSkills.Add(skill.Value);
                }
            }
        }

        [HarmonyPostfix]
        public static void Postfix(DT_SkillProficiencyCheats __instance, List<DT_SkillDisplayCheat> ___m_skillDisplays,
            DT_SkillDisplayCheat ___m_skillDisplayTemplate, CharacterUI ___m_characterUI, List<int> ___m_knownSkillDisplay,
            List<int> ___m_unknownSkillDisplay, RectTransform ___m_knownSkillHolder, RectTransform ___m_unknownSkillHolder)
        {
            foreach (var skill in SL.s_customSkills)
            {
                if (!skill.Value)
                    continue;

                var query = ___m_skillDisplays.Where(it => it.RefSkill == skill.Value);

                if (!query.Any())
                {
                    //SL.LogWarning("Adding custom skill display to F3 menu: " + skill.Value.Name);

                    var display = UnityEngine.Object.Instantiate(___m_skillDisplayTemplate);
                    display.SetReferencedSkill(skill.Value);
                    display.SetCharacterUI(___m_characterUI);
                    ___m_skillDisplays.Add(display);

                    if (__instance.LocalCharacter.Inventory.SkillKnowledge.IsItemLearned(skill.Value.ItemID))
                    {
                        ___m_knownSkillDisplay.Add(___m_skillDisplays.IndexOf(display));
                        display.transform.SetParent(___m_knownSkillHolder);
                        display.transform.ResetLocal();
                    }
                    else
                    {
                        ___m_unknownSkillDisplay.Add(___m_skillDisplays.IndexOf(display));
                        display.transform.SetParent(___m_unknownSkillHolder);
                        display.transform.ResetLocal();
                    }

                    display.gameObject.SetActive(true);
                }
                else
                {
                    foreach (var result in query)
                        result.gameObject.SetActive(true);
                }
            }
        }
    }

    #endregion


    //#region SL_SCENE PATCHES

    //// This is to fix some things when loading custom scenes.
    //// Note: There is another patch on this method for SL_Characters which is a Postfix, this one is a Prefix.
    //[HarmonyPatch(typeof(NetworkLevelLoader), "MidLoadLevel")]
    //public class NetworkLevelLoader_MidLoadLevel_2
    //{
    //    [HarmonyPostfix]
    //    public static void Prefix()
    //    {
    //        if (CustomScenes.IsLoadingCustomScene && CustomScenes.IsRealScene(SceneManager.GetActiveScene()))
    //        {
    //            CustomScenes.PopulateNecessarySceneContents();
    //        }
    //    }
    //}

    //#endregion
}
