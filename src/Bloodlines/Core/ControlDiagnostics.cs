using System;
using Bloodlines.Crew;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// One log line describing everything that decides whether the player can
    /// move: who they are, what they are sitting in, which flags are set, who owns
    /// their tasks and how the last scene ended. Written at the transitions where
    /// control has gone missing in playtests (scene end, gameplay start, the
    /// prologue hand-off), so a report of "I couldn't drive" comes with the state
    /// that produced it instead of a guess.
    /// </summary>
    public static class ControlDiagnostics
    {
        public static string Snapshot(string reason, CrewRoster crew, CutsceneDirector scenes, MissionHandoff gate, CrewHomes homes, string mission)
        {
            try
            {
                var player = Game.Player.Character;
                string line = "CONTROL [" + reason + "] mission=" + (mission ?? "none");
                if (player == null || !player.Exists()) return Log(line + " player=none");
                var vehicle = player.CurrentVehicle;
                line += " ped=" + player.Handle + "/" + player.Model.Hash +
                        " active=" + (crew != null && crew.IsDeployed ? crew.ActiveSlot.ToString() : "story") +
                        " control=" + Game.Player.CanControlCharacter +
                        " frozen=" + player.IsPositionFrozen + " invincible=" + player.IsInvincible +
                        " dead=" + player.IsDead;
                if (vehicle != null && vehicle.Exists())
                    line += " vehicle=" + vehicle.DisplayName + "/" + vehicle.Handle + " seat=" + player.SeatIndex +
                            " driver=" + (vehicle.GetPedOnSeat(VehicleSeat.Driver) == player) +
                            " vFrozen=" + vehicle.IsPositionFrozen + " driveable=" + vehicle.IsDriveable + " engine=" + vehicle.IsEngineRunning;
                else line += " vehicle=none";
                line += " gate=" + (gate != null && gate.IsWaiting ? "waiting" : "open");
                if (crew != null && crew.IsDeployed) line += " companion=" + crew.CompanionAI.StateOf(crew.ActiveSlot);
                line += " scene=" + (scenes != null ? (scenes.IsActive ? "active" : scenes.LastOutcome.ToString()) : "none");
                if (homes != null) line += " apartment=" + (homes.Apartment.Busy ? "busy" : homes.Apartment.Inside ? "inside" : "no");
                line += " collision=" + Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, player);
                return Log(line);
            }
            catch (Exception ex)
            {
                Logger.Error("Control diagnostics failed", ex);
                return "";
            }
        }

        private static string Log(string line)
        {
            Logger.Info(line);
            return line;
        }
    }
}
