using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

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
    /// This is the mission the wave objective was written for: Ice's job is not to
    /// clear the alley, it is to hold it for as long as the thermite takes. Two
    /// objectives running at once in the same stage — one a burn timer, one a siege.
    /// </summary>
    public sealed class M06CleanSweep : ComposedMission
    {
        private readonly List<Ped> _swat = new List<Ped>();
        private readonly List<HeliInsertion> _insertions = new List<HeliInsertion>();
        private bool _airMomentPlayed;

        private Vehicle _granger;
        private Vector3 _feeder;
        private Vector3 _sallyPort;
        private Vector3 _racks;
        private Vector3 _alley;

        public override string Id => "M06";
        public override string Title => "Clean Sweep";

        protected override bool Setup()
        {
            if (!MissionSites.Ground(Ctx.Locations, "M06.Culvert", "M06.Feeder", "M06.SallyPort", "M06.ServerRacks", "M06.AlleyHold", "M06.GrangerSpawn")) return false;
            _feeder = Ctx.Locations.Position("M06.Feeder");
            _sallyPort = Ctx.Locations.Position("M06.SallyPort");
            _racks = Ctx.Locations.Position("M06.ServerRacks");
            _alley = Ctx.Locations.Position("M06.AlleyHold");

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, new Dictionary<CrewSlot, PedPlacement>
            {
                [CrewSlot.Gohan] = new PedPlacement(Ctx.Locations.Position("M06.Culvert"), 0f),
                [CrewSlot.Ice] = new PedPlacement(_alley, Ctx.Locations.Heading("M06.AlleyHold")),
                [CrewSlot.Guess] = new PedPlacement(Ctx.Locations.Position("M06.GrangerSpawn"), Ctx.Locations.Heading("M06.GrangerSpawn"))
            })) return false;
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.TakeControl(hero.Slot);
            Ctx.Crew.PedFor(CrewSlot.Ice).Task.GuardCurrentPosition();

            ApplyBibleSetting();
            SpawnGranger();
            if (_granger == null || !_granger.Exists()) return false;
            Ctx.Crew.PedFor(CrewSlot.Guess).SetIntoVehicle(_granger, VehicleSeat.Driver);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Cut the power",
                    new MissionInteraction("Gohan: cut the marked power feeder", () => _feeder, 6, 3f))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => Say("M06_S1_01_GOHAN"));

            yield return new MissionStage("Sally port",
                    new ReachZoneObjective("Ice: walk into the yellow depot ENTRANCE marker. No ability or button is needed.", () => _sallyPort, 5f))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context => Say("M06_S1_02_ICE"));

            // The burn and the siege run together: the thermite does not care how the
            // alley is going, and the alley does not stop when the racks are slag.
            yield return new MissionStage("Burn the racks",
                    new AssignedWorkObjective("Gohan is preparing the thermite. Ice: hold the alley while he works.", CrewSlot.Gohan, () => _racks, 20),
                    new SurviveWavesObjective("Ice: defeat the RED-marked SWAT waves while Gohan finishes the burn. Stay on Ice.", SpawnSwatWave, 3, 6000))
                .OwnedBy(CrewSlot.Ice)
                
                .OnEnter(context => { Say("M06_S2_03_ICE"); Game.Player.WantedLevel = 3; context.Crew.CompanionAI.ReleaseControl(CrewSlot.Ice); context.Crew.CompanionsHoldPosition = true; })
                .OnExit(context => { Say("M06_S2_04_GOHAN"); GameUtils.Subtitle("~g~Core is slag. Return to the Granger.", 4000); });

            yield return new MissionStage("Reverse extraction",
                    new EnterVehicleObjective("Switch to Guess in the Granger and wait for Ice and Gohan to board.", () => _granger, VehicleSeat.Driver, requireCrew: true))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context =>
                {
                    Say("M06_S2_05_GUESS");
                    context.Crew.CompanionAI.ReleaseAll();
                    context.Crew.CompanionsHoldPosition = false;
                    context.Crew.CompanionAI.RequireSharedVehicle = true;
                    if (_granger != null && _granger.Exists()) _granger.IsEngineRunning = true;
                    context.Crew.AssignCompanionAI();
                });

            yield return new MissionStage("Out of Vespucci",
                    new LoseWantedObjective("Lose the police."),
                    new ProtectObjective("", () => _granger, "The Granger was destroyed."))
                .OnExit(context => GameUtils.Subtitle("~g~Depot clean. Nothing left to match a face to.", 5000));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            foreach (var insertion in _insertions) insertion.Update();
            _insertions.RemoveAll(insertion => insertion.Current == HeliInsertion.Phase.Done);
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
                var trooper = World.CreatePed(model, _alley + offset, 180f);
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
            var model = new Model("granger");
            if (!GameUtils.RequestModel(model)) return;

            _granger = Track(World.CreateVehicle(model, Ctx.Locations.Position("M06.GrangerSpawn"),
                Ctx.Locations.Heading("M06.GrangerSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_granger == null || !_granger.Exists()) return;

            _granger.IsPersistent = true;
            _granger.IsEngineRunning = false;

            var blip = Track(_granger.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Armored Granger";
        }

        protected override void OnCleanup()
        {
            _swat.Clear();
            _insertions.Clear();
        }
    }
}
