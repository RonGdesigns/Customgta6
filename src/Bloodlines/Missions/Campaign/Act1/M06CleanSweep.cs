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
    /// M06 — "Clean Sweep". Vespucci canals LSPD evidence depot, 01:30, fog and rain.
    ///
    /// The biometric backups have to burn. Gohan comes up through the drainage
    /// culverts and cuts the 480-volt feeder, Ice holds the alley against SWAT while
    /// the racks melt, Guess reverses the Granger through the cruisers to get everyone
    /// out.
    ///
    /// Seen, not told: the three positions before anyone moves; Gohan's work at the
    /// racks and the fire it leaves; the rotors over the roofline turning Ron's
    /// waiting job into a pickup at the alley mouth, where Ice and Gohan come to him
    /// and board real seats. The record's destruction is recorded as evidence; the
    /// escape is an escape checkpoint, nothing clears the police for the crew.
    ///
    /// This is the mission the wave objective was written for: Ice's job is not to
    /// clear the alley, it is to hold it for as long as the thermite takes. Two
    /// objectives running at once in the same stage — one a burn timer, one a siege.
    /// </summary>
    public sealed class M06CleanSweep : ComposedMission
    {
        private readonly List<Ped> _swat = new List<Ped>();
        private readonly List<HeliInsertion> _insertions = new List<HeliInsertion>();
        /// <summary>A SWAT foot squad entering through one alley mouth.</summary>
        private sealed class Convoy
        {
            public Vector3 Destination; public List<Ped> Crew = new List<Ped>(); public int NextOrders, UnloadedAt;
            /// <summary>Who each trooper was last ordered to fight, by handle; absent while he is still running in.</summary>
            public readonly Dictionary<int, Ped> FightingAgainst = new Dictionary<int, Ped>();
            /// <summary>Where each running trooper stood at the last review, to tell a stalled run from a moving one.</summary>
            public readonly Dictionary<int, Vector3> LastSeen = new Dictionary<int, Vector3>();
        }
        /// <summary>How far a running trooper has to have moved between reviews for his run to count as under way.</summary>
        public const float RunStallMeters = 1f;
        private bool _boardingOpen;
        private readonly List<Convoy> _convoys = new List<Convoy>();
        public IReadOnlyList<Vehicle> Convoys => new Vehicle[0];
        private bool _airMomentPlayed, _pickupCalled;
        private int _fire = -1;

        private Vehicle _granger;
        private Prop _rackBench;
        private readonly List<Prop> _rackCases = new List<Prop>();
        private RoleTracks _roles;
        private Vector3 _culvert;
        private Vector3 _feeder;
        private Vector3 _sallyPort;
        private Vector3 _racks;
        private Vector3 _alley;
        private Vector3 _pickup;

        public override string Id => "M06";
        public override string Title => "Clean Sweep";

        public Vehicle Granger => _granger;
        /// <summary>No case at the feeder anymore: the cut is the map's own power box (Ron, September 12).</summary>
        public Prop FeederPanel => null;
        public Prop RackBench => _rackBench;
        public IReadOnlyList<Prop> RackCases => _rackCases;
        public RoleTracks Roles => _roles;
        public bool PickupCalled => _pickupCalled;
        public bool FireBurning => _fire >= 0;
        public Vector3 Pickup => _pickup;

        protected override bool Setup()
        {
            if (!MissionSites.Ground(Ctx.Locations, "M06.Culvert", "M06.Feeder", "M06.SallyPort", "M06.ServerRacks", "M06.AlleyHold", "M06.GrangerSpawn")) return false;
            _culvert = Ctx.Locations.Position("M06.Culvert");
            // Gohan's two work points off the street (Ron, September 11: he stood in
            // the middle of the road): the sidewalk nearest each estimate, with a real
            // thing at each to work on.
            // The feeder is the real power box Ron surveyed (September 12); an estimate still goes to the sidewalk.
            _feeder = Ctx.Locations.Get("M06.Feeder")?.Status == LocationStatus.Surveyed ? Ctx.Locations.Position("M06.Feeder") : OffStreet(Ctx.Locations.Position("M06.Feeder"));
            _sallyPort = Ctx.Locations.Position("M06.SallyPort");
            _racks = OffStreet(Ctx.Locations.Position("M06.ServerRacks"));
            _alley = Ctx.Locations.Position("M06.AlleyHold");
            // The crew's actual exit: the alley mouth on the Granger's side, where
            // Ron brings the truck once the rotors are over the roof.
            _pickup = _alley + new Vector3(-10f, 7f, 0f);

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, new Dictionary<CrewSlot, PedPlacement>
            {
                [CrewSlot.Gohan] = new PedPlacement(_culvert, 0f),
                // Both approach from the culvert; the feeder is Gohan's destination.
                [CrewSlot.Ice] = new PedPlacement(_culvert + new Vector3(3f, -2.5f, 0f), Ctx.Locations.Heading("M06.Culvert")),
                [CrewSlot.Guess] = new PedPlacement(Ctx.Locations.Position("M06.GrangerSpawn"), Ctx.Locations.Heading("M06.GrangerSpawn"))
            })) return false;
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.TakeControl(hero.Slot);
            Ctx.Crew.PedFor(CrewSlot.Ice).Task.GuardCurrentPosition();

            ApplyBibleSetting();
            SpawnGranger();
            SpawnWorkProps();
            if (_granger == null || !_granger.Exists()) return false;
            Ctx.Crew.PedFor(CrewSlot.Guess).SetIntoVehicle(_granger, VehicleSeat.Driver);
            _roles = new RoleTracks(Ctx.Crew, () => _swat);
            PlayPositions();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Cut the power",
                    new MissionInteraction("Gohan: cut the marked power feeder", () => _feeder, 6, 3f, animation: MissionInteraction.ReachInside))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => Say("M06_S1_01_GOHAN"));

            yield return new MissionStage("Sally port",
                    new ReachZoneObjective("Ice: walk into the yellow depot entrance marker.", () => _sallyPort, 5f))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context => Say("M06_S1_02_ICE"));

            // The burn and the siege run together: the thermite does not care how the
            // alley is going, and the alley does not stop when the racks are slag.
            // The rotors are the change of plan: Ron stops waiting and brings the truck.
            yield return new MissionStage("Burn the racks",
                    new AssignedWorkObjective("Gohan is preparing the thermite. Ice: hold the alley while he works.", CrewSlot.Gohan, () => _racks, 20),
                    new SurviveWavesObjective("Ice: defeat the RED-marked SWAT waves while Gohan finishes the burn. Stay on Ice.", SpawnSwatWave, 3, 6000),
                    new ReactionTrigger(() => _airMomentPlayed, CallPickup))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context => { Say("M06_S2_03_ICE"); Game.Player.WantedLevel = 3; context.Crew.CompanionAI.ReleaseControl(CrewSlot.Ice); context.Crew.CompanionsHoldPosition = true; })
                .OnExit(context => RacksBurned());

            yield return new MissionStage("The pickup",
                    new ReachZoneObjective("Guess: bring the Granger to the alley mouth for Ice and Gohan.", () => _pickup, 9f, flat: true, requireVehicle: true))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context =>
                {
                    Say("M06_S2_05_GUESS");
                    if (_granger != null && _granger.Exists()) _granger.IsEngineRunning = true;
                    _roles.For(CrewSlot.Ice).Extract(_pickup);
                    _roles.For(CrewSlot.Gohan).Extract(_pickup);
                    // Only now does the truck move: the siege is over (Ron, September 11:
                    // Ron's AI drove into the fight). His own AI brings it if the player is elsewhere.
                    DriveIn();
                });

            yield return new MissionStage("Everyone aboard",
                    new ConditionObjective("Hold at the alley mouth until all three are in the Granger. Once aboard, any brother can continue.", AllAboard))
                .AnyBrother()
                .OnEnter(context =>
                {
                    _boardingOpen = true;
                    // The truck is the crew's from here: it can be lost again.
                    if (_granger != null && _granger.Exists()) _granger.IsInvincible = false;
                    _roles.Release();
                    context.Crew.CompanionAI.ReleaseAll();
                    context.Crew.CompanionsHoldPosition = false;
                    context.Crew.CompanionAI.RequireSharedVehicle = true;
                    context.Crew.AssignCompanionAI();
                });

            yield return new MissionStage("Out of Vespucci",
                    new LoseWantedObjective("Lose the police."),
                    new ConditionObjective("Keep all three aboard the Granger for the escape.", () =>
                        Protagonist.All.All(hero =>
                        {
                            var ped = Ctx.Crew.PedFor(hero.Slot);
                            return ped != null && ped.Exists() && ped.IsAlive && ped.IsInVehicle(_granger);
                        })),
                    new ProtectObjective("", () => _granger, "The Granger was destroyed."))
                .AnyBrother()
                .OnEnter(context =>
                {
                    RequiredSwitch = null;
                    context.Switching.SetUnlocked();
                    // Boarding already released scripted roles. The shared driver
                    // takes over Guess's existing seat when a passenger is selected.
                    GameUtils.Subtitle("~y~Everyone is aboard. Switch to any brother; lose the police together.", 5000);
                })
                .OnExit(context => GameUtils.Subtitle("~g~Depot clean. Nothing left to match a face to.", 5000));
        }

        // ---------- beats ----------

        /// <summary>The depot from three places before anyone moves: Gohan under the feeder, Ice in the alley, the Granger.</summary>
        private void PlayPositions()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking()
                .Then(gohan != null ? ShotStep.Low(3000, gohan, 2.4f, 0.8f, 0.9f) : (SceneStep)new WaitStep(3000))
                .Then(ice != null ? ShotStep.OverShoulder(3000, ice, _granger, 0.3f) : (SceneStep)new WaitStep(3000))
                .Then(new ShotStep(2800, _granger, new Vector3(-6f, 2.5f, 1.5f), _granger, new Vector3(0f, 0f, 0.8f), 1.0f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "positions", Title = "Positions",
                Reason = "The backup is a separate copy in this depot; Gohan's access point, Ice's breach angle and Ron's extraction position are where they will be needed.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M06 positions scene did not play; the feeder objective stands on its own.");
        }

        /// <summary>Rotors over the roofline: Ron says the pickup is his once the burn is done. The truck stays put until then.</summary>
        private void CallPickup()
        {
            if (_pickupCalled) return;
            _pickupCalled = true;
            Radio("GUESS", "Rotors over the roofline. Finish the burn and hold the alley; the second it's done I bring the Granger to the alley mouth. Not before.", "M06_RADIO_01_GUESS");
        }

        /// <summary>Ron's own AI brings the truck to the alley mouth when the player is somebody else; the player drives it himself otherwise.</summary>
        private void DriveIn()
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (guess == null || !guess.Exists() || guess.Handle == Game.Player.Character.Handle || _granger == null || !_granger.Exists()) return;
            Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess);
            if (!guess.IsInVehicle(_granger)) guess.SetIntoVehicle(_granger, VehicleSeat.Driver);
            _granger.IsEngineRunning = true;
            guess.Task.DriveTo(_granger, _pickup, 6f, 12f, DrivingStyle.Normal);
        }

        /// <summary>
        /// A place a trooper can stand: the navmesh's nearest walkable point to the
        /// offset, then the sidewalk, then a step toward the alley (Ron, September 11:
        /// one spawned inside a wall). Never the raw offset when the map has an answer.
        /// </summary>
        private Vector3 StreetPost(Vector3 wanted)
        {
            var safe = World.GetSafeCoordForPed(wanted, false, 0);
            if (safe != Vector3.Zero && GameUtils.IsWithinFlat(safe, wanted, 20f) && Math.Abs(safe.Z - wanted.Z) < 4f) return safe;
            safe = World.GetSafeCoordForPed(wanted, true, 16);
            if (safe != Vector3.Zero && GameUtils.IsWithinFlat(safe, wanted, 25f) && Math.Abs(safe.Z - wanted.Z) < 4f) return safe;
            var toward = _alley - wanted; toward.Z = 0f;
            float run = (float)Math.Sqrt(toward.X * toward.X + toward.Y * toward.Y);
            if (run > 1f)
            {
                var closer = wanted + toward * (10f / run);
                safe = World.GetSafeCoordForPed(closer, false, 0);
                if (safe != Vector3.Zero && GameUtils.IsWithinFlat(safe, closer, 20f) && Math.Abs(safe.Z - closer.Z) < 4f) return safe;
            }
            Logger.Warn("M06: no walkable point near a SWAT spawn at " + wanted + "; using it as is.");
            return wanted;
        }

        /// <summary>The sidewalk nearest an estimated work point, so the work is not done in the road; the point itself when none is close.</summary>
        private static Vector3 OffStreet(Vector3 point)
        {
            var side = World.GetSafeCoordForPed(point, true, 16);
            return side != Vector3.Zero && GameUtils.IsWithinFlat(side, point, 25f) ? side : point;
        }

        /// <summary>The backup array on a bench at the racks: a thing Gohan works on, not a mark in the street. The feeder is the map's own power box; no case is placed there (Ron, September 12).</summary>
        private void SpawnWorkProps()
        {
            var benchModel = new Model("prop_table_03");
            var caseModel = new Model("prop_ld_case_01");
            if (!GameUtils.RequestModel(benchModel) || !GameUtils.RequestModel(caseModel)) return;
            _rackBench = Track(World.CreateProp(benchModel, _racks + new Vector3(0f, 1.2f, 0f), false, true));
            benchModel.MarkAsNoLongerNeeded();
            if (_rackBench != null && _rackBench.Exists())
            {
                _rackBench.IsPersistent = true; _rackBench.IsPositionFrozen = true;
                for (int i = 0; i < 2; i++)
                {
                    var box = Track(World.CreateProp(caseModel, _rackBench.Position + new Vector3(0f, 0f, 1f), false, false));
                    if (box == null || !box.Exists()) continue;
                    box.IsPersistent = true;
                    StowPropStep.Stow(box, _rackBench, new Vector3(i == 0 ? -0.35f : 0.35f, 0f, 0.8f));
                    _rackCases.Add(box);
                }
            }
            caseModel.MarkAsNoLongerNeeded();
            Logger.Info("M06 work points: the power box at " + _feeder + ", backup bench at " + _racks + ".");
        }

        /// <summary>The racks are slag: a real fire where the work was, and the record's destruction on the books.</summary>
        private void RacksBurned()
        {
            Say("M06_S2_04_GOHAN");
            if (_fire < 0) _fire = Function.Call<int>(Hash.START_SCRIPT_FIRE, _racks.X, _racks.Y, _racks.Z, 6, false);
            Ctx.State?.SetEvidence("vespucciBackup", EvidenceState.Destroyed);
            GameUtils.Subtitle("~g~Core is slag. To the Granger at the alley mouth.", 4000);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            // Tied to the boarding stage's own entry rather than its index, so a stage
            // inserted earlier cannot silently move the unlock.
            if(_boardingOpen&&AllAboard()){RequiredSwitch=null;Ctx.Switching.SetUnlocked();}
            _roles?.Update();
            foreach (var insertion in _insertions) insertion.Update();
            _insertions.RemoveAll(insertion => insertion.Current == HeliInsertion.Phase.Done);
            MaintainConvoys();
        }
        private bool AllAboard() => _granger!=null&&_granger.Exists()&&Protagonist.All.All(h=>
        {var p=Ctx.Crew.PedFor(h.Slot);return p!=null&&p.Exists()&&p.IsAlive&&p.IsInVehicle(_granger);});

        /// <summary>The aftermath: the Granger with the three aboard, away from the smoke.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_granger == null || !_granger.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _granger, new Vector3(-7f, 3f, 1.8f), _granger, new Vector3(0f, 0f, 0.8f), 1.2f));
        }

        /// <summary>
        /// The first wave is already at the depot. The second and third come in over
        /// the roofs: two Mavericks each, two troopers on ropes per aircraft, the rest
        /// of the wave on the street. The first aircraft's arrival is shown once.
        /// </summary>
        private IEnumerable<Ped> SpawnSwatWave(int wave)
        {
            var model = new Model("s_m_y_swat_01");
            if (!GameUtils.RequestModel(model)) return Enumerable.Empty<Ped>();

            var police = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            var spawned = new List<Ped>();
            var airborne = new List<Ped>();
            var byRoad = new List<Ped>();
            // The first wave is already on the street. The later waves come by air
            // and on foot: two on each Maverick's ropes, the rest approaching
            // from the two alley mouths without road-navigation detours.
            int count = wave >= 2 ? 4 + 2 * wave : 3 + wave;
            int byAir = wave >= 2 ? Math.Min(count, 2 * HeliInsertion.Capacity) : 0;
            int roadCount = wave >= 2 ? count - byAir : 0;

            for (int i = 0; i < count; i++)
            {
                var offset = new Vector3(-8f + i * 3f, 22f + (i % 2) * 5f, 0f);
                var post = StreetPost(_alley + offset);
                var trooper = World.CreatePed(model, post, DriveUpStep.HeadingBetween(post, _alley));
                if (trooper == null || !trooper.Exists()) continue;

                trooper.RelationshipGroup = police;
                trooper.IsPersistent = true;
                trooper.BlockPermanentEvents = true;
                trooper.Accuracy = 30 + wave * 5;
                trooper.Armor = 50;
                trooper.Weapons.Give(wave >= 2 ? WeaponHash.CarbineRifle : WeaponHash.SMG, 200, true, true);
                if (i < byAir) airborne.Add(trooper);
                else if (i < byAir + roadCount) byRoad.Add(trooper);
                else trooper.Task.FightAgainstHatedTargets(90f);

                spawned.Add(Track(trooper));
                _swat.Add(trooper);
            }
            model.MarkAsNoLongerNeeded();
            if (byRoad.Count > 0) LaunchConvoys(byRoad);

            for (int group = 0; group * HeliInsertion.Capacity < airborne.Count; group++)
            {
                var load = airborne.Skip(group * HeliInsertion.Capacity).Take(HeliInsertion.Capacity).ToList();
                var insertion = HeliInsertion.Launch(_alley, 200f + group * 70f, load, entity => Track(entity));
                if (insertion == null)
                {
                    // No aircraft: these troopers are already standing at their street spawn.
                    foreach (var trooper in load) trooper.Task.FightAgainstHatedTargets(90f);
                    continue;
                }
                _insertions.Add(insertion);
                if (!_airMomentPlayed)
                {
                    _airMomentPlayed = true;
                    Ctx.Cutscenes.PlayMoment(Id, "SWAT air support", "ICE", "Rotors. They're putting the next team down from the roofline. Keep the alley.", insertion.Pilot);
                }
            }
            return spawned;
        }

        /// <summary>Two foot approaches, one from each alley mouth, using the existing survey entry pairs.</summary>
        private void LaunchConvoys(List<Ped> troopers)
        {
            // Reuse the editor's two entry/stop pairs as foot approaches. A distant
            // road-node detour must never strand a required wave on another block.
            var axis = _pickup - _alley; axis.Z = 0f;
            float length = (float)Math.Sqrt(axis.X * axis.X + axis.Y * axis.Y);
            axis = length > .5f ? axis * (1f / length) : new Vector3(1f, 0f, 0f);
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? 1f : -1f;
                var fallback = _alley + axis * (sign * 30f);
                var spawn = MissionPlacement.Position(Ctx.Locations, "M06.ConvoySpawn" + (side + 1), fallback);
                if (!GameUtils.IsWithinFlat(spawn, _alley, 75f) || Math.Abs(spawn.Z - _alley.Z) > 5f) spawn = fallback;
                var stop = MissionPlacement.Position(Ctx.Locations, "M06.ConvoyStop" + (side + 1), _alley + axis * (sign * 12f));
                if (!GameUtils.IsWithinFlat(stop, _alley, 30f) || Math.Abs(stop.Z - _alley.Z) > 5f) stop = _alley + axis * (sign * 12f);
                var approach = new Convoy { Destination = StreetPost(stop), UnloadedAt = Game.GameTime };
                int row = 0;
                foreach (var trooper in troopers.Where((t, index) => index % 2 == side))
                {
                    if (trooper == null || !trooper.Exists()) continue;
                    trooper.Position = StreetPost(spawn + new Vector3(row++ * 2f, 0f, 0f));
                    trooper.AlwaysKeepTask = true;
                    trooper.Task.RunTo(approach.Destination, false, 20000);
                    approach.Crew.Add(trooper);
                }
                _convoys.Add(approach);
            }
            Logger.Info("M06: SWAT approaching both alley mouths on foot.");
        }

        /// <summary>
        /// The foot approaches, reviewed every three seconds but ordered only on a change.
        /// Ron, September 22: every trooper inside 24 m was handed a fresh fight order on
        /// every review, and every runner a fresh run, which restarts the task before the
        /// ped can act on it. A trooper is ordered to fight once, and again only when he has
        /// dropped out of combat or Ice is a different man; once fighting he is never sent
        /// back to his run. A runner is re-sent only when his run has stalled.
        /// </summary>
        private void MaintainConvoys()
        {
            foreach (var approach in _convoys)
            {
                if (Game.GameTime < approach.NextOrders) continue;
                approach.NextOrders = Game.GameTime + 3000;
                foreach (var trooper in approach.Crew)
                {
                    if (trooper == null || !trooper.Exists() || trooper.IsDead) continue;
                    var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
                    if (ice == null || !ice.Exists() || ice.IsDead) continue;
                    int handle = trooper.Handle;
                    bool ordered = approach.FightingAgainst.TryGetValue(handle, out var target);
                    if (ordered || GameUtils.IsWithinFlat(trooper.Position, _alley, 24f))
                    {
                        if (ordered && target == ice && trooper.IsInCombat) continue;
                        trooper.Task.FightAgainst(ice);
                        approach.FightingAgainst[handle] = ice;
                        approach.LastSeen.Remove(handle);
                        continue;
                    }
                    var entry = approach.Destination;
                    bool moved = !approach.LastSeen.TryGetValue(handle, out var seen) || !GameUtils.IsWithinFlat(trooper.Position, seen, RunStallMeters);
                    approach.LastSeen[handle] = trooper.Position;
                    if (Game.GameTime - approach.UnloadedAt > 25000 && !trooper.IsOnScreen &&
                        !Function.Call<bool>(Hash.IS_SPHERE_VISIBLE, entry.X, entry.Y, entry.Z, 3f))
                    {
                        trooper.Position = entry;
                        approach.LastSeen[handle] = entry;
                        moved = false;
                    }
                    if (!moved) trooper.Task.RunTo(entry, false, 20000);
                }
            }
        }

        private void SpawnGranger()
        {
            // The crew's own Granger, customized as they left it; the M11 package goes on top.
            // Staged away from the depot, on a road node (Ron, September 11: it was lost
            // at the alley before the pickup), and protected until the crew boards it.
            var spot = Ctx.Locations.Position("M06.GrangerSpawn"); float heading = Ctx.Locations.Heading("M06.GrangerSpawn");
            if (GameUtils.NearestRoadNode(spot, 40f, out var node, out float nodeHeading)) { spot = node; heading = nodeHeading; }
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(spot, heading) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, spot, heading);
                model.MarkAsNoLongerNeeded();
            }
            _granger = Track(granger);
            if (_granger == null || !_granger.Exists()) return;

            _granger.IsPersistent = true;
            _granger.IsEngineRunning = false;
            _granger.IsInvincible = true;
            float standoff = _granger.Position.DistanceTo(_alley);
            if (standoff < 150f) Logger.Warn("M06: the Granger is staged only " + standoff.ToString("0") + " m from the alley; move M06.GrangerSpawn farther out.");
            else Logger.Info("M06: the Granger staged " + standoff.ToString("0") + " m from the alley, protected until the crew boards.");

            var blip = Track(_granger.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Armored Granger";
        }

        protected override void OnCleanup()
        {
            _roles?.Release();
            if (_granger != null && _granger.Exists()) _granger.IsInvincible = false;
            if (_fire >= 0) { Function.Call(Hash.REMOVE_SCRIPT_FIRE, _fire); _fire = -1; }
            _swat.Clear();
            _rackCases.Clear();
            _insertions.Clear();
            _convoys.Clear();
            _boardingOpen = false;
        }
    }
}
