﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SideLoader
{
    public class SL_ProximitySoulSpotCondition : SL_EffectCondition
    {
        public float Distance;

        public override void ApplyToComponent<T>(T component)
        {
            (component as ProximitySoulSpotCondition).ProximityDist = this.Distance;
        }

        public override void SerializeEffect<T>(EffectCondition component, T template)
        {
            (template as SL_ProximitySoulSpotCondition).Distance = (component as ProximitySoulSpotCondition).ProximityDist;
        }
    }
}