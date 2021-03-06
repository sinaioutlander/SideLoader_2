﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SideLoader
{
    public class SL_ShootProjectile : SL_Shooter
    {
        public ProjectilePrefabs BaseProjectile;
        public SL_ProjectileShot[] ProjectileShots;
        public int InstantiatedAmount = 1;

        public float Lifespan;
        public float LateShootTime;
        public bool Unblockable;
        public bool EffectsOnlyIfHitCharacter;
        public Projectile.EndLifeMode EndMode;
        public bool DisableOnHit;
        public bool IgnoreShooterCollision;

        public ShootProjectile.TargetMode TargetingMode;
        public int TargetCountPerProjectile;
        public float TargetRange;
        public bool AutoTarget;
        public float AutoTargetMaxAngle;
        public float AutoTargetRange;

        public float ProjectileForce;
        public Vector3 AddDirection;
        public Vector3 AddRotationForce;
        public float YMagnitudeAffect;
        public float YMagnitudeForce;
        public float DefenseLength;
        public float DefenseRange;
        public float CameraAddYDirection;

        public EquipmentSoundMaterials ImpactSoundMaterial;
        public Vector2 LightIntensityFade;
        public Vector3 PointOffset;
        public bool TrailEnabled;
        public float TrailTime;

        public EditBehaviours EffectBehaviour = EditBehaviours.Override;
        public SL_EffectTransform[] ProjectileEffects;

        public override void ApplyToComponent<T>(T component)
        {
            base.ApplyToComponent(component);

            var comp = component as ShootProjectile;

            comp.IntanstiatedAmount = this.InstantiatedAmount;
            comp.AddDirection = this.AddDirection;
            comp.AddRotationForce = this.AddRotationForce;
            comp.AutoTarget = this.AutoTarget;
            comp.AutoTargetMaxAngle = this.AutoTargetMaxAngle;
            comp.AutoTargetRange = this.AutoTargetRange;
            comp.IgnoreShooterCollision = this.IgnoreShooterCollision;
            comp.ProjectileForce = this.ProjectileForce;
            comp.TargetCountPerProjectile = this.TargetCountPerProjectile;
            comp.TargetingMode = this.TargetingMode;
            comp.TargetRange = this.TargetRange;
            comp.YMagnitudeAffect = this.YMagnitudeAffect;
            comp.YMagnitudeForce = this.YMagnitudeForce;
            comp.CameraAddYDirection = this.CameraAddYDirection;

            if (this.ProjectileShots != null)
            {
                var list = new List<ProjectileShot>();
                foreach (var shot in this.ProjectileShots)
                {
                    list.Add(new ProjectileShot()
                    {
                        RandomLocalDirectionAdd = shot.RandomLocalDirectionAdd,
                        LocalDirectionOffset = shot.LocalDirectionOffset,
                        LockDirection = shot.LockDirection,
                        MustShoot = shot.MustShoot,
                        NoBaseDir = shot.NoBaseDir
                    });
                }
                comp.ProjectileShots = list.ToArray();
            }

            if (GetProjectilePrefab(this.BaseProjectile) is GameObject projectile)
            {
                var copy = GameObject.Instantiate(projectile);
                GameObject.DontDestroyOnLoad(copy);
                copy.SetActive(false);

                var newProjectile = copy.GetComponent<Projectile>();
                comp.BaseProjectile = newProjectile;

                newProjectile.DefenseLength = this.DefenseLength;
                newProjectile.DefenseRange = this.DefenseRange;
                newProjectile.DisableOnHit = this.DisableOnHit;
                newProjectile.EffectsOnlyIfHitCharacter = this.EffectsOnlyIfHitCharacter;
                newProjectile.EndMode = this.EndMode;
                newProjectile.LateShootTime = this.LateShootTime;
                newProjectile.Lifespan = this.Lifespan;
                newProjectile.LightIntensityFade = this.LightIntensityFade;
                newProjectile.PointOffset = this.PointOffset;
                newProjectile.TrailEnabled = this.TrailEnabled;
                newProjectile.TrailTime = this.TrailTime;
                newProjectile.Unblockable = this.Unblockable;

                newProjectile.ImpactSoundMaterial = this.ImpactSoundMaterial;
                if (newProjectile.GetComponentInChildren<ImpactSoundPlayer>() is ImpactSoundPlayer player)
                {
                    player.SoundMaterial = this.ImpactSoundMaterial;
                }

                SL_EffectTransform.ApplyTransformList(newProjectile.transform, ProjectileEffects, EffectBehaviour);
            }
        }

        public override void SerializeEffect<T>(T effect)
        {
            base.SerializeEffect(effect);

            var comp = effect as ShootProjectile;

            AddDirection = comp.AddDirection;
            AddRotationForce = comp.AddRotationForce;
            AutoTarget = comp.AutoTarget;
            AutoTargetMaxAngle = comp.AutoTargetMaxAngle;
            AutoTargetRange = comp.AutoTargetRange;
            IgnoreShooterCollision = comp.IgnoreShooterCollision;
            ProjectileForce = comp.ProjectileForce;
            TargetCountPerProjectile = comp.TargetCountPerProjectile;
            TargetingMode = comp.TargetingMode;
            TargetRange = comp.TargetRange;
            YMagnitudeAffect = comp.YMagnitudeAffect;
            YMagnitudeForce = comp.YMagnitudeForce;
            InstantiatedAmount = comp.IntanstiatedAmount;
            CameraAddYDirection = comp.CameraAddYDirection;

            var prefabEnum = GetProjectilePrefabEnum(comp.BaseProjectile);

            if (prefabEnum != ProjectilePrefabs.NONE)
            {
                var proj = comp.BaseProjectile;

                BaseProjectile = prefabEnum;
                DefenseLength = proj.DefenseLength;
                DefenseRange = proj.DefenseRange;
                DisableOnHit = proj.DisableOnHit;
                EffectsOnlyIfHitCharacter = proj.EffectsOnlyIfHitCharacter;
                EndMode = proj.EndMode;
                ImpactSoundMaterial = proj.ImpactSoundMaterial;
                LateShootTime = proj.LateShootTime;
                Lifespan = proj.Lifespan;
                LightIntensityFade = proj.LightIntensityFade;
                PointOffset = proj.PointOffset;
                TrailEnabled = proj.TrailEnabled;
                TrailTime = proj.TrailTime;
                Unblockable = proj.Unblockable;

                var list = new List<SL_EffectTransform>();
                foreach (Transform child in proj.transform)
                {
                    var effectsChild = SL_EffectTransform.ParseTransform(child);

                    if (effectsChild.HasContent)
                    {
                        list.Add(effectsChild);
                    }
                }
                ProjectileEffects = list.ToArray();
            }
            else if (comp.BaseProjectile)
            {
                SL.Log("Couldn't parse blast prefab to enum: " + comp.BaseProjectile.name);
            }

            var shots = new List<SL_ProjectileShot>();
            foreach (var shot in comp.ProjectileShots)
            {
                shots.Add(new SL_ProjectileShot()
                {
                    RandomLocalDirectionAdd = shot.RandomLocalDirectionAdd,
                    LocalDirectionOffset = shot.LocalDirectionOffset,
                    LockDirection = shot.LockDirection,
                    MustShoot = shot.MustShoot,
                    NoBaseDir = shot.NoBaseDir
                });
            }
            ProjectileShots = shots.ToArray();
        }

        public class SL_ProjectileShot
        {
            public Vector3 LocalDirectionOffset;
            public Vector3 LockDirection;
            public bool MustShoot;
            public bool NoBaseDir;
            public Vector3 RandomLocalDirectionAdd;
        }



        // ============ Projectiles Dictionary ============ //

        private static bool m_initDone = false;

        internal static Dictionary<ProjectilePrefabs, GameObject> ProjectilePrefabCache = new Dictionary<ProjectilePrefabs, GameObject>();

        public static GameObject GetProjectilePrefab(ProjectilePrefabs name)
        {
            if (name != ProjectilePrefabs.NONE)
            {
                return ProjectilePrefabCache[name];
            }
            else
            {
                return null;
            }
        }

        public static void BuildProjectileDictionary()
        {
            if (m_initDone)
            {
                return;
            }

            foreach (var projectile in Resources.FindObjectsOfTypeAll<Projectile>())
            {
                var name = GetProjectilePrefabEnum(projectile);

                if (name == ProjectilePrefabs.NONE)
                {
                    SL.Log("Couldn't parse projectile prefab: " + projectile.name);
                }
                else if (!ProjectilePrefabCache.ContainsKey(name))
                {
                    bool wasActive = projectile.gameObject.activeSelf;
                    projectile.gameObject.SetActive(false);

                    var copy = GameObject.Instantiate(projectile.gameObject);
                    GameObject.DontDestroyOnLoad(copy);
                    copy.SetActive(false);

                    ProjectilePrefabCache.Add(name, copy);

                    projectile.gameObject.SetActive(wasActive);
                }
            }

            m_initDone = true;
        }

        /// <summary>
        /// Helper to take a Projectile and get the ProjectilePrefabs enum value for it (if valid).
        /// </summary>
        /// <param name="projectile">The projectile prefab</param>
        public static ProjectilePrefabs GetProjectilePrefabEnum(Projectile projectile)
        {
            if (!projectile)
            {
                return ProjectilePrefabs.NONE;
            }

            var prefabName = projectile.name.Replace("(Clone)", "").Replace(" ", "_").Trim();

            if (Enum.TryParse(prefabName, out ProjectilePrefabs name))
            {
                return name;
            }
            else
            {
                return ProjectilePrefabs.NONE;
            }
        }

        public enum ProjectilePrefabs
        {
            NONE,
            AncientDwellerPortalMine,
            AncientDwellerPortalMineWithSound,
            AncientDwellerProjectile,
            AshPriestCirclingMine,
            BeetleFrostball,
            BeetleFrostSpread,
            BloodMageHeal,
            BoozuDecayTrash,
            BoozuProudBeasttPreBlast,
            BoozuProudBeasttPreBlastBig,
            BulletBlood,
            BulletBloodSyphonProjectile,
            BulletLightning,
            BulletNormal,
            BulletShatter,
            CageBlast,
            CageBossPreBlast,
            CageBossProjectile,
            ChimesProjectile,
            CorruptionSpiritShot,
            CrimsonBoonMine,
            CrimsonBoonSun,
            CrimsonEliteDodgeMine,
            CrimsonEliteDodgeProjectile,
            CrimsonEliteMine,
            CrimsonEliteMinePowered,
            CrimsonEliteMinePowered2,
            CrimsonElitePreSun,
            CrimsonElitePreSunPowered,
            CrimsonEliteSunProjectil,
            CrimsonEliteSunProjectilPowered,
            CrimsonMine,
            CrimsonMinePowered,
            CrimsonPreSun,
            CrimsonPreSunPowered,
            CrimsonSunProjectil,
            CrimsonSunProjectilPowered,
            DjinBigMeleetPreBlast,
            DjinBigMeleetPreBlastBig,
            DjinHomming,
            DjinWave,
            DrumProjectile,
            ElementalBuffProjDecay,
            ElementalBuffProjEthereal,
            ElementalBuffProjFire,
            ElementalBuffProjFrost,
            ElementalBuffProjLight,
            ElementalParasiteMortar,
            ElementalProjectileBolt,
            ElementalProjectileDecay,
            ElementalProjectileEthereal,
            ElementalProjectileFire,
            ElementalProjectileIce,
            ElementalProjectileLight,
            ElementalProjectilePoison,
            ElementalProjectileWind,
            ElementalShot1Decay,
            ElementalShot1Ethereal,
            ElementalShot1Fire,
            ElementalShot1Frost,
            ElementalShot1Light,
            ElementalShot3Decay,
            ElementalShot3Ethereal,
            ElementalShot3Fire,
            ElementalShot3Frost,
            ElementalShot3Light,
            EliteBurningManHoming,
            EliteCalixaGunShot,
            EliteCalixaJumpDecayHomming,
            EliteCalixaJumpElectricHomming,
            EliteCalixaJumpFireHomming,
            EliteCalixaJumpIceHomming,
            EliteForgeGolemFirebolt,
            EliteForgeGolemIcebolt,
            EliteSharkMine,
            EliteShrimpLightningBolt,
            EliteTrogBoonHealProjectile,
            EliteTrogGrenade,
            EliteTrogQueenBoonHealProjectile,
            EliteTrogQueenPoisonProjectile,
            EliteTuanosaurFireball,
            EvasiveShotProjectile,
            Fireball,
            FireballBeetle,
            FireboltBeetleFireworks,
            FireboltForgeGolem,
            ForgeGolemRustLichMinionProjectile,
            GargoyleProjectile,
            GargoyleProjectile_1,
            GargoyleProjectile_2,
            GargoyleProjectileFinal,
            GargoyleProjectileSide,
            GateProjectile,
            GiantHunterBolt,
            GolemShieldedBolt,
            GolemShieldedHealProjectile,
            GolemShieldedHomming,
            GolemShieldedHommingShortRange,
            GolemShieldedMortar,
            GolemShieldedMortarDown,
            GrandmotherProjectile,
            GrandmotherWave,
            Grenade,
            GrenadeBlazing,
            GrenadeFrost,
            GrenadeIncendiary,
            GrenadeNapalm,
            GrenadeNerve,
            GrenadeShrapnel,
            GrenadeSpark,
            GrenadeToxin,
            IceWitchIcicleProjectile,
            IcicleProjectile,
            IlluminatorForwardMine,
            IlluminatorHealProjectile,
            IlluminatorMine,
            ImmaculateMaceWave,
            ImmaculateSwordMine,
            ImmaculateSwordMineProjectile,
            JellyGreenMorphingProjectile1,
            JellyGreenMorphingProjectile2,
            JellyGreenMorphingProjectile3,
            JinxProjectile,
            JosefBulletShatter,
            KrypteiaFrostProjectile,
            KrypteiaGrenade,
            KrypteiaMine,
            KrypteiaThunderProjectile,
            LichGoldLightningBolt,
            LichGoldStaffBolt,
            LichJadeCirclingMine,
            LichJadeHomming,
            LichJadeMine,
            LichRustProjectileEthereal,
            LichRustTeleportPreBlast,
            LionElderBolt,
            LionLightningBolt,
            LionmanElderProjectile,
            ManticoreSpike,
            MarshGuardianCircle,
            MultitargetStrike,
            ObsidianHoming,
            PierceShot,
            ProjectileArrow,
            PureIlluminatorAllieProjectile,
            PureIlluminatorBoonProjectile,
            PureIlluminatorHommingMine,
            RunicRayAmplifiedProjectile,
            RunicRayProjectile,
            ShrimpLightningBolt,
            SlughellFireball,
            SlughellIceball,
            SlughellProjectile,
            SniperShot,
            SpecterLightBolt,
            SquireFireWave,
            SquireTendrils,
            StekoBoonHealProjectile,
            StrafingRunProjectile,
            Tendrils,
            TorcrabFireBall,
            TorcrabGiantSpeardShot,
            TorcrabGiantSpeardShotHigh,
            TrogBoonHealProjectile,
            TrogBoonProjectile,
            TrogGrenade,
            TrogHealProjectile,
            TrogHexProjectile,
            VendavelIcicleProjectile,
            WindGust,
        }

        public static void DebugProjectileNames()
        {
            var projs = Resources.FindObjectsOfTypeAll<Projectile>();
            var names = new List<string>();
            foreach (var proj in projs)
            {
                if (!names.Contains(proj.name))
                {
                    names.Add(proj.name);
                    //SL.Log(proj.name + ",");
                }
            }
            File.WriteAllLines("projectiles.txt", names.ToArray());
        }
    }
}
