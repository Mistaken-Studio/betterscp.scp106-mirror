// -----------------------------------------------------------------------
// <copyright file="SCP106Handler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using Mistaken.API;
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using UnityEngine;

namespace Mistaken.BetterSCP.SCP106
{
    /// <inheritdoc/>
    internal class SCP106Handler : Module
    {
        public const float CooldownOnStart = 35f;

        public SCP106Handler(PluginHandler p)
            : base(p)
        {
        }

        public override string Name => nameof(SCP106Handler);

        public override void OnEnable()
        {
            Exiled.Events.Handlers.Scp106.Teleporting += this.Scp106_Teleporting;
            Exiled.Events.Handlers.Scp106.CreatingPortal += this.Scp106_CreatingPortal;
            Exiled.Events.Handlers.Scp106.Containing += this.Scp106_Containing;
            Exiled.Events.Handlers.Server.RoundStarted += this.Server_RoundStarted;
            Exiled.Events.Handlers.Map.Decontaminating += this.Map_Decontaminating;
            Exiled.Events.Handlers.Player.FailingEscapePocketDimension += this.Player_FailingEscapePocketDimension;
            Exiled.Events.Handlers.Player.EscapingPocketDimension += this.Player_EscapingPocketDimension;
            Exiled.Events.Handlers.Player.EnteringFemurBreaker += this.Player_EnteringFemurBreaker;
            Exiled.Events.Handlers.Server.WaitingForPlayers += this.Server_WaitingForPlayers;
            Exiled.Events.Handlers.Player.ChangingRole += this.Player_ChangingRole;
            Exiled.Events.Handlers.Player.InteractingDoor += this.Player_InteractingDoor;

            SCPGUIHandler.SCPMessages.Add(RoleType.Scp106, PluginHandler.Instance.Translation.StartMessage);
        }

        public override void OnDisable()
        {
            Exiled.Events.Handlers.Scp106.Teleporting -= this.Scp106_Teleporting;
            Exiled.Events.Handlers.Scp106.CreatingPortal -= this.Scp106_CreatingPortal;
            Exiled.Events.Handlers.Scp106.Containing -= this.Scp106_Containing;
            Exiled.Events.Handlers.Server.RoundStarted -= this.Server_RoundStarted;
            Exiled.Events.Handlers.Map.Decontaminating -= this.Map_Decontaminating;
            Exiled.Events.Handlers.Player.FailingEscapePocketDimension -= this.Player_FailingEscapePocketDimension;
            Exiled.Events.Handlers.Player.EscapingPocketDimension -= this.Player_EscapingPocketDimension;
            Exiled.Events.Handlers.Player.EnteringFemurBreaker -= this.Player_EnteringFemurBreaker;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= this.Server_WaitingForPlayers;
            Exiled.Events.Handlers.Player.ChangingRole -= this.Player_ChangingRole;
            Exiled.Events.Handlers.Player.InteractingDoor -= this.Player_InteractingDoor;

            SCPGUIHandler.SCPMessages.Remove(RoleType.Scp106);
        }

        private static readonly Dictionary<RoomType, string> RoomTranslations = new Dictionary<RoomType, string>()
        {
            { RoomType.HczTesla, "Bramka Tesli" },
            { RoomType.HczTCross, "Skrzyżowanie typu T" },
            { RoomType.HczStraight, "Prosty korytarz" },
            { RoomType.HczServers, "Serwerownia" },
            { RoomType.HczNuke, "Głowicą nuklearna" },
            { RoomType.HczHid, "Zbrojownia z MicroHID" },
            { RoomType.HczEzCheckpoint, "Przejście HCZ-EZ" },
            { RoomType.HczCurve, "Zakręt" },
            { RoomType.HczCrossing, "Skrzyżowanie typu X" },
            { RoomType.HczChkpB, "Winda B do LCZ" },
            { RoomType.HczChkpA, "Winda A do LCZ" },
            { RoomType.HczArmory, "Zbrojownia" },
            { RoomType.Hcz939, "Przechowalnia SCP-939" },
            { RoomType.Hcz106, "Przechowalnia SCP-106" },
            { RoomType.Hcz096, "Przechowalnia SCP-096" },
            { RoomType.Hcz079, "Przechowalnia SCP-079" },
            { RoomType.Hcz049, "Przechowalnia SCP-049" },
        };

