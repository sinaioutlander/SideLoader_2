﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace SideLoader
{
    public class SL_Bag : SL_Equipment
    {
        public float? Capacity;
        public bool? Restrict_Dodge;
        public float? InventoryProtection;

        [Obsolete("Use SL_Preserver as SL_Item.ItemExtension instead!")]
        [XmlIgnore]
        public float? Preserver_Amount = -1;
        [Obsolete("Use SL_Preserver as SL_Item.ItemExtension instead!")]
        [XmlIgnore]
        public bool? Nullify_Perish = false;

        public override void ApplyToItem(Item item)
        {
            base.ApplyToItem(item);

            // set container capacity
            var container = item.transform.Find("Content").GetComponent<ItemContainerStatic>();
            if (this.Capacity != null)
            {
                At.SetValue((float)this.Capacity, typeof(ItemContainer), container, "m_baseContainerCapacity");
            }

            // set restrict dodge 
            if (this.Restrict_Dodge != null)
            {
                At.SetValue((bool)this.Restrict_Dodge, typeof(Bag), item, "m_restrictDodge");
            }

            // set invent prot
            At.SetValue(this.InventoryProtection, typeof(Bag), item, "m_inventoryProtection");

            //if (this.Preserver_Amount != null || this.Nullify_Perish == true)
            //{
            //    var preserver = container.transform.GetOrAddComponent<Preserver>();

            //    var nullperish = this.Nullify_Perish == null || this.Nullify_Perish == false;

            //    if (!nullperish)
            //    {
            //        var elements = new List<Preserver.PreservedElement>()
            //        {
            //            new Preserver.PreservedElement()
            //            {
            //                Preservation = (float)this.Preserver_Amount,
            //                Tag = new TagSourceSelector(CustomItems.GetTag("Food"))
            //            }
            //        };

            //        At.SetValue(elements, typeof(Preserver), preserver, "m_preservedElements");
            //    }
            //    else
            //    {
            //        preserver.NullifyPerishing = true;
            //    }
            //}
        }

        public override void SerializeItem(Item item, SL_Item holder)
        {
            base.SerializeItem(item, holder);

            var bag = item as Bag;
            var template = holder as SL_Bag;

            template.Capacity = bag.BagCapacity;
            template.Restrict_Dodge = bag.RestrictDodge;
            template.InventoryProtection = bag.InventoryProtection;

            //if (bag.GetComponentInChildren<Preserver>() is Preserver p
            //    && At.GetValue(typeof(Preserver), p, "m_preservedElements") is List<Preserver.PreservedElement> list && list.Count > 0)
            //{
            //    template.Preserver_Amount = list[0].Preservation;
            //    template.Nullify_Perish = p.NullifyPerishing;
            //}
        }
    }
}
