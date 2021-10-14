// -----------------------------------------------------------------------
// <copyright file="PocketHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using Mirror;
using Mistaken.API;
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using UnityEngine;

namespace Mistaken.BetterSCP.SCP106
{
    internal class PocketHandler : Module
    {
        public PocketHandler(PluginHandler p)
            : base(p)
        {
            Log = base.Log;
        }

        public override string Name => nameof(PocketHandler);

        public override void OnEnable()
        {
            Exiled.Events.Handlers.Player.Dying += this.Handle<Exiled.Events.EventArgs.DyingEventArgs>((ev) => this.Player_Dying(ev));
            Exiled.Events.Handlers.Player.Hurting += this.Handle<Exiled.Events.EventArgs.HurtingEventArgs>((ev) => this.Player_Hurting(ev));
            Exiled.Events.Handlers.Player.DroppingItem += this.Handle<Exiled.Events.EventArgs.DroppingItemEventArgs>((ev) => this.Player_DroppingItem(ev));
            Exiled.Events.Handlers.Server.WaitingForPlayers += this.Handle(() => this.Server_WaitingForPlayers(), "WaitingForPlayers");
            Exiled.Events.Handlers.Player.EscapingPocketDimension += this.Handle<Exiled.Events.EventArgs.EscapingPocketDimensionEventArgs>((ev) => this.Player_EscapingPocketDimension(ev));
            Exiled.Events.Handlers.Server.RoundStarted += this.Handle(() => this.Server_RoundStarted(), "RoundStart");
            Exiled.Events.Handlers.Player.FailingEscapePocketDimension += this.Handle<Exiled.Events.EventArgs.FailingEscapePocketDimensionEventArgs>((ev) => this.Player_FailingEscapePocketDimension(ev));
            Exiled.Events.Handlers.Player.Shooting += this.Handle<Exiled.Events.EventArgs.ShootingEventArgs>((ev) => this.Player_Shooting(ev));
        }

        public override void OnDisable()
        {
            Exiled.Events.Handlers.Player.Dying -= this.Handle<Exiled.Events.EventArgs.DyingEventArgs>((ev) => this.Player_Dying(ev));
            Exiled.Events.Handlers.Player.Hurting -= this.Handle<Exiled.Events.EventArgs.HurtingEventArgs>((ev) => this.Player_Hurting(ev));
            Exiled.Events.Handlers.Player.DroppingItem -= this.Handle<Exiled.Events.EventArgs.DroppingItemEventArgs>((ev) => this.Player_DroppingItem(ev));
            Exiled.Events.Handlers.Server.WaitingForPlayers -= this.Handle(() => this.Server_WaitingForPlayers(), "WaitingForPlayers");
            Exiled.Events.Handlers.Player.EscapingPocketDimension -= this.Handle<Exiled.Events.EventArgs.EscapingPocketDimensionEventArgs>((ev) => this.Player_EscapingPocketDimension(ev));
            Exiled.Events.Handlers.Server.RoundStarted -= this.Handle(() => this.Server_RoundStarted(), "RoundStart");
            Exiled.Events.Handlers.Player.FailingEscapePocketDimension -= this.Handle<Exiled.Events.EventArgs.FailingEscapePocketDimensionEventArgs>((ev) => this.Player_FailingEscapePocketDimension(ev));
            Exiled.Events.Handlers.Player.Shooting -= this.Handle<Exiled.Events.EventArgs.ShootingEventArgs>((ev) => this.Player_Shooting(ev));
        }

        internal static void OnKilledINPocket(Player player)
        {
            ThrowItems(player);
            try
            {
                Exiled.API.Features.Ragdoll.Spawn(
                    player,
                    DamageTypes.Pocket,
                    Map.Rooms[UnityEngine.Random.Range(0, Map.Rooms.Count)].Position + new Vector3(0, 3, 0),
                    velocity: Vector3.down * 5,
                    allowRecall: false);
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }

            player.Kill(DamageTypes.RagdollLess);
        }