        private static readonly RoomType[] DisallowedRoomTypes = new RoomType[]
        {
            RoomType.EzShelter,
            RoomType.EzCollapsedTunnel,
            RoomType.EzGateA,
            RoomType.EzGateB,
            RoomType.LczChkpA,
            RoomType.LczChkpB,
            RoomType.HczEzCheckpoint,
            RoomType.HczTesla,
            RoomType.Lcz173,
            RoomType.Hcz939,
            RoomType.Pocket,
            RoomType.Hcz096,
        };

        private readonly HashSet<int> inTeleportExecution = new HashSet<int>();
        private readonly HashSet<int> cooldown = new HashSet<int>();
        private readonly Dictionary<int, List<Vector3>> lastRooms = new Dictionary<int, List<Vector3>>();

        private Room[] rooms;

        private Room RandomRoom
        {
            get
            {
                if (this.rooms == null || this.rooms.Length == 0)
                    this.rooms = Room.List.Where(r => r != null && !DisallowedRoomTypes.Contains(r.Type)).ToArray();
                return this.rooms[UnityEngine.Random.Range(0, this.rooms.Length)] ?? this.RandomRoom;
            }
        }

        private void Player_InteractingDoor(Exiled.Events.EventArgs.InteractingDoorEventArgs ev)
        {
            if (ev.Player.Role.Type == RoleType.Scp079)
                return;

            if (ev.Player.IsBypassModeEnabled)
                return;

            var type = ev.Door.Type;
            if (type == DoorType.Scp106Primary || type == DoorType.Scp106Secondary || type == DoorType.Scp106Bottom)
            {
                if (Generator.List.Where(x => x.IsEngaged).Count() < 2)
                {
                    ev.Player.SetGUI("scp106doordenymessage", PseudoGUIPosition.MIDDLE, PluginHandler.Instance.Translation.DoorDenyMessage, 5);
                    ev.IsAllowed = false;
                }
            }
        }

        private void Player_ChangingRole(Exiled.Events.EventArgs.ChangingRoleEventArgs ev)
        {
            if (ev.NewRole == RoleType.Scp106)
            {
                this.RunCoroutine(this.UpdateGenerators(ev.Player), "UpdateGenerators");
                this.CallDelayed(1f, () => ev.Player.ReferenceHub.scp106PlayerScript.NetworkportalPosition = ev.Player.Position);
                if (Round.ElapsedTime.TotalSeconds < CooldownOnStart)
                    this.RunCoroutine(this.StartCooldown(ev.Player), "LockStart");
            }
        }

        private void Server_WaitingForPlayers()
        {
            this.CallDelayed(
                5,
                () =>
                {
                    if (this.rooms == null || this.rooms.Length == 0)
                        this.Server_WaitingForPlayers();
                },
                "Server_WaitingForPlayers");
            this.rooms = Room.List.Where(r => r != null && !DisallowedRoomTypes.Contains(r.Type)).ToArray();
        }

        private void Player_EnteringFemurBreaker(Exiled.Events.EventArgs.EnteringFemurBreakerEventArgs ev)
        {
            this.CallDelayed(
                10,
                () =>
                {
                    if (!OneOhSixContainer.used)
                        Cassie.Message("SCP 1 0 6 is READY FOR RECONTAINMENT");
                },
                "Player_EnteringFemurBreaker");
        }

        private void Player_FailingEscapePocketDimension(Exiled.Events.EventArgs.FailingEscapePocketDimensionEventArgs ev)
        {
            if (ev.Player.IsScp)
                ev.IsAllowed = false;
        }

        private void Player_EscapingPocketDimension(Exiled.Events.EventArgs.EscapingPocketDimensionEventArgs ev)
        {
            if (ev.Player.IsScp)
                ev.IsAllowed = false;
        }

