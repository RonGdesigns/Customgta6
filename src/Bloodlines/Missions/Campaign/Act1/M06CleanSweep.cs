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
        private bool _airMomentPlayed, _pickupCalled;
        private int _fire = -1;

        private Vehicle _granger;
        private Prop _feederPanel, _rackBench;
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
        public Prop FeederPanel => _feederPanel;
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
            _feeder = OffStreet(Ctx.Locations.Position("M06.Feeder"));
            _sallyPort = Ctx.Locations.Position("M06.SallyPort");
            _racks = OffStreet(Ctx.Locations.Position("M06.ServerRacks"));
            _alley = Ctx.Locations.Position("M06.AlleyHold");
            // The crew's actual exit: the alley mouth on the Granger's side, where
            // Ron brings the truck once the rotors are over the roof.
            _pickup = _alley + new Vector3(-10f, 7f, 0f);

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, new Dictionary<CrewSlot, PedPlacement>
            {
                [CrewSlot.Gohan] = new PedPlacement(_culvert, 0f),
                [CrewSlot.Ice] = new PedPlacement(_alley, Ctx.Locations.Heading("M06.AlleyHold")),
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
                    new MissionInteraction("Gohan: cut the marked power feeder", () => _feeder, 6, 3f))
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
                });

            yield return new MissionStage("Everyone aboard",
                    new EnterVehicleObjective("Guess: hold at the alley mouth until Ice and Gohan are in the Granger.", () => _granger, VehicleSeat.Driver, requireCrew: true))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context =>
                {
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
                    new ProtectObjective("", () => _granger, "The Granger was destroyed."))
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

        /// <summary>Rotors over the roofline: Ron's waiting job becomes a pickup. His own AI moves the truck if the player is elsewhere.</summary>
        private void CallPickup()
        {
            if (_pickupCalled) return;
            _pickupCalled = true;
            Radio("GUESS", "Rotors over the roofline. I'm bringing the Granger to the alley mouth. Both of you come to me when it's done.", "M06_RADIO_01_GUESS");
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (guess == null || !guess.Exists() || guess.Handle == Game.Player.Character.Handle || _granger == null || !_granger.Exists()) return;
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
            if (safe != Vector3.Zero && GameUtils.IsWithinFlat(safe, wanted, 20f)) return safe;
            safe = World.GetSafeCoordForPed(wanted, true, 16);
            if (safe != Vector3.Zero && GameUtils.IsWithinFlat(safe, wanted, 25f)) return safe;
            var toward = _alley - wanted; toward.Z = 0f;
            float run = (float)Math.Sqrt(toward.X * toward.X + toward.Y * toward.Y);
            if (run > 1f)
            {
                var closer = wanted + toward * (10f / run);
                safe = World.GetSafeCoordForPed(closer, false, 0);
                if (safe != Vector3.Zero && GameUtils.IsWithinFlat(safe, closer, 20f)) return safe;
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

        /// <summary>A feeder panel at the cut and the backup array on a bench at the racks: things Gohan works on, not marks in the street.</summary>
        private void SpawnWorkProps()
        {
            var panelModel = new Model("prop_ld_case_01");
            if (GameUtils.RequestModel(panelModel))
            {
                _feederPanel = Track(World.CreateProp(panelModel, _feeder + new Vector3(0.7f, 0.4f, 0f), false, true));
                panelModel.MarkAsNoLongerNeeded();
                if (_feederPanel != null && _feederPanel.Exists()) { _feederPanel.IsPersistent = true; _feederPanel.IsPositionFrozen = true; }
            }
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
            Logger.Info("M06 work points: feeder panel at " + _feeder + ", backup bench at " + _racks + ".");
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
            _roles?.Update();
            foreach (var insertion in _insertions) insertion.Update();
            _insertions.RemoveAll(insertion => insertion.Current == HeliInsertion.Phase.Done);
        }

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
            int count = 3 + wave;
            int byAir = wave >= 2 ? Math.Min(count, 2 * HeliInsertion.Capacity) : 0;

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
                else trooper.Task.FightAgainstHatedTargets(90f);

                spawned.Add(Track(trooper));
                _swat.Add(trooper);
            }
            model.MarkAsNoLongerNeeded();

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
        }
    }
}
