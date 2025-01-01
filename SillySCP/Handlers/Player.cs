﻿using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using InventorySystem;
using MEC;
using PlayerRoles;
using Scp914;
using SillySCP.API.Features;
using SillySCP.API.Interfaces;
using SillySCP.API.Modules;
using UnityEngine;
using Features = Exiled.API.Features;
using Random = UnityEngine.Random;

namespace SillySCP.Handlers
{
    public class Player : IRegisterable
    {
        public static Player Instance { get; private set; }
        
        public void Init()
        {
            Instance = this;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Died += ScpDeathHandler;
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
            Exiled.Events.Handlers.Scp914.UpgradingPlayer += OnUpgradingPlayer;
            Exiled.Events.Handlers.Scp914.UpgradingInventoryItem += OnScp914UpgradeInv;
            Exiled.Events.Handlers.Player.Escaping += OnEscaping;
            Exiled.Events.Handlers.Player.UsingItemCompleted += OnUsingItemCompleted;
        }

        public void Unregister()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.Died -= ScpDeathHandler;
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
            Exiled.Events.Handlers.Scp914.UpgradingPlayer -= OnUpgradingPlayer;
            Exiled.Events.Handlers.Scp914.UpgradingInventoryItem -= OnScp914UpgradeInv;
            Exiled.Events.Handlers.Player.Escaping -= OnEscaping;
            Exiled.Events.Handlers.Player.UsingItemCompleted -= OnUsingItemCompleted;
        }

        private void OnUpgradingPlayer(UpgradingPlayerEventArgs ev)
        {
            if (ev.KnobSetting == Scp914KnobSetting.Rough && ev.Player.CurrentItem == null)
            {
                ev.IsAllowed = false;
                Room randomRoom = Room.Get(ZoneType.LightContainment)
                    .Where(r => r.Type is not RoomType.Lcz330 and not RoomType.Lcz914 and not RoomType.LczArmory)
                    .GetRandomValue();
                
                ev.Player.Position = new (randomRoom.Position.x, randomRoom.Position.y + 1, randomRoom.Position.z);
                if (ev.Player.IsHuman)
                {
                    if (ev.Player.Health <= 25)
                    {
                        ev.Player.Kill(DamageType.Scp);
                        return;
                    }
                    ev.Player.EnableEffect(EffectType.Disabled, 1, 10);
                }
                else
                {
                    ev.Player.EnableEffect(EffectType.Flashed, 1, 10);
                }

                ev.Player.Health *= 0.75f;
            }
        }

        private void ScpDeathHandler(DiedEventArgs ev)
        {
            if (!ev.TargetOldRole.IsScp()) return;
            List<Features.Player> scps = Features.Player.List.Where(p => p.IsScp).ToList();
            if (scps.Count == 1 && scps.First().Role.Type == RoleTypeId.Scp079 &&
                !VolunteerSystem.ReadyVolunteers)
            {
                Scp079Recontainment.Recontain();
            }
        }
        
        private void OnUsingItemCompleted(UsingItemCompletedEventArgs ev)
        {
            Vector3 pos = ev.Player.Position;
            StatusEffectBase effectNormal = ev.Player.GetEffect(EffectType.Scp207);
            StatusEffectBase effectAnti = ev.Player.GetEffect(EffectType.AntiScp207);
            if (!effectNormal.IsEnabled || effectAnti.IsEnabled) return;
            if (ev.Item.Type == ItemType.SCP207 && effectAnti.Intensity > 1)
            {
                byte intensity = effectAnti.Intensity;
                Map.Explode(pos, ProjectileType.FragGrenade, ev.Player);
                if (intensity == 2)
                {
                    Map.Explode(pos, ProjectileType.FragGrenade, ev.Player);
                }
                else if (intensity == 3)
                {
                    Map.Explode(pos, ProjectileType.FragGrenade, ev.Player);
                    Map.Explode(pos, ProjectileType.FragGrenade, ev.Player);
                }
            }

            if (ev.Item.Type == ItemType.AntiSCP207 && effectNormal.Intensity > 1)
            {
                byte intensity = effectNormal.Intensity;
                Map.Explode(pos, ProjectileType.FragGrenade, ev.Player);
                if (intensity == 2)
                {
                    Map.Explode(pos, ProjectileType.FragGrenade, ev.Player);
                }
                else if (intensity == 3)
                {
                    Map.Explode(pos, ProjectileType.FragGrenade, ev.Player);
                    Map.Explode(pos, ProjectileType.FragGrenade, ev.Player);
                }
            }
        }

        private void OnEscaping(EscapingEventArgs ev)
        {
            if (ev.Player.Role.Type == RoleTypeId.FacilityGuard && ev.Player.IsCuffed)
            {
                ev.IsAllowed = true;
                ev.NewRole = RoleTypeId.ChaosConscript;
            }
            if(ev.Player.IsNTF && ev.Player.IsCuffed)
            {
                ev.IsAllowed = true;
                ev.NewRole = RoleTypeId.ChaosConscript;
            }
            if(ev.Player.IsCHI && ev.Player.IsCuffed)
            {
                ev.IsAllowed = true;
                ev.NewRole = RoleTypeId.NtfPrivate;
            }
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            if (ev.Player.Role == RoleTypeId.Tutorial && ev.Player.RemoteAdminAccess)
            {
                ev.Player.IsGodModeEnabled = true;
            } else if (ev.Player.Role != RoleTypeId.Tutorial && ev.Player.RemoteAdminAccess && ev.Player.IsGodModeEnabled)
            {
                ev.Player.IsGodModeEnabled = false;
            }
        }
        
        private void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Reason == SpawnReason.Escaped)
            {
                // PriorityInventoryModule.Main(ev.Player, ev.Items);
            }
        }
        
        private void OnScp914UpgradeInv(UpgradingInventoryItemEventArgs ev)
        {
            if (ev.KnobSetting == Scp914KnobSetting.Fine && ev.Item.Type == ItemType.Coin)
            {
                var randomNum = Random.Range(1, 3);
                switch (randomNum)
                {
                    case 1:
                    {
                        ev.Item.Destroy();
                        ev.Player.AddItem(ItemType.Flashlight);
                        break;
                    }
                    case 2:
                    {
                        ev.Item.Destroy();
                        ev.Player.AddItem(ItemType.Radio);
                    }
                        break;
                    case 3:
                    {
                        ev.Item.Destroy();
                        ev.Player.AddItem(ItemType.KeycardJanitor);
                        break;
                    }
                }
            }
        }

        public IEnumerator<float> StartNukeDamage(Features.Player player)
        {
            yield return Timing.WaitForSeconds(180);
            while(player.CurrentRoom.Type == RoomType.HczNuke)
            {
                player.Hurt(1f);
                yield return Timing.WaitForSeconds(1);
            }

            yield return 0;
        }
    }
}