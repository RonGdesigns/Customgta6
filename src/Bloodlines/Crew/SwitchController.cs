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
    ///  * The aerial switch camera is allowed to run before control changes hands,
    ///    so the transition reads as GTA V's own switch instead of a hard cut.
    /// </summary>
    public sealed class SwitchController
    {
        private const int SwitchCooldownMs = 1500;

        private readonly CrewRoster _crew;
        private int _lastSwitchTime;

        public SwitchController(CrewRoster crew)
        {
            _crew = crew;
        }

        /// <summary>Set by missions that script their own switch beats or forbid switching outright.</summary>
        public bool Locked { get; set; }

        public string LockReason { get; set; }

        public bool IsSwitching { get; private set; }
        public System.Action BeforeSwitch { get; set; }

        public void Cancel()
        {
            if (Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS)) Function.Call(Hash.STOP_PLAYER_SWITCH);
            IsSwitching = false;
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

        public bool TrySwitch(CrewSlot target)
        {
            if (!_crew.IsDeployed) return false;

            if (_crew.IsSolo)
            {
                GameUtils.Subtitle("~r~" + _crew.Active.FirstName + " is working this one alone.", 2500);
                return false;
            }

            if (IsSwitching) return false;

            if (Locked)
            {
                GameUtils.Subtitle("~r~" + (LockReason ?? "You can't switch right now."), 2500);
                return false;
            }

            if (target == _crew.ActiveSlot) return false;

            if (Game.GameTime - _lastSwitchTime < SwitchCooldownMs) return false;

            var targetPed = _crew.PedFor(target);
            var currentPed = Game.Player.Character;

            if (targetPed == null || targetPed.IsDead)
            {
                GameUtils.Subtitle("~r~" + Protagonist.Of(target).DisplayName + " is down.", 2500);
                return false;
            }

            if (currentPed == null || !currentPed.Exists() || currentPed.IsDead)
            {
                GameUtils.Subtitle("~r~Can't switch while you're going down.", 2500);
                return false;
            }

            return Execute(currentPed, targetPed, target);
        }

        private bool Execute(Ped currentPed, Ped targetPed, CrewSlot target)
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

                // 0 = default flags, 1 = short/medium range switch descriptor.
                Function.Call(Hash.START_PLAYER_SWITCH, currentPed, targetPed, 0, 1);

                // Let the swoop camera get airborne before control changes hands.
                int guard = 0;
                while (Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS) && guard < 40)
                {
                    if (Function.Call<int>(Hash.GET_PLAYER_SWITCH_STATE) >= 5) break;
                    Script.Wait(50);
                    guard++;
                }

                if (!currentPed.Exists() || currentPed.IsDead || !targetPed.Exists() || targetPed.IsDead) return false;
                Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, targetPed, true, true);
                if (Game.Player.Character.Handle != targetPed.Handle) return false;

                _crew.SetActive(target);

                GameUtils.Subtitle("~b~" + protagonist.DisplayName + "~s~ — " + protagonist.Role, 3000);
                GameUtils.PlayFrontendSound("Object_Dropped_Remote", "GTAO_Magnate_Gra_Soundset");
                for (int i = 0; i < 80 && Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS); i++)
                {
                    if (!targetPed.Exists() || targetPed.IsDead) return false;
                    Script.Wait(50);
                }
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
