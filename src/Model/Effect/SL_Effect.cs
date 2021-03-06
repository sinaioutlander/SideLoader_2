﻿using System;
using UnityEngine;

namespace SideLoader
{
    [SL_Serialized]
    public abstract class SL_Effect
    {
        /// <summary>The time, in seconds, after which the effects will be applied. Default is 0.</summary>
        public float Delay = 0f;
        /// <summary>Sync type determines the networking behaviour. Refer to an existing effect of this type if ensure.</summary>
        public Effect.SyncTypes SyncType = Effect.SyncTypes.OwnerSync;
        /// <summary>Override the SL_EffectTransform.TransformName category with a manual value.</summary>
        public EffectSynchronizer.EffectCategories OverrideCategory = EffectSynchronizer.EffectCategories.None;

        public abstract void ApplyToComponent<T>(T component) where T : Effect;

        /// <summary>Adds and applies this effect to the provided Transform.</summary>
        public Effect ApplyToTransform(Transform t)
        {
            Type componentType = Serializer.GetGameType(this.GetType());

            if (componentType != null)
            {
                var comp = t.gameObject.AddComponent(componentType) as Effect;

                // set base fields
                comp.Delay = this.Delay;
                comp.SyncType = this.SyncType;
                comp.OverrideEffectCategory = this.OverrideCategory;

                ApplyToComponent(comp);

                return comp;
            }
            else
            {
                SL.Log("Could not get Game type for SL_Type: " + this.ToString());
                return null;
            }
        }

        public abstract void SerializeEffect<T>(T effect) where T : Effect;

        /// <summary>Serialize an effect and get the equivalent SL_Effect.</summary>
        public static SL_Effect ParseEffect(Effect effect)
        {
            Type slType = Serializer.GetBestSLType(effect.GetType());

            if (slType != null && !slType.IsAbstract)
            {
                var holder = Activator.CreateInstance(slType) as SL_Effect;
                holder.Delay = effect.Delay;
                holder.OverrideCategory = effect.OverrideEffectCategory;
                holder.SyncType = effect.SyncType;

                holder.SerializeEffect(effect);
                return holder;
            }
            else
            {
                SL.Log(effect.ToString() + " is not supported yet, sorry!");
                return null;
            }
        }
    }
}