        private void Scp106_Containing(Exiled.Events.EventArgs.ContainingEventArgs ev)
        {
            foreach (Door door in Door.List)
            {
                if (door.Type == DoorType.Scp106Bottom || door.Type == DoorType.Scp106Primary || door.Type == DoorType.Scp106Secondary)
                {
                    this.CallDelayed(20f, () =>
                    {
                        door.Base.ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason.Warhead, true);
                        door.IsOpen = true;
                    });
                }
            }

            Vector3 newTarget = Door.List.FirstOrDefault(d => d.Type == DoorType.Scp106Primary)?.Base.transform.position ?? default;
            if (newTarget == default)
            {
                RealPlayers.Get(RoleType.Scp106).ToList().ForEach(p => p.SendConsoleMessage("[106] Not teleporting to cell, cell not found | Code: 5.1", "red"));
                return;
            }

            foreach (var player in RealPlayers.Get(RoleType.Scp106).ToArray())
            {
                player.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Scp207>();
                player.ReferenceHub.playerEffectsController.ChangeEffectIntensity<CustomPlayerEffects.Scp207>(4);
                if (Vector3.Distance(player.Position, newTarget) < 20)
                {
                    player.SendConsoleMessage("[106] Not teleporting to cell, too close | Code: 5.2", "red");
                    continue;
                }

                this.TeleportOldMan(player, newTarget, true);
            }
        }

        private void Map_Decontaminating(Exiled.Events.EventArgs.DecontaminatingEventArgs ev)
        {
            foreach (var player in RealPlayers.Get(RoleType.Scp106).ToArray())
            {
                if (player.CurrentRoom?.Zone != ZoneType.LightContainment)
                    continue;
                this.TeleportOldMan(player, this.GetRandomRoom(player, false), true);
            }
        }

        private void Server_RoundStarted()
        {
            this.cooldown.Clear();
            this.Log.Info("Setting Rooms");
            this.rooms = Room.List.Where(r => r != null && !DisallowedRoomTypes.Contains(r.Type)).ToArray();
            this.lastRooms.Clear();
            this.inTeleportExecution.Clear();
        }

        private IEnumerator<float> StartCooldown(Player scp)
        {
            yield return Timing.WaitForSeconds(0.1f);
            while (Round.ElapsedTime.TotalSeconds < CooldownOnStart)
            {
                if (!scp.IsConnected)
                    break;
                scp.SetGUI("scp106", PseudoGUIPosition.TOP, $"Cooldown: <color=yellow>{CooldownOnStart - Mathf.RoundToInt((float)Round.ElapsedTime.TotalSeconds)}</color>s");
                yield return Timing.WaitForSeconds(1);
            }

            scp.SetGUI("scp106", PseudoGUIPosition.TOP, null);
        }

        private void TeleportOldMan(Player player, Vector3 target, bool force = false)
        {
            if (!force)
            {
                if (player.Role != RoleType.Scp106)
                {
                    player.SendConsoleMessage("[106] Not 106 | Code: 1.1", "red");
                    return;
                }

                if (player.GetEffectActive<CustomPlayerEffects.Ensnared>())
                {
                    player.SendConsoleMessage("[106] Ensnared active | Code: 1.2", "red");
                    return;
                }

                if (this.inTeleportExecution.Contains(player.Id))
                {
                    player.SendConsoleMessage("[106] Already teleporting | Code: 1.3", "red");
                    return;
                }

                if (Warhead.IsDetonated)
                {
                    player.SendConsoleMessage("[106] Detonated | Code: 1.4", "red");
                    return;
                }

                if (this.cooldown.Contains(player.Id))
                {
                    player.SendConsoleMessage("[106] Cooldown | Code: 1.5", "red");
                    return;
                }
            }

            this.inTeleportExecution.Add(player.Id);
            Vector3 oldPos = player.Position;
            this.CallDelayed(
                2,
                () =>
                {
                    this.inTeleportExecution.Remove(player.Id);
                    if (Warhead.IsDetonated)
                        player.Position = oldPos;
                },
                "TeleportOldMan");
            Scp106PlayerScript s106 = player.ReferenceHub.scp106PlayerScript;
            s106.NetworkportalPosition = target;
            s106.UserCode_CmdUsePortal();
        }

