﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SideLoader
{
    public class SL_PassiveSkillCondition : SL_EffectCondition
    {
        public int ReqSkillID;

        public override void ApplyToComponent<T>(T component)
        {
            var skill = ResourcesPrefabManager.Instance.GetItemPrefab(ReqSkillID) as PassiveSkill;

            if (!skill)
            {
                SL.Log("SL_PassiveSkillCondition: Could not find a Passive Skill with the ID " + this.ReqSkillID, 0);
                return;
            }

            (component as PassiveSkillCondition).Inverse = this.Invert;
            (component as PassiveSkillCondition).PassiveSkill = skill;
        }

        public override void SerializeEffect<T>(EffectCondition component, T template)
        {
            (template as SL_PassiveSkillCondition).Invert = (component as PassiveSkillCondition).Inverse;
            (template as SL_PassiveSkillCondition).ReqSkillID = (component as PassiveSkillCondition).PassiveSkill.ItemID;
        }
    }
}