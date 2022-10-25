// -----------------------------------------------------------------------
// <copyright file="PocketHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.Pickups;
using MEC;
using Mirror;
using Mistaken.API;
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using PlayerStatsSystem;
using UnityEngine;

namespace Mistaken.BetterSCP.SCP106
{
    internal sealed class PocketHandler : Module
    {
        public PocketHandler(PluginHandler p)
            : base(p)
        {
            Log = base.Log;
        }

        public override string Name => nameof(PocketHandler);

        public override void OnEnable()
        {
            Exiled.Events.Handlers.Player.Dying += this.Player_Dying;
            Exiled.Events.Handlers.Player.Hurting += this.Player_Hurting;
            Exiled.Events.Handlers.Player.DroppingItem += this.Player_DroppingItem;
            Exiled.Events.Handlers.Player.EscapingPocketDimension += this.Player_EscapingPocketDimension;
            Exiled.Events.Handlers.Player.FailingEscapePocketDimension += this.Player_FailingEscapePocketDimension;
            Exiled.Events.Handlers.Player.Shooting += this.Player_Shooting;
            Exiled.Events.Handlers.Player.SpawningRagdoll += this.Player_SpawningRagdoll;
            Exiled.Events.Handlers.Server.RoundStarted += this.Server_RoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers += this.Server_WaitingForPlayers;
        }

        public override void OnDisable()
        {
            Exiled.Events.Handlers.Player.Dying -= this.Player_Dying;
            Exiled.Events.Handlers.Player.Hurting -= this.Player_Hurting;
            Exiled.Events.Handlers.Player.DroppingItem -= this.Player_DroppingItem;
            Exiled.Events.Handlers.Player.EscapingPocketDimension -= this.Player_EscapingPocketDimension;
            Exiled.Events.Handlers.Player.FailingEscapePocketDimension -= this.Player_FailingEscapePocketDimension;
            Exiled.Events.Handlers.Player.Shooting -= this.Player_Shooting;
            Exiled.Events.Handlers.Player.SpawningRagdoll -= this.Player_SpawningRagdoll;
            Exiled.Events.Handlers.Server.RoundStarted -= this.Server_RoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= this.Server_WaitingForPlayers;
        }