        private void Scp106_CreatingPortal(Exiled.Events.EventArgs.CreatingPortalEventArgs ev)
        {
            ev.IsAllowed = false;
            if (Round.ElapsedTime.TotalSeconds < CooldownOnStart)
            {
                ev.Player.SendConsoleMessage("[106] Too early | Code: 2.1", "red");
                return;
            }

            if (this.cooldown.Contains(ev.Player.Id))
            {
                ev.Player.SendConsoleMessage("[106] Cooldown | Code: 2.2", "red");
                return;
            }

            this.TeleportOldMan(ev.Player, this.GetRandomRoom(ev.Player, false));
        }

        private void Scp106_Teleporting(Exiled.Events.EventArgs.TeleportingEventArgs ev)
        {
            if (ev.Player.GetEffectActive<CustomPlayerEffects.Ensnared>())
            {
                ev.IsAllowed = false;
                ev.Player.SendConsoleMessage("[106] Ensnared active | Code: 3.2", "red");
                return;
            }

            if (this.inTeleportExecution.Contains(ev.Player.Id))
            {
                this.RunCoroutine(this.DoPostTeleport(ev.Player), "DoPostTeleport");
                return;
            }

            if (Round.ElapsedTime.TotalSeconds < CooldownOnStart)
            {
                ev.Player.SendConsoleMessage("[106] Too early | Code: 3.1", "red");
                ev.IsAllowed = false;
                return;
            }

            if (Warhead.IsDetonated)
            {
                ev.Player.SendConsoleMessage("[106] Detonated | Code: 3.4", "red");
                ev.IsAllowed = false;
                return;
            }

            if (this.cooldown.Contains(ev.Player.Id))
            {
                ev.Player.SendConsoleMessage("[106] Cooldown | Code: 3.5", "red");
                ev.IsAllowed = false;
                return;
            }

            Vector3 newTarget = this.GetRandomRoom(ev.Player, true);
            ev.Player.ReferenceHub.scp106PlayerScript.NetworkportalPosition = newTarget;
            ev.PortalPosition = newTarget;
            Vector3 oldPos = ev.Player.Position;
            this.RunCoroutine(this.DoPostTeleport(ev.Player), "DoPostTeleport");
            this.CallDelayed(
                8f,
                () =>
                {
                    if (Warhead.IsDetonated)
                        ev.Player.Position = oldPos;
                },
                "SCP106_Teleporting");
        }

        private IEnumerator<float> DoPostTeleport(Player player)
        {
            player.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Ensnared>(7.7f);
            player.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Blinded>(4);
            player.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Deafened>(4);

            this.cooldown.Add(player.Id);
            for (int i = 0; i < 15; i++)
            {
                if (!player.IsConnected)
                    break;
                player.SetGUI("scp106", PseudoGUIPosition.TOP, $"Cooldown: <color=yellow>{15 - i}</color>s");
                yield return Timing.WaitForSeconds(1);
            }

            this.cooldown.Remove(player.Id);
            player.SetGUI("scp106", PseudoGUIPosition.TOP, null);
        }

