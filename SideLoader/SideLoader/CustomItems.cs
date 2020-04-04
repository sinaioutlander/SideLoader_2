﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using Localizer;
using System.Reflection;

namespace SideLoader
{
    public class CustomItems : MonoBehaviour
    {
        public static CustomItems Instance;

        /// <summary> Cached ORIGINAL Item Prefabs (not modified) </summary>
        private static readonly Dictionary<int, Item> OrigItemPrefabs = new Dictionary<int, Item>();

        // Used to get tags more easily (by string instead of UID)
        private static readonly Dictionary<string, Tag> AllTags = new Dictionary<string, Tag>();

        /// <summary> cached ResourcesPrefabManager.ITEM_PREFABS Dictionary (reference to actual object) </summary>
        private static Dictionary<string, Item> RPM_ITEM_PREFABS;

        /// <summary> cached LocalizationManager.Instance.ItemLocalization </summary>
        private static Dictionary<int, ItemLocalization> ITEM_LOCALIZATION;

        internal void Awake()
        {
            Instance = this;

            // Cache useful dictionaries used by the game
            RPM_ITEM_PREFABS = At.GetValue(typeof(ResourcesPrefabManager), null, "ITEM_PREFABS") as Dictionary<string, Item>;
            ITEM_LOCALIZATION = At.GetValue(typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalization") as Dictionary<int, ItemLocalization>;

            var tags = At.GetValue(typeof(TagSourceManager), TagSourceManager.Instance, "m_tags") as Tag[];
            foreach (var tag in tags)
            {
                AllTags.Add(tag.TagName, tag);
            }

            // Hooks for bug fixing
            On.ItemListDisplay.SortBySupport += SortBySupportHook;
        }

        private int SortBySupportHook(On.ItemListDisplay.orig_SortBySupport orig, Item _item1, Item _item2)
        {
            try
            {
                return orig(_item1, _item2);
            }
            catch
            {
                return -1;
            }
        }

        // ============================================================================================ //
        /*                                       Public Helpers                                         */
        // ============================================================================================ //

        /// <summary> Will return the true original prefab for this Item ID. </summary>
        public static Item GetOriginalItemPrefab(int ItemID)
        {
            if (OrigItemPrefabs.ContainsKey(ItemID))
            {
                return OrigItemPrefabs[ItemID];
            }
            else
            {
                return ResourcesPrefabManager.Instance.GetItemPrefab(ItemID);
            }
        }

        /// <summary>
        /// Returns the game's actual Tag for the string you provide, if it exists.
        /// </summary>
        /// <param name="TagName">Eg "Food", "Blade", etc...</param>
        /// <returns></returns>
        public static Tag GetTag(string TagName)
        {
            if (AllTags.ContainsKey(TagName))
            {
                return AllTags[TagName];
            }
            else
            {
                SL.Log("GetTag :: Could not find a tag by the name: " + TagName);
                return Tag.None;
            }
        }

        // ============================================================================================ //
        /*                                   Setting up a Custom Item                                   */
        // ============================================================================================ //

        /// <summary>
        /// Clones an item prefab and returns the clone to you. Caches the original prefab for other mods or other custom items to reference.
        /// </summary>
        /// <param name="cloneTargetID">The Item ID of the Item you want to clone from</param>
        /// <param name="newID">The new Item ID for your cloned item. Can be the same as the target, will overwrite.</param>
        /// <param name="name">Only used for the gameObject name, not the actual Item Name. This is the name thats used in Debug Menus.</param>
        /// <param name="template">[Optional] If you want to apply a template for this item manually, you can provide it here.</param>
        /// <returns>Your cloned Item prefab</returns>
        public static Item CreateCustomItem(int cloneTargetID, int newID, string name, SL_Item template = null)
        {
            Item target;

            // Check if another Custom Item has already modified our target. If so, get the cached original.
            if (OrigItemPrefabs.ContainsKey(cloneTargetID))
            {
                //SL.Log("CustomItems::CreateCustomItem - The target Item has already been modified. Getting the original item.");
                target = OrigItemPrefabs[cloneTargetID];
            }
            else
            {
                target = ResourcesPrefabManager.Instance.GetItemPrefab(cloneTargetID);

                if (!target)
                {
                    SL.Log("CustomItems::CreateCustomItem - Error! Could not find the clone target Item ID: " + cloneTargetID, 1);
                    return null;
                }
            }            

            if (newID == cloneTargetID && !OrigItemPrefabs.ContainsKey(newID))
            {
                //SL.Log("CustomItems::CreateCustomItem - Modifying an original item for the first time, caching it.");
                OrigItemPrefabs.Add(target.ItemID, target);
            }

            var clone = Instantiate(target.gameObject).GetComponent<Item>();
            clone.gameObject.SetActive(false);
            DontDestroyOnLoad(clone);

            clone.gameObject.name = newID + "_" + name;

            clone.ItemID = newID;
            SetItemID(newID, clone);

            // fix for recipes (not sure if needed anymore?)
            if (!clone.GetComponent<TagSource>())
            {
                var tags = clone.gameObject.AddComponent<TagSource>();
                tags.RefreshTags();
            }

            if (template != null)
            {
                template.ApplyTemplateToItem();
            }

            return clone;
        }

        /// <summary>
        /// Fixes the ResourcesPrefabManager.ITEM_PREFABS dictionary for a custom Item ID. Will overwrite if the ID exists.
        /// This is called by CustomItems.CreateCustomItem
        /// </summary>
        /// <param name="ID">The Item ID you want to set</param>
        /// <param name="item">The Item prefab</param>
        public static void SetItemID(int ID, Item item)
        {
            //SL.Log("Setting a custom Item ID to the ResourcesPrefabManager dictionary. ID: " + ID + ", item name: " + item.Name);

            var idstring = ID.ToString();
            if (RPM_ITEM_PREFABS.ContainsKey(idstring))
            {
                RPM_ITEM_PREFABS[idstring] = item;
            }
            else
            {
                RPM_ITEM_PREFABS.Add(idstring, item);
            }
        }

        /// <summary> Helper for setting an Item's name easily </summary>
        public static void SetName(Item item, string name)
        {
            SetNameAndDescription(item, name, item.Description);
        }

        /// <summary> Helper for setting an Item's description easily </summary>
        public static void SetDescription(Item item, string description)
        {
            SetNameAndDescription(item, item.Name, description);
        }

        /// <summary> Set both name and description. Used by SetName and SetDescription. </summary>
        public static void SetNameAndDescription(Item _item, string _name, string _description)
        {
            var name = _name ?? "";
            var desc = _description ?? "";

            At.SetValue(name, typeof(Item), _item, "m_name");
            At.SetValue(desc, typeof(Item), _item, "m_description");

            ItemLocalization loc = new ItemLocalization(name, desc);

            if (ITEM_LOCALIZATION.ContainsKey(_item.ItemID))
            {
                ITEM_LOCALIZATION[_item.ItemID] = loc;
            }
            else
            {
                ITEM_LOCALIZATION.Add(_item.ItemID, loc);
            }
        }

        /// <summary> Adds the range of tags to the Items' TagSource, and optionally destroys the existing tags.</summary>
        public static void SetItemTags(Item item, List<string> tags, bool destroyExisting)
        {
            if (destroyExisting && item.GetComponent<TagSource>() is TagSource origTags)
            {
                GameObject.DestroyImmediate(origTags);
            }

            var tagsource = item.transform.GetOrAddComponent<TagSource>();
            tagsource.RefreshTags();

            var taglist = new List<TagSourceSelector>();
            foreach (var tag in tags)
            {
                taglist.Add(new TagSourceSelector(GetTag(tag)));
            }

            At.SetValue(taglist, typeof(TagListSelectorComponent), tagsource as TagListSelectorComponent, "m_tagSelectors");
        }

        /// <summary> Small helper for destroying all children on a given Transform 't'. Uses DestroyImmediate(). </summary>
        public static void DestroyChildren(Transform t)
        {
            while (t.childCount > 0)
            {
                DestroyImmediate(t.GetChild(0).gameObject);
            }
        }
    }        
}