        internal static void OnKilledINPocket(Player player)
        {
            ThrowItems(player);
            var rooms = Room.List.ToList();

            try
            {
                GameObject model_ragdoll = player.ReferenceHub.characterClassManager.CurRole.model_ragdoll;
                if (!(model_ragdoll == null) && UnityEngine.Object.Instantiate(model_ragdoll).TryGetComponent<Ragdoll>(out var component))
                {
                    component.NetworkInfo = new RagdollInfo(
                        player.ReferenceHub,
                        new UniversalDamageHandler(-1f, DeathTranslations.PocketDecay),
                        player.ReferenceHub.characterClassManager.CurClass,
                        rooms[UnityEngine.Random.Range(0, rooms.Count)].Position + new Vector3(0, 3, 0) + model_ragdoll.transform.localPosition,
                        player.ReferenceHub.transform.rotation * model_ragdoll.transform.localRotation,
                        player.ReferenceHub.nicknameSync.DisplayName,
                        NetworkTime.time);

                    NetworkServer.Spawn(component.gameObject);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }

            player.Kill(PluginHandler.Instance.Translation.UnluckyMessage);
        }

        private static readonly RoomType[] DisallowedRoomTypes = new[]
        {
            RoomType.EzShelter,
            RoomType.EzCollapsedTunnel,
            RoomType.HczTesla,
            RoomType.Lcz173,
            RoomType.Hcz939,
            RoomType.Pocket,
        };

        private static Room[] _rooms;

        private static new ModuleLogger Log { get; set; }

        private static List<int> InPocket { get; } = new();

        private static Room RandomRoom => _rooms[UnityEngine.Random.Range(0, _rooms.Length)];

        private static void ThrowItems(Player player)
        {
            var rooms = Room.List.ToList();
            DropAllAmmo(player);

            foreach (var item in player.Items.ToArray())
            {
                if (item.Type == ItemType.MicroHID)
                    continue;

                try
                {
                    player.RemoveItem(item, false);

                    if (!item.IsKeycard)
                        item.Spawn(rooms[UnityEngine.Random.Range(0, rooms.Count)].Position + new Vector3(0, 2, 0));
                }
                catch (System.Exception e)
                {
                    Log.Error(e.Message);
                    Log.Error(e.StackTrace);
                }
            }
        }

        private static void DropAllAmmo(Player player)
        {
            var rooms = Room.List.ToList();

            foreach (var kvp in player.Ammo.ToArray())
            {
                if (InventoryItemLoader.AvailableItems.TryGetValue(kvp.Key, out var value2))
                {
                    if (value2.PickupDropModel == null)
                    {
                        Debug.LogError("No pickup drop model set. Could not drop the ammo.");
                        return;
                    }

                    int num2 = kvp.Value;
                    player.Ammo[kvp.Key] = 0;
                    player.Inventory.SendAmmoNextFrame = true;

                    while (num2 > 0)
                    {
                        PickupSyncInfo pickupSyncInfo = default;
                        pickupSyncInfo.ItemId = kvp.Key;
                        pickupSyncInfo.Serial = ItemSerialGenerator.GenerateNext();
                        pickupSyncInfo.Weight = value2.Weight;
                        pickupSyncInfo.Position = player.Inventory.transform.position;
                        AmmoPickup ammoPickup2;
                        if ((ammoPickup2 = player.Inventory.ServerCreatePickup(value2, pickupSyncInfo) as AmmoPickup) != null)
                        {
                            ammoPickup2.NetworkSavedAmmo = (ushort)Mathf.Min(ammoPickup2.MaxAmmo, num2);
                            num2 -= ammoPickup2.SavedAmmo;
                            ammoPickup2.transform.position = rooms[UnityEngine.Random.Range(0, rooms.Count)].Position + new Vector3(0, 2, 0);
                        }
                        else
                        {
                            num2--;
                        }
                    }
                }
            }
        }

        private void Player_Shooting(Exiled.Events.EventArgs.ShootingEventArgs ev)
        {
            if (ev.Shooter.IsInPocketDimension)
                ev.IsAllowed = false;
        }

        private void Player_FailingEscapePocketDimension(Exiled.Events.EventArgs.FailingEscapePocketDimensionEventArgs ev)
        {
            if (!ev.Player.IsReadyPlayer())
            {
                ev.IsAllowed = false;
                return;
            }
        }

        private void Server_RoundStarted()
        {
            this.RunCoroutine(this.VisiblityHandler(), "VisiblityHandler", true);
        }

        private IEnumerator<float> VisiblityHandler()
        {
            yield return Timing.WaitForSeconds(1);

            while (true)
            {
                yield return Timing.WaitForSeconds(1);

                foreach (var player in RealPlayers.List.ToArray())
                {
                    if (player.IsHuman && player.Position.y < -1900)
                        player.IsInvisible = true;
                    else
                        player.IsInvisible = false;
                }
            }
        }

        private void Player_EscapingPocketDimension(Exiled.Events.EventArgs.EscapingPocketDimensionEventArgs ev)
        {
            if (!ev.Player.IsReadyPlayer() || ev.Player.IsScp)
            {
                ev.IsAllowed = false;
                return;
            }

            /*int trie = 0;
            bool forceNext = false;
            Room targetRoom = RandomRoom;
            var position = targetRoom.Position + (Vector3.up * 2);
            while (!this.IsRoomOK(targetRoom) || forceNext)
            {
                forceNext = false;
                targetRoom = RandomRoom;
                position = targetRoom.Position + (Vector3.up * 2);
                trie++;

                if (trie >= 1000)
                {
                    position = ev.TeleportPosition;
                    targetRoom = null;
                    Log.Error("Failed to generate pocket exit position in 1000 tries");
                    break;
                }
            }

            Log.Debug($"Teleported {ev.Player?.Nickname} to {position} | {targetRoom?.Type} | {targetRoom?.Zone}", PluginHandler.Instance.Config.VerbouseOutput);
            ev.Player.SendConsoleMessage($"[BETTER POCKET] Teleported to {position} | {targetRoom?.Type} | {targetRoom?.Zone}", "yellow");
            ev.TeleportPosition = position;
            ev.IsAllowed = false;
            ev.Player.Position = ev.TeleportPosition;*/

            var pec = ev.Player.ReferenceHub.playerEffectsController;
            pec.EnableEffect<CustomPlayerEffects.Flashed>(1);
            pec.EnableEffect<CustomPlayerEffects.Blinded>(2.5f);
            pec.EnableEffect<CustomPlayerEffects.Deafened>(3);
            pec.EnableEffect<CustomPlayerEffects.Concussed>(5);
            InPocket.Remove(ev.Player.Id);
            {
                PocketDimensionTeleport[] array = PocketDimensionGenerator.PrepTeleports();
                int exits = GameCore.ConfigFile.ServerConfig.GetInt("pd_exit_count", 2);

                while (exits > 0 && PocketDimensionGenerator.ContainsKiller(array))
                {
                    int rand = UnityEngine.Random.Range(0, array.Length);

                    if (array[rand].GetTeleportType() == global::PocketDimensionTeleport.PDTeleportType.Exit)
                        continue;

                    array[rand].SetType(PocketDimensionTeleport.PDTeleportType.Exit);
                    exits--;
                }
            }

            PocketDimensionTeleport.RefreshExit = false;
        }

        private bool IsRoomOK(Room room)
        {
            if (DisallowedRoomTypes.Contains(room.Type))
                return false;

            if (MapPlus.IsLCZDecontaminated(60) && room.Zone == ZoneType.LightContainment)
                return false;

            if (!UnityEngine.Physics.Raycast(room.Position, Vector3.down, 5))
                return false;

            return true;
        }

        private void Player_DroppingItem(Exiled.Events.EventArgs.DroppingItemEventArgs ev)
        {
            if (ev.Player.Position.y < -1900)
                ev.IsAllowed = false;
        }

        private void Player_Hurting(Exiled.Events.EventArgs.HurtingEventArgs ev)
        {
            if (ev.Target.Position.y < -1900 && ev.Handler.Type != DamageType.Unknown)
            {
                if (!ev.Target.IsReadyPlayer())
                {
                    ev.IsAllowed = false;
                    return;
                }
            }
        }

        private void Server_WaitingForPlayers()
        {
            InPocket.Clear();
            _rooms = Room.List.Where(r => !DisallowedRoomTypes.Contains(r.Type) && r != null).ToArray();
        }

        private void Player_Dying(Exiled.Events.EventArgs.DyingEventArgs ev)
        {
            if (InPocket.Contains(ev.Target.Id))
                InPocket.Remove(ev.Target.Id);

            if (ev.Target.Position.y < -1900 && ev.Handler.Base is not CustomReasonDamageHandler)
            {
                OnKilledINPocket(ev.Target);
                ev.IsAllowed = false;
            }
        }

        private void Player_SpawningRagdoll(Exiled.Events.EventArgs.SpawningRagdollEventArgs ev)
        {
            if (ev.Position.y < -1900)
                ev.IsAllowed = false;
        }
    }
}