        private Vector3 GetRandomRoom(Player player, bool sameZone)
        {
            ZoneType zone = player.CurrentRoom?.Zone ?? ZoneType.Unspecified;
            if (zone == ZoneType.LightContainment && Map.IsLczDecontaminated)
                sameZone = false;
            Room targetRoom = this.RandomRoom;
            int trie = 0;
            bool first = true;
            if (!this.lastRooms.ContainsKey(player.Id))
                this.lastRooms.Add(player.Id, new List<Vector3>());

            bool specialAbility = UnityEngine.Random.Range(1, 6) == 1 &&
                        Round.ElapsedTime.TotalMinutes > 20 &&
                        RealPlayers.List.Where(p => p.IsHuman).Count() < 5;
            if (specialAbility)
                player.SendConsoleMessage("[106] Finder: Activated", "blue");

            while (!this.IsRoomOK(targetRoom, sameZone, zone, ref first, specialAbility) || (this.lastRooms[player.Id].Contains(targetRoom.Position) && targetRoom.Position.y < 800))
            {
                targetRoom = this.RandomRoom;
                trie++;
                if (trie >= 1000)
                {
                    this.Log.Warn("Failed to generate teleport position in 1000 tries");
                    player.SendConsoleMessage("[106] Failed to generate | Code: 4.1", "red");
                    return new Vector3(0, 1000, 0);
                }
            }

            this.Log.Debug($"New position is {targetRoom.Position} | {sameZone} | {zone} | {targetRoom.Zone}", PluginHandler.Instance.Config.VerbouseOutput);
            this.lastRooms[player.Id].Add(targetRoom.Position);
            if (this.lastRooms[player.Id].Count > 3)
                this.lastRooms[player.Id].RemoveAt(0);

            Vector3 targetPos;

            if (targetRoom.Position.y > 800)
                targetPos = Random.Range(0, 2) == 1 ? targetRoom.Position : new Vector3(86, 992, -49);
            else
                targetPos = targetRoom.Position + (Vector3.down * 0.2f);

            if (Physics.Raycast(new Ray(targetPos + Vector3.up, -Vector3.up), out RaycastHit raycastHit, 10f, Server.Host.ReferenceHub.scp106PlayerScript.teleportPlacementMask))
                targetPos = raycastHit.point - Vector3.up;
            else
                this.Log.Error($"Safe portal position not found | {targetPos}");

            this.Log.Debug($"New raw position is {targetPos}", PluginHandler.Instance.Config.VerbouseOutput);

            return targetPos;
        }

        private bool IsRoomOK(Room room, bool sameZone, ZoneType targetZone, ref bool first, bool goToHuman)
        {
            if (room?.gameObject == null)
                return false;
            if (first && goToHuman && room.Players.Count() == 0)
                return false;
            first = false;
            if (DisallowedRoomTypes.Contains(room.Type))
                return false;
            if (sameZone && targetZone != room.Zone)
                return false;
            if (!sameZone && targetZone == room.Zone)
                return false;
            if (MapPlus.IsLCZDecontaminated(30) && room.Zone == ZoneType.LightContainment)
                return false;
            if (!UnityEngine.Physics.Raycast(room.Position + (Vector3.up / 2), Vector3.down, 5))
                return false;
            return true;
        }

        private IEnumerator<float> UpdateGenerators(Player player)
        {
            yield return Timing.WaitForSeconds(60);

            if (RealPlayers.Any(RoleType.Scp079))
                yield break;

            var generators = Generator.List.ToArray();
            string singleGeneratorMessage = "<color=red>Generator w trakcie aktywacji!</color><br>Znajduje się on w pomieszczeniu:";
            string multipleGeneratorsMessage = "<color=red>Generatory w trakcie aktywacji!</color><br>Znajdują się one w pomieszczeniach:";
            while (player.IsConnected && player.Role.Type == RoleType.Scp106)
            {
                string message = string.Empty;
                var runningGenerators = generators.Where(x => x.IsActivating).ToArray();

                if (runningGenerators.Length == 1)
                    message = singleGeneratorMessage;
                else if (runningGenerators.Length > 1)
                    message = multipleGeneratorsMessage;

                foreach (var generator in runningGenerators)
                {
                    string room = "Nie znaleziono";
                    if (RoomTranslations.ContainsKey(generator.Room.Type))
                        room = RoomTranslations[generator.Room.Type];
                    else
                        room += $" ({generator.Room.Type})";
                    message += $"<br><color=yellow>{room}</color>";
                }

                player.SetGUI($"scp106generator", PseudoGUIPosition.BOTTOM, message);
                yield return Timing.WaitForSeconds(1);
            }

            player.SetGUI($"scp106generator", PseudoGUIPosition.BOTTOM, null);
        }
    }
}
