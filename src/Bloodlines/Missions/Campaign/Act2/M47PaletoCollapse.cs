using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// Chapter four. Gohan triggers what he clamped, the two of them go off the side,
    /// and Guess — who has put the helicopter down and taken the boat out, the way
    /// the road-back plan says — picks them out of the water and clears the area.
    ///
    /// The authored beat has the whole rig tilting forty degrees and sinking. A
    /// stationary structure with staged damage is what is actually supported: the
    /// charges go off at the mooring points that were clamped in M44, the structure
    /// burns and is finished, and nobody is asked to run across geometry that is
    /// rotating under them. That was recorded as decision D04 before this was built.
    /// </summary>
    public sealed class M47PaletoCollapse : ComposedMission
    {
        public const string BoatModel = "tropic";
        /// <summary>How far from the structure counts as out of the demolition area.</summary>
        public const float ClearRange = 120f;

        private readonly CrewBoarding _pickup = new CrewBoarding();
        /// <summary>The rail and the edge where they really are, for the same reason M45 probes its deck.</summary>
        private Vector3 _trigger, _edge;
        private Vehicle _boat;
        private Vehicle _chopper;
        private bool _triggered;
        private bool _jumped;

        /// <summary>
        /// The brother the player is not playing has to go off the side too. Nothing used to
        /// tell him to, so as Ice the stage waited on a Gohan standing at the rail forever,
        /// and as either one the other was left on a burning deck (the September 22 audit).
        /// He is walked to the rail, goes over it, and if something stops him the stage is not
        /// left hanging: after <see cref="OverboardFallbackMs"/> he is put in the water beside
        /// the hull.
        /// </summary>
        private sealed class Overboard { public int OrderedAt; public float Remaining = -1f; public int PushedAt; }
        private readonly Dictionary<CrewSlot, Overboard> _overboard = new Dictionary<CrewSlot, Overboard>();
        private int _chargesFiredAt;
        /// <summary>How long a brother may walk toward the rail without closing on it before he is sent again.</summary>
        public const int OverboardStallMs = 4000;
        /// <summary>How long after going over he may still be on the structure before he is sent to the rail again.</summary>
        public const int OverboardRetryMs = 4000;
        /// <summary>The last resort: this long after the charges, a brother still aboard is put in the water.</summary>
        public const int OverboardFallbackMs = 25000;
        /// <summary>How hard he goes over the rail, outward and up.</summary>
        public const float OverboardSpeed = 5f, OverboardLift = 3f;

        public override string Id => "M47";
        public override string Title => "Paleto Deep-Sea: Collapse";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        public Vehicle Boat => _boat;
        public bool Triggered => _triggered;
        /// <summary>Both brothers actually went into the water rather than being teleported off.</summary>
        public bool Jumped => _jumped;

        private Vector3 At(string key) => Ctx.Locations.Position(key);

        protected override bool Setup()
        {
            var world = Paleto.Of(Ctx);
            if (world != null && (!world.ChargesArmed || !world.EvidenceHeld))
                throw new InvalidOperationException("M47 opened before the charges were armed and the evidence taken.");
            if (!Paleto.IsContinuing(Ctx) && !Ctx.Crew.Deploy(CrewSlot.Gohan, At("M47.Jump"), Ctx.Locations.Heading("M47.Jump"))) return false;

            _trigger = PaletoSite.OnDeck(At("M47.Trigger"), Id + " charge rail");
            _edge = PaletoSite.OnDeck(At("M47.Jump"), Id + " jump point");

            _chopper = world?.Get<Vehicle>("chopper");
            // Opened alone there is no helicopter to inherit: stage one on the strip
            // Guess is being asked to put it down on.
            if (_chopper == null || !_chopper.Exists()) _chopper = StageHelicopter();
            if (!SpawnBoat()) return false;
            Paleto.Review(Ctx, PlacementContract.Ped("M47.Jump"), PlacementContract.Interaction("M47.Trigger"),
                PlacementContract.Aircraft("M47.Landing", new Model(M45PaletoBreach.HelicopterModel), 30f));
            world?.Bind("boat", _boat);
            RequireAsset(_boat, "The pickup boat was lost. Nobody comes off the structure without it.");
            return true;
        }

        private bool SpawnBoat()
        {
            var model = new Model(BoatModel);
            if (!GameUtils.RequestModel(model))
            { Logger.Error(Id + ": the Tropic model would not load."); return false; }
            // M43 left the Tropic on its holding marker; it is taken from there, not
            // conjured beside the swimmers at the moment they need it.
            // M40 records this under the plural key it actually writes.
            string staged = Paleto.CargoAt(Ctx, "extractionLaunches");
            string key = !string.IsNullOrEmpty(staged) && Ctx.Locations.Get(staged)?.Kind == "water" ? staged : "M47.BoatStart";
            var point = Afloat(key) ?? Afloat("M47.Clear") ?? At(key);
            _boat = Track(World.CreateVehicle(model, point, Ctx.Locations.Heading(key)));
            model.MarkAsNoLongerNeeded();
            if (_boat == null || !_boat.Exists())
            { Logger.Error(Id + ": the Tropic could not be created at " + point); return false; }
            _boat.IsPersistent = true;
            return true;
        }

        /// <summary>
        /// Water deep enough for the Tropic at a key, or null when the probe will not
        /// give it.
        ///
        /// The resolve used to be allowed to throw, and a throw out of Setup is the whole
        /// operation refusing to start: that is what Ron got after arming the charges,
        /// five chapters into one sitting. A boat put somewhere slightly wrong is
        /// recoverable. Losing the sitting is not.
        /// </summary>
        private Vector3? Afloat(string key)
        {
            if (Ctx.Locations.Get(key) == null) return null;
            try { return MarineSites.ResolveOrThrow(Ctx.Locations, key, 2f); }
            catch (Exception ex)
            {
                Logger.Warn(Id + ": no clear water for the Tropic at " + key + " (" + ex.Message + ").");
                Ctx.Doctor?.Warn("placement", key, "the boat could not be floated here: " + ex.Message);
                return null;
            }
        }

        private Vehicle StageHelicopter()
        {
            var model = new Model(M45PaletoBreach.HelicopterModel);
            if (!GameUtils.RequestModel(model)) return null;
            var heli = Track(World.CreateVehicle(model, At("M45.Hold"), Ctx.Locations.Heading("M45.Hold")));
            model.MarkAsNoLongerNeeded();
            if (heli == null || !heli.Exists()) return null;
            heli.IsPersistent = true;
            // Staged at the hold point, which is 40 meters up: same reason as M45.
            AircraftHold.LaunchAirborne(heli);
            return heli;
        }

        private void FireCharges()
        {
            _triggered = true;
            _chargesFiredAt = Game.GameTime;
            _overboard.Clear();
            foreach (var key in new[] { "M44.Clamp1", "M44.Clamp2", "M44.Clamp3" })
            {
                var point = At(key);
                World.AddExplosion(new Vector3(point.X, point.Y, PaletoSite.Keel), ExplosionType.Boat, 4f, 1.4f, null, true, false);
            }
            Logger.Info("Paleto: charges fired at the clamped mooring points. The structure stays where it is and burns.");
        }

        private bool InWater(CrewSlot slot)
        {
            var ped = Ctx.Crew.PedFor(slot);
            return ped != null && ped.Exists() && !ped.IsDead && !ped.IsInVehicle() && ped.Position.Z < PaletoSite.WaterlineDeck;
        }

        private bool Recovered =>
            _boat != null && _boat.Exists() && _boat.IsDriveable &&
            Protagonist.All.All(hero => Ctx.Crew.PedFor(hero.Slot)?.IsInVehicle(_boat) == true);

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Put the helicopter down and take the boat",
                new DeliverVehicleObjective("Guess: land the Annihilator on the cove strip", () => _chopper, () => At("M47.Landing"), 6f, true),
                new EnterVehicleObjective("Guess: take the Tropic out to the pickup marker", () => _boat, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Trigger the charges",
                new MissionInteraction("Gohan: trigger the charges from the rail", () => _trigger, 3, 3f,
                    animation: MissionInteraction.Kneel))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c => FireCharges())
                .AfterCues("M47_S1_01_GOHAN");

            // Either of them. Unowned, this inherited Gohan from the trigger, so as Ice the
            // objective read "Switch to Gohan" while Ice was the one standing at the edge.
            yield return new MissionStage("Go off the side",
                new ConditionObjective("Ice and Gohan: get to the edge and go into the water",
                    () => InWater(CrewSlot.Ice) && InWater(CrewSlot.Gohan)) { Marker = () => _edge, MarkerRadius = 4f })
                .AnyOf()
                .AnyBrother()
                .OnExit(c => _jumped = true)
                .AfterCues("M47_S1_02_ICE");

            yield return new MissionStage("Pick them up",
                new ConditionObjective("Guess: bring the boat onto both swimmers until all three are aboard", () => Recovered))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(c => _pickup.Reset())
                .AfterCues("M47_S1_03_GUESS");

            yield return new MissionStage("Clear the demolition area",
                new TravelObjective("Guess: take the boat clear of the burning vessel", () => At("M47.Clear"), 20f, () => _boat))
                .OwnedBy(CrewSlot.Guess);
        }

        /// <summary>
        /// Two men in the water do not climb into a passing boat by themselves, and the
        /// stage that waits for them only ever asked whether they had. Guess still has to
        /// bring the boat onto them — the swim to it is a few meters, not a few hundred —
        /// but somebody has to tell them to make it.
        /// </summary>
        protected override void OnUpdate()
        {
            if (_triggered && !_jumped) SendOverTheSide();
            if (_jumped && !Recovered) _pickup.Update(Ctx.Crew, _boat, Swimmers, Id);
            base.OnUpdate();
        }

        /// <summary>
        /// Walk each brother the player is not playing to the rail and put him over it. The
        /// walk is ordered once and only re-ordered when he has stopped closing on the rail;
        /// the jump is a push outward from wherever he stopped, which is the rail whether or
        /// not the hull's extent reaches the marked point.
        /// </summary>
        private void SendOverTheSide()
        {
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan })
            {
                if (slot == Ctx.Crew.ActiveSlot) { _overboard.Remove(slot); continue; }
                if (InWater(slot)) continue;
                var ped = Ctx.Crew.PedFor(slot);
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                if (!_overboard.TryGetValue(slot, out var state)) { state = new Overboard(); _overboard[slot] = state; }
                int now = Game.GameTime;

                if (now - _chargesFiredAt > OverboardFallbackMs)
                {
                    // Something kept him on the structure: a rail with no way over, a
                    // navmesh that ends short of it. The stage cannot wait on that.
                    var water = OutsideTheRail(ped.Position, 4f);
                    ped.Task.ClearAllImmediately();
                    ped.Position = new Vector3(water.X, water.Y, 0.5f);
                    Logger.Warn(Id + ": " + slot + " could not get off the structure in " + (OverboardFallbackMs / 1000) + " s; placed him in the water beside the hull.");
                    continue;
                }

                if (state.PushedAt != 0)
                {
                    if (now - state.PushedAt < OverboardRetryMs) continue;
                    // Over the rail and still aboard: landed on a lower deck. Send him again.
                    state.PushedAt = 0; state.OrderedAt = 0; state.Remaining = -1f;
                }

                var rail = RailPoint(ped.Position);
                float remaining = ped.Position.DistanceTo2D(rail);
                bool closing = state.Remaining < 0f || state.Remaining - remaining > 1f;
                if (closing) state.Remaining = remaining;
                bool stopped = state.OrderedAt != 0 && now - state.OrderedAt > OverboardStallMs && !closing;
                if (remaining > 2f && !stopped)
                {
                    if (state.OrderedAt == 0)
                    {
                        Ctx.Crew.CompanionAI.TakeControl(slot);
                        ped.Task.ClearAll();
                        ped.Task.GoTo(rail);
                        state.OrderedAt = now;
                    }
                    else if (closing) state.OrderedAt = now;
                    continue;
                }

                // At the rail, or as close as the deck lets him get: over the side.
                var outward = OutwardFrom(ped.Position);
                ped.Task.ClearAllImmediately();
                Function.Call(Hash.SET_PED_TO_RAGDOLL, ped, 2500, 2500, 0, false, false, false);
                Function.Call(Hash.SET_ENTITY_VELOCITY, ped, outward.X * OverboardSpeed, outward.Y * OverboardSpeed, OverboardLift);
                state.PushedAt = now;
                Logger.Info(Id + ": " + slot + " goes over the side at " + ped.Position + ".");
            }
        }

        /// <summary>Flat unit direction off the nearer beam of the hull.</summary>
        private static Vector3 OutwardFrom(Vector3 at) =>
            at.Y - PaletoSite.HullMin.Y < PaletoSite.HullMax.Y - at.Y ? new Vector3(0f, -1f, 0f) : new Vector3(0f, 1f, 0f);

        /// <summary>The rail on the nearer beam, a meter inboard, level with where he stands.</summary>
        private static Vector3 RailPoint(Vector3 at)
        {
            var outward = OutwardFrom(at);
            float y = outward.Y < 0f ? PaletoSite.HullMin.Y + 1f : PaletoSite.HullMax.Y - 1f;
            float x = Math.Max(PaletoSite.HullMin.X + 1f, Math.Min(PaletoSite.HullMax.X - 1f, at.X));
            return new Vector3(x, y, at.Z);
        }

        /// <summary>Open water just off the nearer beam.</summary>
        private static Vector3 OutsideTheRail(Vector3 at, float clearance)
        {
            var rail = RailPoint(at);
            return rail + OutwardFrom(at) * (1f + clearance);
        }

        /// <summary>Guess is driving, so the two who went off the side take the other seats.</summary>
        private static readonly System.Collections.Generic.KeyValuePair<CrewSlot, VehicleSeat>[] Swimmers =
            CrewBoarding.Passengers(CrewSlot.Guess);

        protected override void OnPassed()
        {
            if (!_triggered || !_jumped) throw new InvalidOperationException("The charges and the jump both have to have happened.");
            if (!Recovered) throw new InvalidOperationException("All three have to be in the boat.");
            var record = OperationHandoff.Capture(Paleto.Operation.Title, Id, "M48", Ctx.Crew, _boat);
            record.Notes["boat"] = "all three aboard, clear of the demolition area";
            record.Notes["structure"] = "burning at its moorings; nobody left on it";
            Ctx.Handoffs.Record(record);
            Release(_boat);
        }
    }
}
