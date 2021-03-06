﻿using System.Collections.Generic;
using System.Linq;

namespace SideLoader
{
    public class SL_AttackSkill : SL_Skill
    {
        public Weapon.WeaponType[] AmmunitionTypes;
        public Weapon.WeaponType[] RequiredOffHandTypes;
        public Weapon.WeaponType[] RequiredWeaponTypes;
        public string[] RequiredWeaponTags;
        public bool? RequireImbue;

        public int? AmmunitionAmount;

        public override void ApplyToItem(Item item)
        {
            base.ApplyToItem(item);

            var attackSkill = item as AttackSkill;

            if (this.AmmunitionTypes != null)
                attackSkill.AmmunitionTypes = this.AmmunitionTypes.ToList();

            if (this.RequiredOffHandTypes != null)
                attackSkill.RequiredOffHandTypes = this.RequiredOffHandTypes.ToList();

            if (this.RequiredWeaponTypes != null)
                attackSkill.RequiredWeaponTypes = this.RequiredWeaponTypes.ToList();

            if (this.RequireImbue != null)
                attackSkill.RequireImbue = (bool)this.RequireImbue;

            if (this.AmmunitionAmount != null)
                attackSkill.AmmunitionAmount = (int)this.AmmunitionAmount;

            if (this.RequiredWeaponTags != null)
            {
                var list = new List<TagSourceSelector>();
                foreach (var tag in this.RequiredWeaponTags)
                {
                    if (CustomItems.GetTag(tag) is Tag _tag && _tag != Tag.None)
                    {
                        list.Add(new TagSourceSelector(_tag));
                    }
                }
                attackSkill.RequiredTags = list.ToArray();
            }
        }

        public override void SerializeItem(Item item)
        {
            base.SerializeItem(item);

            var attackSkill = item as AttackSkill;

            if (attackSkill.AmmunitionTypes != null)
                AmmunitionTypes = attackSkill.AmmunitionTypes.ToArray();

            if (attackSkill.RequiredOffHandTypes != null)
                RequiredOffHandTypes = attackSkill.RequiredOffHandTypes.ToArray();

            if (attackSkill.RequiredWeaponTypes != null)
                RequiredWeaponTypes = attackSkill.RequiredWeaponTypes.ToArray();

            RequireImbue = attackSkill.RequireImbue;

            if (attackSkill.RequiredTags != null)
            {
                var tagList = new List<string>();
                foreach (var tag in attackSkill.RequiredTags)
                {
                    tagList.Add(tag.Tag.TagName);
                }
                RequiredWeaponTags = tagList.ToArray();
            }
        }
    }
}
