using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Crew;
using GTA;

namespace Bloodlines.Core
{
    /// <summary>
    /// Getting the other brothers into a vehicle, rather than waiting to notice that
    /// they are already in it.
    ///
    /// <c>EnterVehicleObjective(requireCrew: true)</c> and a hand-written "everyone
    /// aboard" condition both only ask whether the crew are seated. Nothing orders
    /// them, and a brother stood on a beach or treading water beside a boat will not
    /// climb in on his own, so an objective written that way waits forever. The
    /// preparation missions have their own board helper; the plain composed missions had
    /// nothing, which is why the Paleto pickup and the run south could not be finished.
    ///
    /// Bounded on purpose, and per brother:
    /// <list type="bullet">
    /// <item>The order is reissued on a cooldown, never every frame: re-tasking a ped
    /// restarts the walk or the swim he was part way through.</item>
    /// <item>He is ordered from any distance, because the task includes getting there —
    /// but the patience clock only runs while he is within <see cref="BoardRange"/>.
    /// A brother crossing a beach is not failing to board; a brother stood at an open
    /// door for <see cref="DirectAfterMs"/> is.</item>
    /// <item>Then he is seated outright, with a warning in the log. A boarding step must
    /// not be able to strand the player, watching a mission wait on something he cannot
    /// influence.</item>
    /// </list>
    /// </summary>
    public sealed class CrewBoarding
    {
        /// <summary>How often the order to board is repeated.</summary>
        public const int OrderIntervalMs = 2500;
        /// <summary>How long a brother within reach gets to board on his own before he is placed.</summary>
        public const int DirectAfterMs = 20000;
        /// <summary>Close enough that boarding is a thing he can actually be doing.</summary>
        public const float BoardRange = 30f;

        private sealed class Attempt { public int Since; public int NextOrder; }

        private readonly Dictionary<CrewSlot, Attempt> _attempts = new Dictionary<CrewSlot, Attempt>();

        /// <summary>How many brothers were placed rather than boarding on their own feet.</summary>
        public int Placed { get; private set; }

        /// <summary>Forget every clock, so a second boarding starts fresh.</summary>
        public void Reset() { _attempts.Clear(); }

        /// <summary>
        /// Call every frame while the boarding is wanted. Orders the named brothers into
        /// the named seats and reports whether every one of them is in the vehicle. The
        /// active character is never ordered: the player boards himself.
        /// </summary>
        public bool Update(CrewRoster crew, Vehicle vehicle, IEnumerable<KeyValuePair<CrewSlot, VehicleSeat>> seats, string missionId)
        {
            if (crew == null || seats == null) return false;
            if (vehicle == null || !vehicle.Exists() || vehicle.IsDead || !vehicle.IsDriveable) return false;
            int now = Game.GameTime;

            bool all = true;
            foreach (var pair in seats)
            {
                var brother = crew.PedFor(pair.Key);
                if (brother == null || !brother.Exists() || brother.IsDead) continue;
                if (brother.IsInVehicle(vehicle)) { _attempts.Remove(pair.Key); continue; }

                all = false;
                // The player is boarding himself; ordering him would take the controls away.
                if (crew.ActiveSlot == pair.Key) continue;

                if (!_attempts.TryGetValue(pair.Key, out var attempt))
                    _attempts[pair.Key] = attempt = new Attempt { Since = now };

                // Still on his way. He is ordered, but he is not yet failing to board,
                // so the clock is held at now rather than running while he travels.
                if (brother.Position.DistanceTo(vehicle.Position) > BoardRange) attempt.Since = now;
                else if (now - attempt.Since > DirectAfterMs)
                {
                    brother.SetIntoVehicle(vehicle, pair.Value);
                    if (!brother.IsInVehicle(vehicle)) continue;
                    _attempts.Remove(pair.Key);
                    Placed++;
                    Logger.Warn(missionId + ": " + pair.Key + " never made it into his seat on his own and was placed in it.");
                    continue;
                }

                if (now < attempt.NextOrder) continue;
                attempt.NextOrder = now + OrderIntervalMs;
                crew.CompanionAI.TakeControl(pair.Key);
                // Already in something else — he has to get out before he can get in.
                if (brother.IsInVehicle()) brother.Task.LeaveVehicle();
                else brother.Task.EnterVehicle(vehicle, pair.Value);
            }
            return all;
        }

        /// <summary>
        /// The ordinary arrangement: one brother named as the driver, the other two
        /// alongside him. Written as a method so a mission states who is driving and
        /// does not have to know the roster order.
        /// </summary>
        public static KeyValuePair<CrewSlot, VehicleSeat>[] Passengers(CrewSlot driver,
            VehicleSeat first = VehicleSeat.RightFront, VehicleSeat second = VehicleSeat.LeftRear)
        {
            var others = Protagonist.All.Select(hero => hero.Slot).Where(slot => slot != driver).ToArray();
            var seats = new List<KeyValuePair<CrewSlot, VehicleSeat>>();
            if (others.Length > 0) seats.Add(new KeyValuePair<CrewSlot, VehicleSeat>(others[0], first));
            if (others.Length > 1) seats.Add(new KeyValuePair<CrewSlot, VehicleSeat>(others[1], second));
            return seats.ToArray();
        }
    }
}
