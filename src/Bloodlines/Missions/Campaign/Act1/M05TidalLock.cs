using System.Collections.Generic;
using System;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M05 — "Tidal Lock". Palomino Highlands shoreline, 03:30, storm.
    ///
    /// The Act I finale and the mission that turns the campaign: Mateo, run down
    /// on the water and taken aboard the dinghy, says the three contracts were
    /// bought to put the three of them on that dock. It is an allegation, and the
    /// mission records it as one; the proof is M46's.
    ///
    /// Seen, not told: the cove from the cliff and from the boat before anyone
    /// moves; Ice coming down to the water once Mateo is stopped, so the three
    /// voices of the questioning are three men in one place; Mateo pulled aboard
    /// and questioned in the dinghy; the crew leaving with him in the boat.
    ///
    /// Faked per docs/FEASIBILITY.md: there is no sea-cave interior, so the grotto is
    /// the water at the cave mouth, and the floodlights are the men working them —
    /// killing the generator crew reads the same as shooting out the lamps and does
    /// not need a destructible prop the engine will not give us.
    /// </summary>
    public sealed class M05TidalLock : ComposedMission
    {
        private readonly List<Ped> _lightCrew = new List<Ped>();

        private Ped _mateo;
        private Vehicle _mateoBoat;
        private Vehicle _dinghy;
        private RoleTracks _roles;
        private Vector3 _perch;
        private Vector3 _cove;
        private Vector3 _grotto;
        private Vector3 _sandbar;
        private Vector3 _shore;
        private bool _iceDown;

        public override string Id => "M05";
        public override string Title => "Tidal Lock";

        public Ped Mateo => _mateo;
        public Vehicle Dinghy => _dinghy;
        public RoleTracks Roles => _roles;
        public bool IceDown => _iceDown;

        protected override bool Setup()
        {
            if (!MissionSites.Ground(Ctx.Locations, "M05.CliffPerch", "M05.LightCrew")) return false;
            if (!MissionSites.Water(Ctx.Locations, "M05.CoveAir", "M05.GrottoMouth", "M05.Sandbar", "M05.DinghySpawn")) return false;
            _perch = Ctx.Locations.Position("M05.CliffPerch");
            _cove = Ctx.Locations.Position("M05.CoveAir");
            _grotto = Ctx.Locations.Position("M05.GrottoMouth");
            _sandbar = Ctx.Locations.Position("M05.Sandbar");
            // Where Ice comes down to: the nearest ground the navmesh accepts on the
            // cliff side of the sandbar, or the perch itself if the shore refuses.
            _shore = World.GetSafeCoordForPed(_sandbar + new Vector3(-30f, 30f, 2f), false, 0);
            if (_shore == Vector3.Zero) _shore = _perch;

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _perch, Ctx.Locations.Heading("M05.CliffPerch"))) return false;

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.SniperRifle, 60, true, true);

            SpawnLightCrew();
            SpawnMateo();
            SpawnDinghy();
            if (_dinghy == null || !_dinghy.Exists() || _mateo == null || !_mateo.Exists() || _mateoBoat == null || !_mateoBoat.Exists() || _lightCrew.Count != 4) return false;
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.TakeControl(hero.Slot);
            // Ron and Gohan in the dinghy, checked: a seat that did not take left Ron
            // standing by the cliff (Ron, September 11).
            Board(CrewSlot.Guess, VehicleSeat.Driver);
            Board(CrewSlot.Gohan, VehicleSeat.RightFront);
            Function.Call(Hash.REQUEST_WEAPON_ASSET, (uint)WeaponHash.FlareGun, 31, 0);
            _mateo.IsInvincible = true;
            _roles = new RoleTracks(Ctx.Crew, () => _lightCrew);
            PlayShore();
            return true;
        }

        private void Board(CrewSlot slot, VehicleSeat seat)
        {
            var ped = Ctx.Crew.PedFor(slot);
            if (ped == null || !ped.Exists() || _dinghy == null || !_dinghy.Exists()) return;
            ped.Task.ClearAllImmediately();
            ped.SetIntoVehicle(_dinghy, seat);
            if (ped.IsInVehicle(_dinghy)) return;
            ped.Task.WarpIntoVehicle(_dinghy, seat);
            Logger.Warn("M05: " + Protagonist.Of(slot).Handle + " did not take the dinghy's " + seat + " seat on the first try; warped in." + (ped.IsInVehicle(_dinghy) ? "" : " Still not aboard."));
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Cliff overwatch",
                    new KillTargetsObjective("Ice — take the generator crew off the cave mouth.",
                        () => _lightCrew))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context => Say("M05_S1_01_ICE"));

            yield return new MissionStage("Light the cove",
                    new MissionInteraction("Guess: launch the signal flare from the dinghy", () => _cove, 1, 45f, () => _dinghy))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => context.Crew.CompanionAI.ReleaseControl(CrewSlot.Guess))
                .OnExit(context =>
                {
                    // Launch a visible flare after the explicit interaction.
                    var point = _dinghy.Position;
                    Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS, point.X, point.Y, point.Z + 2f, point.X, point.Y, point.Z + 70f, 0, true, (uint)WeaponHash.FlareGun, context.Crew.PedFor(CrewSlot.Guess), true, false, 35f);
                    Say("M05_S1_02_GUESS");
                    GameUtils.Subtitle("~y~Flare away. Follow the yellow cove marker.", 4000);
                });

            yield return new MissionStage("Breach the grotto",
                    new OccupiedVehicleDestination("Gohan: stay in the dinghy. Let Guess drive to the yellow cove marker, or switch back to drive, then return to Gohan.", () => _dinghy, () => _grotto, 25f))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context =>
                {
                    Say("M05_S1_03_GOHAN");
                    if (_mateo != null && _mateo.Exists() && _mateoBoat != null && _mateoBoat.Exists())
                    {
                        _mateo.Task.StartBoatMission(_mateoBoat, _sandbar, VehicleMissionType.GoTo, 12f, (VehicleDrivingFlags)786603, 12f, (BoatMissionFlags)7);
                    }
                });

            yield return new MissionStage("Run him to the sandbar",
                    new CaptureBoatObjective(() => _mateo, () => _mateoBoat, () => _dinghy))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => MateoStopped());

            // The questioning happens in the dinghy, not shouted across water: bring
            // it alongside and take him aboard. That is the stage's whole objective.
            yield return new MissionStage("Take him aboard",
                    new MissionInteraction("Gohan: bring the dinghy alongside and take Mateo aboard", () => MateoPosition(), 2, 20f, () => _dinghy))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => PlayAccount());

            yield return new MissionStage("Mateo's account", new DialogueFinishedObjective("Hold the dinghy. Mateo is aboard."))
                .OnExit(context =>
                {
                    // Act I ends on information, not a kill: an allegation the crew
                    // now holds and cannot yet prove. The proof is M46's.
                    Ctx.State?.SetEvidence("mateoAllegation", EvidenceState.Alleged);
                    if (_mateo != null && _mateo.Exists() && _dinghy != null && _dinghy.Exists() && !_mateo.IsInVehicle(_dinghy)) _mateo.SetIntoVehicle(_dinghy, VehicleSeat.LeftRear);
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    GameUtils.Subtitle("~y~Aegis built this. All of it. Mateo's word is a lead, not proof.", 6000);
                });
        }

        // ---------- beats ----------

        /// <summary>The cove before anyone moves: from the perch, from the dinghy, his boat at the cave mouth.</summary>
        private void PlayShore()
        {
            var blocking = new SceneBlocking()
                .Then(new ShotStep(3200, null, _perch + new Vector3(2f, 2f, 1.6f), null, _grotto + new Vector3(0f, 0f, 1f), 0f))
                .Then(new ShotStep(3200, _dinghy, new Vector3(-6f, 2.5f, 1.8f), _dinghy, new Vector3(0f, 0f, 0.8f), 1.0f))
                .Then(new ShotStep(3000, _mateoBoat, new Vector3(-10f, 4f, 2.5f), _mateoBoat, new Vector3(0f, 0f, 0.8f), 1.5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "shore", Title = "The cove",
                Reason = "The recovered location is real: Mateo's boat at the cave mouth under the lamps, Ice above on the cliff, Ron and Gohan in the dinghy below.",
                Blocking = blocking
            }.With("MATEO", _mateo);
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M05 shore scene did not play; the overwatch stands on its own.");
        }

        /// <summary>Mateo is stopped on the water. Ice comes down off the cliff to the shore so the three of them are in one place for what he says.</summary>
        private void MateoStopped()
        {
            if (_mateo != null && _mateo.Exists()) _mateo.Task.ClearAll();
            if (_mateoBoat != null && _mateoBoat.Exists()) { _mateoBoat.IsEngineRunning = false; Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, _mateoBoat, 0f); }
            _iceDown = true;
            _roles.For(CrewSlot.Ice).Extract(_shore);
            Radio("ICE", "He's stopped. I'm coming down to the water. Keep him breathing until I'm there.", "M05_RADIO_01_ICE");
        }

        /// <summary>
        /// The questioning, staged from the bible's own stage-two lines: Mateo is
        /// pulled aboard the dinghy, and says it over Gohan's shoulder with Ice
        /// on the shore. Skipping still leaves him in the boat.
        /// </summary>
        private void PlayAccount()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (_mateo == null || !_mateo.Exists() || _dinghy == null || !_dinghy.Exists()) return;
            Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess);
            if (guess != null && guess.Exists()) guess.Task.ClearAll();
            _dinghy.IsEngineRunning = false;
            Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, _dinghy, 0f);
            var blocking = new SceneBlocking { DialogueAfterStep = 1 }
                .Then(new EnterVehicleStep(_mateo, _dinghy, VehicleSeat.LeftRear) { TimeoutMs = 9000 })
                .Then(gohan != null && gohan.Exists() ? ShotStep.OverShoulder(5000, gohan, _mateo, 0.3f) : (SceneStep)new ShotStep(5000, _dinghy, new Vector3(-3f, 2f, 1.6f), _mateo, new Vector3(0f, 0f, 0.7f)))
                .Then(new ShotStep(4500, _dinghy, new Vector3(-7f, 3f, 2f), _dinghy, new Vector3(0f, 0f, 0.9f), 1.2f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "account", Title = "Mateo's account",
                Reason = "Mateo, aboard the dinghy and alive, says the three contracts were bought; Gohan separates what he said from what the data confirms; Ice says what comes next.",
                Blocking = blocking
            }.With("MATEO", _mateo);
            if (!Ctx.Cutscenes.PlayStaged(spec, Ctx.Data?.Stage(Id, 2)))
            {
                Logger.Warn("M05 account scene did not play; the lines play as dialogue and Mateo is put aboard.");
                blocking.Complete();
                SayStage(2);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            _roles?.Update();
        }

        /// <summary>The aftermath: the dinghy with the four of them in it, leaving. Custody is seen.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_dinghy == null || !_dinghy.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _dinghy, new Vector3(-8f, 3f, 2.2f), _dinghy, new Vector3(0f, 0f, 0.8f), 1.5f));
        }

        private Vector3 MateoPosition()
        {
            return _mateo != null && _mateo.Exists() ? _mateo.Position : _sandbar;
        }

        /// <summary>
        /// The generator crew works the lamps on the beach below the cliff: four
        /// posts spread across Ron's surveyed beach key (September 12: the crew had
        /// been standing inside the mountain), each snapped to walkable ground.
        /// </summary>
        private Vector3 LightCrewPost(int index)
        {
            var beach = Ctx.Locations.Position("M05.LightCrew");
            var wanted = beach + new Vector3(index * 3.5f - 5.25f, (index % 2) * 2.5f, 0f);
            var safe = World.GetSafeCoordForPed(wanted, false, 0);
            if (safe != Vector3.Zero && GameUtils.IsWithinFlat(safe, beach, 20f)) return safe;
            return wanted;
        }

        private void SpawnLightCrew()
        {
            var model = new Model("g_m_y_mexgoon_02");
            if (!GameUtils.RequestModel(model)) return;

            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < 4; i++)
            {
                var post = LightCrewPost(i);
                var guard = World.CreatePed(model, post, DriveUpStep.HeadingBetween(post, _grotto));
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = cartel;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 30;
                guard.Weapons.Give(WeaponHash.MicroSMG, 150, true, true);
                guard.Task.GuardCurrentPosition();

                _lightCrew.Add(Track(guard));
            }
            if (_lightCrew.Count > 0) Logger.Info("M05 generator crew at the cave mouth: first post " + _lightCrew[0].Position + " (grotto " + _grotto + ").");

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnMateo()
        {
            var model = new Model("g_m_m_mexboss_01");
            var boatModel = new Model("tropic");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(boatModel)) return;

            // His boat sits at the key itself: Ron's surveyed spot off the beach, where the two boats moor.
            float boatHeading = Ctx.Locations.Heading("M05.GrottoMouth");
            _mateoBoat = Track(World.CreateVehicle(boatModel, _grotto, boatHeading));
            _mateo = Track(World.CreatePed(model, _grotto + new Vector3(2f, 0f, 0f), boatHeading));
            model.MarkAsNoLongerNeeded();
            boatModel.MarkAsNoLongerNeeded();

            if (_mateo == null || !_mateo.Exists()) return;

            _mateo.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");
            _mateo.IsPersistent = true;
            _mateo.BlockPermanentEvents = true;
            _mateo.Armor = 100;
            _mateo.Weapons.Give(WeaponHash.APPistol, 100, true, true);

            var blip = Track(_mateo.AddBlip());
            blip.Sprite = BlipSprite.Enemy;
            blip.Color = BlipColor.Red;
            blip.Name = "Mateo Cifuentes";

            if (_mateoBoat != null && _mateoBoat.Exists())
            {
                _mateoBoat.IsPersistent = true;
                _mateo.Task.WarpIntoVehicle(_mateoBoat, VehicleSeat.Driver);
            }
        }

        private void SpawnDinghy()
        {
            var model = new Model("dinghy");
            if (!GameUtils.RequestModel(model)) return;

            _dinghy = Track(World.CreateVehicle(model, Ctx.Locations.Position("M05.DinghySpawn"), Ctx.Locations.Heading("M05.DinghySpawn")));
            model.MarkAsNoLongerNeeded();
            if (_dinghy == null || !_dinghy.Exists()) return;

            _dinghy.IsPersistent = true;
            Logger.Info("M05 dinghy on the water at " + _dinghy.Position + ".");

            var blip = Track(_dinghy.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Stealth dinghy";
        }

        protected override void OnCleanup()
        {
            _roles?.Release();
            if (_mateo != null && _mateo.Exists()) { _mateo.IsInvincible=false; Release(_mateo); }
            if (_mateoBoat != null && _mateoBoat.Exists()) Release(_mateoBoat);
            Function.Call(Hash.REMOVE_WEAPON_ASSET,(uint)WeaponHash.FlareGun);
            _lightCrew.Clear();
        }
    }
}
