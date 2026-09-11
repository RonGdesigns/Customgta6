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
    /// M04 — "Severed Wire". Pillbox Hill and Textile City, 22:00, thunderstorm.
    ///
    /// Detective Miller is selling the dry-dock forensics to an Aegis buyer in a
    /// surface lot. The crew drives in together, splits at the lot, and the player
    /// sees the sale before anyone touches it: Miller with the case, the buyer
    /// checking it, guards on the entrances, Ron's van nosed at the exit. Gohan cuts
    /// the breaker; that, not a body count, is what makes Miller grab the case and
    /// run. Ron's own AI is already on him while the player is offered the wheel.
    /// Ice keeps the escort off Gohan. Ron stops the car, takes the drive, and the
    /// crew loses the police together. Whether Miller lives is the player's; the
    /// mission asks for the drive.
    ///
    /// This is the reference for docs/STORY-TO-PLAY-PLAN.md: every piece here
    /// (travel with radio calls, a transaction scene with a support cast and authored
    /// shots, role tracks for the inactive brothers, the offered switch, evidence
    /// custody, an honest endpoint) is reusable by the missions that follow.
    /// </summary>
    public sealed class M04SeveredWire : ComposedMission
    {
        public const int SwitchWindowMs = 8000;

        private readonly List<Ped> _bodyguards = new List<Ped>();

        private Ped _miller, _buyer;
        private Vehicle _millerCar, _buyerCar, _van;
        private Prop _case;
        private RoleTracks _roles;
        private Vector3 _lot, _breaker, _meet, _exit, _gohanCover, _iceCover, _iceWatch, _gohanApproach;
        private bool _flightStarted, _hostile;

        public override string Id => "M04";
        public override string Title => "Severed Wire";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public Ped Miller => _miller;
        public Vehicle Van => _van;
        public Prop Case => _case;
        public RoleTracks Roles => _roles;
        public bool FlightStarted => _flightStarted;

        protected override bool Setup()
        {
            if (!MissionSites.Ground(Ctx.Locations, "M04.GarageEntry", "M04.Breaker", "M04.RampGuards", "M04.ChaseCar", "Base.CypressFlats")) return false;
            _lot = Ctx.Locations.Position("M04.GarageEntry");
            _breaker = Ctx.Locations.Position("M04.Breaker");
            _meet = Ctx.Locations.Position("M04.RampGuards");
            _exit = Ctx.Locations.Position("M04.ChaseCar");
            // Where the inactive brothers stand and where they fight from: derived
            // from the surveyed keys so a survey moves the whole set.
            _gohanApproach = _breaker + new Vector3(-4f, -4f, 0f);
            _gohanCover = _breaker + new Vector3(-9f, -7f, 0f);
            _iceWatch = _meet + new Vector3(-12f, 9f, 0f);
            _iceCover = _meet + new Vector3(-16f, 12f, 0f);

            // The briefing played at the base; the crew leaves from there in their own van.
            var basePoint = Ctx.Locations.Position("Base.CypressFlats");
            float baseHeading = Ctx.Locations.Heading("Base.CypressFlats");
            if (!Ctx.Crew.Deploy(CrewSlot.Guess, basePoint, baseHeading)) return false;

            ApplyBibleSetting();
            Ctx.Abilities.Refill();

            SpawnVan(basePoint + new Vector3(6f, 0f, 0f), baseHeading);
            SpawnMiller();
            SpawnBuyer();
            SpawnBodyguards();
            if (!RequireAssets(_van, _miller, _millerCar, _case) || _bodyguards.Count != 6) return false;
            RequireAsset(_van, "The crew's van was lost. Restart this mission.");
            Preserve(_van);
            Preserve(_millerCar);

            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.TakeControl(hero.Slot);
            Station(CrewSlot.Guess, _van, VehicleSeat.Driver);
            // Ron pulls up alone. Ice and Gohan are already on the lot, in their
            // positions, and were never seen arriving (Ron, September 10).
            Station(CrewSlot.Ice, _iceWatch);
            Station(CrewSlot.Gohan, _gohanApproach);
            // Guards are a threat to the brothers only once the lights go out; before that they are a meeting, not a fight.
            _roles = new RoleTracks(Ctx.Crew, () => _hostile ? (IEnumerable<Ped>)_bodyguards : Enumerable.Empty<Ped>());
            _roles.For(CrewSlot.Ice).Observe(_iceWatch, _iceCover);
            _roles.For(CrewSlot.Gohan).Observe(_gohanApproach, _gohanCover);
            _miller.IsInvincible = true;
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Drive to the lot",
                    new TravelObjective("Guess: drive the crew to the Pillbox Hill lot.", () => _exit, 12f, () => _van)
                        .Cue(0.6f, () => Radio("GOHAN", "I'm on the breaker already, east side of the lot, outside the fence. Nobody has looked at me twice.", "M04_RADIO_01_GOHAN"))
                        .Cue(0.3f, () => Radio("ICE", "I've got the ramp side and the meeting. Ron, nose the van at the exit and keep it running.", "M04_RADIO_02_ICE")))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => DropOff());

            yield return new MissionStage("In position",
                    new WaitForRolesObjective("Hold at the exit. Ice and Gohan are in position.", _roles, RoleState.Observing, 25000, CrewSlot.Ice, CrewSlot.Gohan))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => Say("M04_S1_01_GOHAN"));

            yield return new MissionStage("Kill the lights",
                    new MissionInteraction("Gohan: cut the marked surface-lot breaker", () => _breaker, 5, 3f))
                .OwnedBy(CrewSlot.Gohan)
                .OnEnter(context => PlayTransaction())
                .OnExit(context => LightsOut());

            yield return new MissionStage("Miller runs",
                    new SwitchWindowObjective("Miller is running. Take Guess to intercept; Ron is already on him", CrewSlot.Guess, SwitchWindowMs,
                        shadow: ShadowMiller, onSwitched: () => Say("M04_S2_03_GUESS")))
                .OnEnter(context => StartFlight());

            yield return new MissionStage("Run him down",
                    new PursueTargetObjective("Guess: chase the red marker. Disable Miller's car or stop Miller, then collect his drive.", () => _miller,
                        "Miller reached his handler and the forensics went with him."))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => { var guess = Ctx.Crew.PedFor(CrewSlot.Guess); if (guess != null && guess.Exists() && guess.Handle == Game.Player.Character.Handle) guess.Task.ClearAll(); Say("M04_S2_04_ICE"); });

            yield return new MissionStage("Recover the drive",
                    new MissionInteraction("Guess: collect Miller's drive", () => MillerPosition(), 3, 6f, animation: MissionInteraction.ReachInside))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => DriveRecovered());

            yield return new MissionStage("Lose them and regroup",
                    new LoseWantedObjective("Lose the police."),
                    new EnterVehicleObjective("Pick up Ice and Gohan in the van.", () => _van, VehicleSeat.Driver, requireCrew: true))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => Regroup())
                .OnExit(context => GameUtils.Subtitle("~g~Drive secured. Gohan's reading it on the way back.", 5000));
        }

        // ---------- beats ----------

        private void DropOff()
        {
            // Nobody gets out of the van: the others were never in it.
            _roles.For(CrewSlot.Guess).Observe(_exit, _exit);
            Objective("Hold at the exit. Ice and Gohan are in position.");
        }

        /// <summary>The sale is real: seen before anyone touches it, from the lot, from the case, from the exit.</summary>
        private void PlayTransaction()
        {
            if (_miller == null || !_miller.Exists()) return;
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var blocking = new SceneBlocking { DialogueAfterStep = 1 }
                .Then(ShotStep.Wide(3500, _meet, 16f, 9f, 6f))
                .Then(ShotStep.OverShoulder(4200, _miller, _buyer, 0.5f))
                .Then(new InspectStep(_buyer, _miller.Position, 3800))
                .Then(new ShotStep(3200, _van, new Vector3(-7f, 2.5f, 1.4f), _van, new Vector3(0f, 0f, 0.9f), 1.0f))
                .Then(ice != null ? ShotStep.Watching(2600, ice, _miller) : (SceneStep)new WaitStep(2600));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "transaction", Title = "The exchange",
                Reason = "Miller has the case, the buyer is checking it, guards hold the entrances, Ron is at the exit, Ice and Gohan are in place.",
                Blocking = blocking
            }.With("MILLER", _miller).With("BUYER", _buyer);
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M04 transaction scene did not play; the breaker objective stands on its own.");
            _roles.For(CrewSlot.Gohan).Work(_gohanApproach, _gohanCover);
        }

        /// <summary>Power cut: the buyer ducks, the escort covers Miller, Miller grabs the case and goes for his car.</summary>
        private void LightsOut()
        {
            Say("M04_S1_02_ICE");
            _hostile = true;
            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            foreach (var guard in _bodyguards)
                if (guard != null && guard.Exists() && !guard.IsDead) { guard.RelationshipGroup = aegis; guard.Task.FightAgainstHatedTargets(60f); }
            if (_buyer != null && _buyer.Exists() && !_buyer.IsDead) _buyer.Task.FleeFrom(Ctx.Crew.PedFor(CrewSlot.Gohan) ?? Game.Player.Character);
            if (_miller != null && _miller.Exists() && _millerCar != null && _millerCar.Exists())
            {
                _miller.Task.ClearAll();
                _miller.Task.EnterVehicle(_millerCar, VehicleSeat.Driver, 8000, 2f, EnterVehicleFlags.None);
            }
            _roles.For(CrewSlot.Gohan).TakeCover(_gohanCover);
            _roles.For(CrewSlot.Ice).Observe(_iceWatch, _iceCover);
        }

        private void StartFlight()
        {
            if (_flightStarted || _miller == null || !_miller.Exists()) return;
            _flightStarted = true;
            _miller.IsInvincible = false;
            if (_millerCar != null && _millerCar.Exists() && !_miller.IsInVehicle(_millerCar)) _miller.SetIntoVehicle(_millerCar, VehicleSeat.Driver);
            var pursuer = Ctx.Crew.PedFor(CrewSlot.Guess) ?? Game.Player.Character;
            if (_millerCar != null && _millerCar.Exists())
            {
                _millerCar.IsEngineRunning = true;
                _miller.Task.StartVehicleMission(_millerCar, pursuer, VehicleMissionType.Flee, 40f,
                    VehicleDrivingFlags.DrivingModeAvoidVehiclesReckless | VehicleDrivingFlags.AllowGoingWrongWay | VehicleDrivingFlags.UseShortCutLinks, 8f, 40f, true);
                Function.Call(Hash.SET_PED_KEEP_TASK, _miller, true);
            }
            Ctx.Cutscenes.PlayMoment(Id, "Miller breaks cover", "GUESS", "He's pulling out with the case. I've got the wheel until you take it.", _miller);
        }

        /// <summary>Ron's own AI stays on Miller while the player decides whether to take the van.</summary>
        private void ShadowMiller()
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (guess == null || !guess.Exists() || _miller == null || !_miller.Exists()) return;
            if (guess.Handle == Game.Player.Character.Handle) return;
            if (_van != null && _van.Exists() && !guess.IsInVehicle(_van)) guess.SetIntoVehicle(_van, VehicleSeat.Driver);
            guess.Task.VehicleChase(_miller);
        }

        private Vector3 MillerPosition() => _miller != null && _miller.Exists() ? _miller.Position : _exit;

        private void DriveRecovered()
        {
            if (_case != null && _case.Exists()) { _case.Detach(); GameUtils.SafeDelete(_case); }
            _case = null;
            Ctx.State?.SetEvidence("millerDrive", EvidenceState.CopyHeld);
            Say("M04_S2_05_ICE");
            if (_miller != null && _miller.Exists() && !_miller.IsDead) _miller.Task.FleeFrom(Game.Player.Character);
            GameUtils.Subtitle("~g~Drive secured. Stored in the van.", 4000);
        }

        private void Regroup()
        {
            _roles.For(CrewSlot.Ice).Extract(_exit);
            _roles.For(CrewSlot.Gohan).Extract(_exit);
            _roles.Release();
            Ctx.Crew.CompanionAI.ReleaseAll();
            Ctx.Crew.CompanionsHoldPosition = false;
            Ctx.Crew.CompanionAI.RequireSharedVehicle = true;
            Ctx.Crew.AssignCompanionAI();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            _roles?.Update();
        }

        protected override void OnPassed()
        {
            // The van is the crew's ride home and Miller's car is part of the street now.
            if (_van != null && _van.Exists()) Release(_van);
            if (_millerCar != null && _millerCar.Exists()) Release(_millerCar);
        }

        protected override void OnCleanup()
        {
            _roles?.Release();
            _bodyguards.Clear();
        }

        // ---------- world building ----------

        private void SpawnVan(Vector3 position, float heading)
        {
            _van = Ctx.Vans != null ? Ctx.Vans.Spawn(position, heading) : null;
            if (_van == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                _van = World.CreateVehicle(model, position, heading);
                model.MarkAsNoLongerNeeded();
            }
            if (_van == null || !_van.Exists()) return;
            Track(_van);
            _van.IsPersistent = true;
            _van.IsEngineRunning = true;
            var blip = Track(_van.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Blue;
            blip.Name = "Crew van";
        }

        private void SpawnMiller()
        {
            var model = new Model("s_m_m_ciasec_01");
            if (!GameUtils.RequestModel(model)) return;
            var spot = _meet + new Vector3(2f, 0f, 0f);
            _miller = Track(World.CreatePed(model, spot, 270f));
            model.MarkAsNoLongerNeeded();
            if (_miller == null || !_miller.Exists()) return;
            _miller.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            _miller.IsPersistent = true;
            _miller.BlockPermanentEvents = true;
            _miller.Armor = 60;
            _miller.Weapons.Give(WeaponHash.Pistol, 60, true, true);
            _miller.Task.StandStill(-1);
            var blip = Track(_miller.AddBlip());
            blip.Sprite = BlipSprite.Enemy;
            blip.Color = BlipColor.Red;
            blip.Name = "Det. Miller";

            var carModel = new Model("fugitive");
            if (GameUtils.RequestModel(carModel))
            {
                _millerCar = Track(World.CreateVehicle(carModel, spot + new Vector3(6f, -3f, 0f), 180f));
                carModel.MarkAsNoLongerNeeded();
                if (_millerCar != null && _millerCar.Exists()) _millerCar.IsPersistent = true;
            }
            // A getaway driver, not a commuter.
            Function.Call(Hash.SET_DRIVER_ABILITY, _miller, 1f);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, _miller, 1f);

            var caseModel = new Model("prop_ld_case_01");
            if (GameUtils.RequestModel(caseModel))
            {
                _case = Track(World.CreateProp(caseModel, spot + new Vector3(0f, 0f, 1f), false, false));
                caseModel.MarkAsNoLongerNeeded();
                if (_case != null && _case.Exists()) CarryPropStep.Attach(_miller, _case, new Vector3(0.12f, 0.02f, -0.02f), new Vector3(0f, 90f, 0f));
            }
        }

        private void SpawnBuyer()
        {
            var model = new Model("a_m_m_business_01");
            if (!GameUtils.RequestModel(model)) return;
            _buyer = Track(World.CreatePed(model, _meet + new Vector3(-1f, 0f, 0f), 90f));
            model.MarkAsNoLongerNeeded();
            if (_buyer == null || !_buyer.Exists()) return;
            _buyer.IsPersistent = true;
            _buyer.BlockPermanentEvents = true;
            _buyer.Task.StandStill(-1);
            var carModel = new Model("baller2");
            if (GameUtils.RequestModel(carModel))
            {
                _buyerCar = Track(World.CreateVehicle(carModel, _meet + new Vector3(-8f, 3f, 0f), 0f));
                carModel.MarkAsNoLongerNeeded();
                if (_buyerCar != null && _buyerCar.Exists()) _buyerCar.IsPersistent = true;
            }
        }

        private void SpawnBodyguards()
        {
            var model = new Model("s_m_m_highsec_02");
            if (!GameUtils.RequestModel(model)) return;
            var quiet = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");
            for (int i = 0; i < 6; i++)
            {
                var guard = World.CreatePed(model, _meet + new Vector3(-6f + i * 2.5f, (i % 2) * 4f + 4f, 0f), 180f);
                if (guard == null || !guard.Exists()) continue;
                guard.RelationshipGroup = quiet;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 35;
                guard.Armor = 40;
                guard.Weapons.Give(WeaponHash.CarbineRifle, 150, true, true);
                guard.Task.GuardCurrentPosition();
                _bodyguards.Add(Track(guard));
            }
            model.MarkAsNoLongerNeeded();
        }
    }
}