        private static readonly RoomType[] DisallowedRoomTypes = new RoomType[]
        {
            RoomType.EzShelter,
            RoomType.EzCollapsedTunnel,
            RoomType.HczTesla,
            RoomType.Lcz173,
            RoomType.Hcz939,
            RoomType.Pocket,
        };

        private static RagdollManager ragdollManager { get; set; }

        private static new ModuleLogger Log { get; set; }

        private static List<int> InPocket { get; } = new List<int>();

        private static void ThrowItems(Player player)
        {
            var items = player.Items;
            player.Ammo[ItemType.Ammo12gauge] = 0;
            player.Ammo[ItemType.Ammo44cal] = 0;
            player.Ammo[ItemType.Ammo556x45] = 0;
            player.Ammo[ItemType.Ammo762x39] = 0;
            player.Ammo[ItemType.Ammo9x19] = 0;
            foreach (var item in items.ToArray())
            {
                try
                {
                    item.Spawn(Map.Rooms[UnityEngine.Random.Range(0, Map.Rooms.Count)].Position + new Vector3(0, 2, 0));
                }
                catch (System.Exception e)
                {
                    Log.Error(e.Message);
                    Log.Error(e.StackTrace);
                }
            }

            player.ClearInventory();
        }

        private Room[] rooms;

        private Room RandomRoom
        {
            get
            {
                if (this.rooms == null)
                    this.SetRooms();
                return this.rooms[UnityEngine.Random.Range(0, this.rooms.Length)] ?? this.RandomRoom;
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

        private void SetRooms()
        {
            this.rooms = Map.Rooms.Where(r => !DisallowedRoomTypes.Contains(r.Type) && r != null).ToArray();
        }

        private void Server_RoundStarted()
        {
            Log.Info("Setting Rooms");
            this.SetRooms();
            this.RunCoroutine(this.VisiblityHandler(), "VisiblityHandler");
        }

        private IEnumerator<float> VisiblityHandler()
        {
            yield return Timing.WaitForSeconds(1);
            while (Round.IsStarted)
            {
                yield return Timing.WaitForSeconds(1);
                foreach (var player in RealPlayers.List)
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

            if (this.rooms == null)
                return;
            int trie = 0;
            bool forceNext = false;
            Room targetRoom = this.RandomRoom;
            var position = targetRoom.Position + (Vector3.up * 2);
            while (!this.IsRoomOK(targetRoom) || forceNext)
            {
                forceNext = false;
                targetRoom = this.RandomRoom;
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
            ev.Player.Position = ev.TeleportPosition;
            var pec = ev.Player.ReferenceHub.playerEffectsController;
            pec.EnableEffect<CustomPlayerEffects.Flashed>(2);
            pec.EnableEffect<CustomPlayerEffects.Blinded>(5);
            pec.EnableEffect<CustomPlayerEffects.Deafened>(10);
            pec.EnableEffect<CustomPlayerEffects.Concussed>(10);
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
            if (ev.Target.Position.y < -1900 && ev.DamageType != DamageTypes.RagdollLess)
            {
                if (!ev.Target.IsReadyPlayer())
                {
                    ev.IsAllowed = false;
                    return;
                }

                if (ev.Target.Health <= ev.Amount)
                {
                    OnKilledINPocket(ev.Target);
                    ev.IsAllowed = false;
                }
            }
        }

        private void Server_WaitingForPlayers()
        {
            InPocket.Clear();
            ragdollManager = GameObject.FindObjectOfType<RagdollManager>();
        }

        private void Player_Dying(Exiled.Events.EventArgs.DyingEventArgs ev)
        {
            if (InPocket.Contains(ev.Target.Id))
                InPocket.Remove(ev.Target.Id);

            if (ev.Target.Position.y < -1900)
                ThrowItems(ev.Target);
        }
    }
}
