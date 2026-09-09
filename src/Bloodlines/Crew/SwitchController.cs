using Bloodlines.Core;
using GTA;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>
    /// Section 6 of the design bible: the dynamic 3-way character switch.
    ///
    /// Two deliberate departures from the bible's draft listing:
    ///  * It uses CHANGE_PLAYER_PED rather than Player.ChangeModel. Changing the
    ///    model rebuilds the player ped from scratch — the companion ped you were
    ///    switching *to* is left standing there as an NPC, and the ped you left
    ///    behind evaporates along with its weapons, health and current task. The
    ///    native hands control of an existing ped to the player, which is what a
    ///    three-hander actually needs.
    ///  * Nearby handovers are immediate. Distant handovers use a short fade and
    ///    a bounded collision check; no native aerial switch is started.
    /// </summary>
    public sealed class SwitchController
    {
        private const int SwitchCooldownMs = 400;
        private const float NearbyDistance = 80f;
        private bool _ownsFade;
        private bool _ownsFocus;

        private readonly CrewRoster _crew;
        private int _lastSwitchTime = -SwitchCooldownMs;

        public SwitchController(CrewRoster crew)
        {
            _crew = crew;
        }

        /// <summary>Set by missions that script their own switch beats or forbid switching outright.</summary>
        public System.Func<string> ExternalBlockReason { get; set; }
        public bool Locked { get; set; }

        public string LockReason { get; set; }

        public bool IsSwitching { get; private set; }
        public System.Action BeforeSwitch { get; set; }
        public System.Action<CrewSlot, Ped> OnDistantHandover { get; set; }

        public void Cancel()
        {
            Release(() => { if (Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS)) Function.Call(Hash.STOP_PLAYER_SWITCH); });
            if (_ownsFocus) { _ownsFocus = false; Release(() => Function.Call(Hash.CLEAR_FOCUS)); }
            if (_ownsFade) { _ownsFade = false; Release(() => GameUtils.FadeIn(200)); }
            IsSwitching = false;
        }

        private static void Release(System.Action action)
        {
            try { action(); } catch (System.Exception ex) { Logger.Error("Switch cleanup failed", ex); }
        }

        public void SetLocked(string reason)
        {
            Locked = true;
            LockReason = reason;
        }

        public void SetUnlocked()
        {
            Locked = false;
            LockReason = null;
        }

        public bool TrySwitch(CrewSlot target, bool missionTransition = false)
        {
            if (!_crew.IsDeployed) return false;
            string block = ExternalBlockReason?.Invoke();
            if (!string.IsNullOrEmpty(block)) { GameUtils.Subtitle("~y~" + block, 2500); return false; }

            if (_crew.IsSolo)
            {
                GameUtils.Subtitle("~r~" + _crew.Active.Handle + " is working this one alone.", 2500);
                return false;
            }

            if (IsSwitching) return false;

            if (Locked)
            {
                GameUtils.Subtitle("~r~" + (LockReason ?? "You can't switch right now."), 2500);
                return false;
            }

            if (target == _crew.ActiveSlot) return false;

            if (!missionTransition && Game.GameTime - _lastSwitchTime < SwitchCooldownMs) return false;

            var targetPed = _crew.PedFor(target);
            var currentPed = Game.Player.Character;

            if (targetPed == null || !targetPed.Exists() || targetPed.IsDead)
            {
                GameUtils.Subtitle("~r~" + Protagonist.Of(target).DisplayName + " is down.", 2500);
                return false;
            }

            if (currentPed == null || !currentPed.Exists() || currentPed.IsDead)
            {
                GameUtils.Subtitle("~r~Can't switch while you're going down.", 2500);
                return false;
            }

            if (!missionTransition && (Game.Player.IsDead || !Game.Player.CanControlCharacter || currentPed.IsPositionFrozen))
            { GameUtils.Subtitle("~y~Character control is not ready for a switch.", 2500); return false; }
            if (currentPed.Handle == targetPed.Handle) return false;
            return Execute(currentPed, targetPed, target, missionTransition);
        }

        private bool Execute(Ped currentPed, Ped targetPed, CrewSlot target, bool missionTransition)
        {
            IsSwitching = true;
            _lastSwitchTime = Game.GameTime;
            var protagonist = Protagonist.Of(target);

            try
            {
                BeforeSwitch?.Invoke();
                Logger.Info("Switching to " + protagonist.DisplayName + ".");

                // Keep the ped we are leaving alive and useful rather than letting the
                // engine clean it up the moment it stops being the player.
                currentPed.IsPersistent = true;
                currentPed.BlockPermanentEvents = true;
                currentPed.RelationshipGroup = _crew.CrewGroup;

                targetPed.IsPersistent = true;

                var preservedRide = targetPed.CurrentVehicle;
                VehicleSeat preservedSeat = VehicleSeat.None;
                if (preservedRide != null && preservedRide.Exists())
                {
                    for (int i = -1; i < preservedRide.PassengerCapacity; i++)
                        if (preservedRide.GetPedOnSeat((VehicleSeat)i)?.Handle == targetPed.Handle) { preservedSeat = (VehicleSeat)i; break; }
                    if (preservedSeat == VehicleSeat.None) { GameUtils.Subtitle("Vehicle seat is still loading. Try again in a moment.", 3000); return false; }
                    preservedRide.IsPersistent = true;
                }
                bool nearby = currentPed.Position.DistanceTo(targetPed.Position) <= NearbyDistance;
                if (!nearby)
                {
                    // A short bounded fade replaces the sky camera. Never transfer
                    // control into unloaded collision; timeout leaves the old hero active.
                    bool alreadyBlack = GameUtils.IsScreenFadedOut();
                    if (alreadyBlack && !missionTransition) return false;
                    if (!alreadyBlack)
                    {
                        _ownsFade = true;
                        GameUtils.FadeOut(150);
                        Script.Wait(150);
                    }
                    _ownsFocus = true;
                    var point = targetPed.Position;
                    Function.Call(Hash.SET_FOCUS_POS_AND_VEL, point.X, point.Y, point.Z, 0f, 0f, 0f);
                    Function.Call(Hash.REQUEST_COLLISION_AT_COORD, point.X, point.Y, point.Z);
                    int attempts = 0;
                    while (targetPed.Exists() && !targetPed.IsDead &&
                           !Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, targetPed) && attempts++ < 30)
                        Script.Wait(50);
                    if (!targetPed.Exists() || targetPed.IsDead ||
                        !Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, targetPed))
                    {
                        GameUtils.Subtitle("~y~That area is still loading. Try the switch again.", 2500);
                        return false;
                    }
                }

                if (!currentPed.Exists() || currentPed.IsDead || !targetPed.Exists() || targetPed.IsDead) return false;
                int health = targetPed.Health, armor = targetPed.Armor;
                int departingHealth = currentPed.Health, departingArmor = currentPed.Armor;
                var departingSlot = _crew.ActiveSlot;
                int departingWanted = Game.Player.WantedLevel;
                bool personalHeat = !_crew.CompanionAI.MissionActive && !missionTransition;
                int incomingWanted = personalHeat ? _crew.CompanionAI.Life.Wanted.Get(target) : departingWanted;
                if (personalHeat && preservedRide != null && currentPed.IsInVehicle(preservedRide))
                    incomingWanted = System.Math.Max(incomingWanted, System.Math.Max(departingWanted, _crew.CompanionAI.Life.Wanted.Get(departingSlot)));
                var riding = preservedRide;
                var seat = preservedSeat;
                if (riding != null && (!riding.Exists() || !riding.IsDriveable || !targetPed.IsInVehicle(riding)))
                { GameUtils.Subtitle("That vehicle is unavailable. Staying with the current character.", 3000); return false; }
                Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, targetPed, true, true);
                // CHANGE_PLAYER_PED can reset the departing NPC's health as well as
                // the incoming player's. Preserve both bodies before another switch.
                CrewDurability.RestoreAfterSwitch(currentPed, departingHealth, departingArmor);
                CrewDurability.RestoreAfterSwitch(targetPed, health, armor);
                if (Game.Player.Character.Handle != targetPed.Handle) return false;

                if (personalHeat) _crew.CompanionAI.Life.Wanted.Capture(departingSlot, departingWanted);
                _crew.SetActive(target);
                _crew.CompanionAI.Life.PlayerTookControl(target);
                Game.Player.WantedLevel = System.Math.Min(5, incomingWanted);
                if (personalHeat) _crew.CompanionAI.Life.Wanted.Set(target, incomingWanted);
                if (incomingWanted > 0)
                {
                    Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                    Function.Call(Hash.SET_EVERYONE_IGNORE_PLAYER, Game.Player, false);
                    Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);
                }
                CrewDurability.RestoreAfterSwitch(targetPed, health, armor);
                // Clearing an NPC driving/boarding task must not eject the new player.
                if (riding != null && riding.Exists() && seat != VehicleSeat.None && seat != VehicleSeat.Any)
                {
                    var occupant = riding.GetPedOnSeat(seat);
                    if (occupant == null || !occupant.Exists() || occupant.Handle == targetPed.Handle)
                        targetPed.SetIntoVehicle(riding, seat);
                    if (!targetPed.IsInVehicle(riding) || riding.GetPedOnSeat(seat)?.Handle != targetPed.Handle)
                    {
                        // Roll back the player instead of leaving a failed aerial handover in free fall.
                        Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, currentPed, true, true);
                        CrewDurability.RestoreAfterSwitch(currentPed, departingHealth, departingArmor);
                        CrewDurability.RestoreAfterSwitch(targetPed, health, armor);
                        _crew.SetActive(departingSlot); Game.Player.WantedLevel = departingWanted;
                        GameUtils.Subtitle("Vehicle handover could not complete. Staying with your previous character.", 4000);
                        return false;
                    }
                }
                if (!nearby && !missionTransition) Release(() => OnDistantHandover?.Invoke(target, targetPed));

                GameUtils.Subtitle("~b~" + protagonist.DisplayName + "~s~ — " + protagonist.Role, 3000);
                GameUtils.PlayFrontendSound("Object_Dropped_Remote", "GTAO_Magnate_Gra_Soundset");
                return true;
            }
            catch (System.Exception ex)
            {
                Logger.Error("Character switch failed", ex);
                return false;
            }
            finally
            {
                try { Cancel(); }
                finally { IsSwitching = false; }
            }
        }
    }
}
