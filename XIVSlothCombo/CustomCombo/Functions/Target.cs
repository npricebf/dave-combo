﻿using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using XIVSlothCombo.Services;

namespace XIVSlothCombo.CustomComboNS.Functions
{
    internal abstract partial class CustomComboFunctions
    {
        /// <summary> Gets the current target or null. </summary>
        public static GameObject? CurrentTarget => Service.TargetManager.Target;

        /// <summary> Find if the player has a target. </summary>
        /// <returns> A value indicating whether the player has a target. </returns>
        public static bool HasTarget() => CurrentTarget is not null;

        /// <summary> Gets the distance from the target. </summary>
        /// <returns> Double representing the distance from the target. </returns>
        public double GetTargetDistance()
        {
            if (CurrentTarget is null || LocalPlayer is null)
                return 0;

            if (CurrentTarget is not BattleChara chara)
                return 0;

            if (CurrentTarget.ObjectId == LocalPlayer.ObjectId)
                return 0;

            var position = new Vector2(chara.Position.X, chara.Position.Z);
            var selfPosition = new Vector2(LocalPlayer.Position.X, LocalPlayer.Position.Z);

            return Math.Max(0, (Vector2.Distance(position, selfPosition) - chara.HitboxRadius) - LocalPlayer.HitboxRadius);
        }

        /// <summary> Gets a value indicating whether you are in melee range from the current target. </summary>
        /// <returns> Bool indicating whether you are in melee range. </returns>
        public bool InMeleeRange()
        {
            if (LocalPlayer.TargetObject == null) return false;

            var distance = GetTargetDistance();

            if (distance == 0)
                return true;

            if (distance > 3 + Service.Configuration.MeleeOffset)
                return false;

            return true;
        }

        /// <summary> Gets a value indicating target's HP Percent. CurrentTarget is default unless specified </summary>
        /// <returns> Double indicating percentage. </returns>
        public static double GetTargetHPPercent(GameObject? OurTarget = null)
        {
            if (OurTarget is null)
            {
                //Fallback to CurrentTarget
                OurTarget = CurrentTarget;
                if (OurTarget is null) return 0;
            }
            if (OurTarget is not BattleChara chara)
                return 0;

            double health = chara.CurrentHp;
            double maxHealth = chara.MaxHp;

            return health / maxHealth * 100;
        }

        public static double EnemyHealthMaxHp()
        {
            if (CurrentTarget is null)
                return 0;
            if (CurrentTarget is not BattleChara chara)
                return 0;

            double maxHealth = chara.MaxHp;

            return maxHealth;
        }

        public static double EnemyHealthCurrentHp()
        {
            if (CurrentTarget is null)
                return 0;
            if (CurrentTarget is not BattleChara chara)
                return 0;

            double currentHp = chara.CurrentHp;

            return currentHp;
        }

        public double PlayerHealthPercentageHp()
        {
            double maxHealth = LocalPlayer.MaxHp;
            double currentHealth = LocalPlayer.CurrentHp;

            return currentHealth / maxHealth * 100;
        }

        public static bool HasBattleTarget() 
            => (CurrentTarget as BattleNpc)?.BattleNpcKind is BattleNpcSubKind.Enemy;

        public static bool HasFriendlyTarget(GameObject? OurTarget = null)
        {
            if (OurTarget is null)
            {
                //Fallback to CurrentTarget
                OurTarget = CurrentTarget;
                if (OurTarget is null) return false;

                //Humans
                if (OurTarget.ObjectKind is ObjectKind.Player) return true;
                //Trust & Chocobo
                return (OurTarget as BattleNpc)?.BattleNpcKind is not BattleNpcSubKind.Enemy;
                //if (OurTarget is BattleNpc) return (OurTarget as BattleNpc).BattleNpcKind is not BattleNpcSubKind.Enemy;
            }
            return false;
        }

        /// <summary> Determines if the enemy can be interrupted if they are currently casting. </summary>
        /// <returns> Bool indicating whether they can be interrupted or not. </returns>
        public static bool CanInterruptEnemy()
        {
            if (CurrentTarget is null)
                return false;
            if (CurrentTarget is not BattleChara chara)
                return false;
            if (chara.IsCasting)
                return chara.IsCastInterruptible;
            return false;
        }

        /// <summary> Sets the player's target. </summary>
        /// <param name="target"> Target must be a game object that the player can normally click and target. </param>
        public static void SetTarget(GameObject? target) =>
            Service.TargetManager.Target = target;

        /// <summary> Checks if target is in appropriate range for targeting </summary>
        /// <param name="target"> The target object to check </param>
        public static bool IsInRange(GameObject? target)
        {
            if (target == null) return false;
            if (target.YalmDistanceX >= 30) return false;

            return true;
        }

        /// <summary> Attempts to target the given party member </summary>
        /// <param name="target"></param>
        protected unsafe void TargetObject(TargetType target)
        {
            var t = GetTarget(target);
            if (t == null) return;
            var o = PartyTargetingService.GetObjectID(t);
            var p = Service.ObjectTable.Where(x => x.ObjectId == o).First();

            if (IsInRange(p)) SetTarget(p);
        }

        public static void TargetObject(GameObject? target)
        {
            if (IsInRange(target)) SetTarget(target);
        }

        protected unsafe static FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* GetTarget(TargetType target)
        {
            GameObject? o = null;

            switch (target)
            {
                case TargetType.Target:
                    o = Service.TargetManager.Target;
                    break;
                case TargetType.SoftTarget:
                    o = Service.TargetManager.SoftTarget;
                    break;
                case TargetType.FocusTarget:
                    o = Service.TargetManager.FocusTarget;
                    break;
                case TargetType.UITarget:
                    return PartyTargetingService.UITarget;
                case TargetType.FieldTarget:
                    o = Service.TargetManager.MouseOverTarget;
                    break;
                case TargetType.TargetsTarget when Service.TargetManager.Target is { TargetObjectId: not 0xE0000000 }:
                    o = Service.TargetManager.Target.TargetObject;
                    break;
                case TargetType.Self:
                    o = Service.ClientState.LocalPlayer;
                    break;
                case TargetType.LastTarget:
                    return PartyTargetingService.GetGameObjectFromPronounID(1006);
                case TargetType.LastEnemy:
                    return PartyTargetingService.GetGameObjectFromPronounID(1084);
                case TargetType.LastAttacker:
                    return PartyTargetingService.GetGameObjectFromPronounID(1008);
                case TargetType.P2:
                    return PartyTargetingService.GetGameObjectFromPronounID(44);
                case TargetType.P3:
                    return PartyTargetingService.GetGameObjectFromPronounID(45);
                case TargetType.P4:
                    return PartyTargetingService.GetGameObjectFromPronounID(46);
                case TargetType.P5:
                    return PartyTargetingService.GetGameObjectFromPronounID(47);
                case TargetType.P6:
                    return PartyTargetingService.GetGameObjectFromPronounID(48);
                case TargetType.P7:
                    return PartyTargetingService.GetGameObjectFromPronounID(49);
                case TargetType.P8:
                    return PartyTargetingService.GetGameObjectFromPronounID(50);
            }

            return o != null ? (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)o.Address : null;
        }

        public enum TargetType
        {
            Target,
            SoftTarget,
            FocusTarget,
            UITarget,
            FieldTarget,
            TargetsTarget,
            Self,
            LastTarget,
            LastEnemy,
            LastAttacker,
            P2,
            P3,
            P4,
            P5,
            P6,
            P7,
            P8
        }
    }
}
