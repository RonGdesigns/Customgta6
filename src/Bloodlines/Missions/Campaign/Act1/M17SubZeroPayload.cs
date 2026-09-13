using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M17 — "Sub-Zero Payload". Elysian dry docks, 13:00.
    ///
    /// The second quiet mission. Gohan and Guess weld plasma-arc torches and magnetic
    /// ballast grapples onto the crew's sub in a hidden slip — the tool that opens
    /// hold 3 of the Titan Star in M19.
    ///
    /// Seen, not told: the sub from the Berth 44 survey, hauled to the slip, with
    /// Ron looking it over and Ice at the pickup gear before anyone works; each
    /// weld point leaving a real part on the hull, so three sites are three parts;
    /// the external release as an object, changed by Gohan where either brother can
    /// reach it, after Ron asks; a test and a radio check. The next chapter gets a
    /// ready sub, recorded, not a new one.
    ///
    /// Nobody shoots at anybody. Act I is a heist build-up, and a build-up that is
    /// all firefights has no build in it.
    /// </summary>
    public sealed class M17SubZeroPayload : ComposedMission
    {
        private readonly List<Vector3> _weldPoints = new List<Vector3>();
        private readonly List<Prop> _modules = new List<Prop>();
        private readonly List<Prop> _parts = new List<Prop>();

        private Vehicle _kraken;
        private Prop _release;
        private Prop _gear;
        private Vector3 _slip;
        private bool _releaseAsked, _releaseChanged;

        public override string Id => "M17";
        public override string Title => "Sub-Zero Payload";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        public Vehicle Kraken => _kraken;
        public IReadOnlyList<Prop> Parts => _parts;
        public Prop ReleaseHandle => _release;
        public bool ReleaseAsked => _releaseAsked;
        public bool ReleaseChanged => _releaseChanged;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _slip = Ctx.Locations.Position("M17.DrySlip");

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, Ctx.Locations.Position("M17.GohanStart"), Ctx.Locations.Heading("M17.DrySlip")))
            {
                return false;
            }

            ApplyBibleSetting();
            Ctx.Crew.CompanionsHoldPosition = true;
            SpawnKraken();
            SpawnGear();
            if (!RequireAssets(_kraken)) return false;
            foreach (var key in new[] { "M17.WeldOne", "M17.WeldTwo", "M17.WeldThree" })
                _weldPoints.Add(Ctx.Locations.Position(key));
            if (!SpawnWorkstations()) return false;
            Station(CrewSlot.Guess, Ctx.Locations.Position("M17.GuessStart"));
            Station(CrewSlot.Ice, Ctx.Locations.Position("M17.IceStart"));
            GameUtils.Notify("Kraken moored in the water. Prepare its modules on the dock. The release is an emergency grapple control, not a launch crane.");
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            var welds = new MultiHoldObjective("Gohan: prepare the three torch modules at the dock workbenches.",
                _weldPoints, 8, 3f, "Welding") { SiteDone = FitPart, Animation = MissionInteraction.ReachInside };
            yield return new MissionStage("Calibrate the torches", welds)
                .OwnedBy(CrewSlot.Gohan)
                .AfterCues("M17_S1_01_GOHAN");

            yield return new MissionStage("Grapple test",
                    new MissionInteraction("Guess — test the fifty-ton magnetic lock.", () => _slip, 10, 1.7f, animation: MissionInteraction.ReachInside, face: () => _slip + new Vector3(-1.5f, 0f, 0f)),
                    new ReactionTrigger(() => !_releaseAsked && !Ctx.Cutscenes.IsActive, AskForRelease))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => GameUtils.Subtitle("~g~Four minutes through eight inches of naval bulkhead.", 5000))
                .AfterCues("M17_S1_02_GUESS");

            // Ron's question becomes an object: a release on the outside, where either
            // brother can reach it. Gohan changes it; the change is seen.
            yield return new MissionStage("The release",
                    new MissionInteraction("Gohan: configure the external grapple release at the marked workbench.", () => ReleasePoint(), 5, 1.7f, animation: MissionInteraction.ReachInside, face: () => ReleasePoint() + new Vector3(-1.5f, 0f, 0f)))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => PlayRelease());

            yield return new MissionStage("Collect the Kraken",
                    new EnterVehicleObjective("Gohan: collect the prepared Kraken from the water beside the dock.", () => _kraken, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Gohan)
                .OnEnter(context => { _kraken.IsPositionFrozen = false; GameUtils.Subtitle("Follow the green sub marker. Board the Kraken with F / Y or E / D-pad Right beside the hatch.", 6000); });

            yield return new MissionStage("Ready",
                    new DialogueFinishedObjective("Finish the radio check before staging the heist."))
                .OnExit(context =>
                {
                    Ctx.State?.SetCargo("kraken", "M17.DrySlip");
                    GameUtils.Subtitle("~y~The sub is ready, with a release either of them can pull. Tomorrow night, Berth 44.", 6000);
                })
                .WithCues("M17_S1_03_ICE");
        }

        // ---------- beats ----------

        /// <summary>The sub in the slip, Ron looking it over, Ice at the pickup gear: the same craft from the survey, hauled here.</summary>
        private void PlayApproach()
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking();
            if (_kraken != null && _kraken.Exists()) blocking.Then(new ShotStep(3400, _kraken, new Vector3(-7f, 4f, 2.2f), _kraken, new Vector3(0f, 0f, 0.5f), 1.0f));
            if (guess != null && guess.Exists() && _kraken != null && _kraken.Exists()) blocking.Then(new InspectStep(guess, _kraken.Position + new Vector3(-2.5f, 0f, 0f), 3000));
            if (ice != null && ice.Exists() && _gear != null && _gear.Exists()) blocking.Then(ShotStep.Watching(2800, ice, _gear));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The slip",
                Reason = "The survey Kraken is moored beside the quay workshop. Gohan prepares three torch modules at the benches, Guess tests the magnetic lock, and Gohan adds its external emergency release. The sub is already afloat; the release is not a launch mechanism.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M17 approach scene did not play; the slip stands on its own.");
        }

        /// <summary>A weld point done is a part on the hull: three sites, three parts, seen.</summary>
        private void FitPart(int site)
        {
            if (_kraken == null || !_kraken.Exists() || site < 0 || site >= _weldPoints.Count) return;
            var part = _modules[site];
            if (part == null || !part.Exists()) { Fail("A torch module is missing. Restart the workshop."); return; }
            part.IsPositionFrozen = false;
            GTA.Native.Function.Call(GTA.Native.Hash.SET_ENTITY_COLLISION, part, false, false);
            StowPropStep.Stow(part, _kraken, new Vector3(-1.2f + site * 1.2f, -1.6f, .9f));
            if (!GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_ENTITY_ATTACHED_TO_ENTITY, part, _kraken))
            { Fail("A torch mount did not attach. Restart the workshop."); return; }
            _parts.Add(part);
            GameUtils.Subtitle("~g~Part " + _parts.Count + " of 3 on the hull.", 3000);
        }

        /// <summary>Ron notices the release: it is inside, where only the pilot can reach it.</summary>
        private void AskForRelease()
        {
            _releaseAsked = true;
            Radio("GUESS", "Where's the release, Gohan? Inside, by your seat. Put one on the outside where either of us can reach it.", "M17_RADIO_01_GUESS");
        }

        /// <summary>The external release, changed: a handle on the hull, tested, from the bible's own lines.</summary>
        private void PlayRelease()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            SpawnRelease();
            if (_release == null || !_release.Exists() || !GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_ENTITY_ATTACHED_TO_ENTITY, _release, _kraken))
            { Fail("The external release control did not attach. Restart the workshop."); return; }
            _releaseChanged = true;
            var blocking = new SceneBlocking();
            if (gohan != null && gohan.Exists())
            {
                blocking.Then(new InspectStep(gohan, ReleasePoint() + new Vector3(-1f, 0f, 1f), 1800));
                blocking.Then(ShotStep.OverShoulder(3400, gohan, _release != null && _release.Exists() ? (Entity)_release : gohan, 0.2f));
            }
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "release", Title = "The release",
                Reason = "Gohan moves the release to the outside of the hull: a release control either brother can operate. He learned to work alone; the handle says he does not mean to die that way.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) { Logger.Warn("M17 release scene did not play; the handle is fitted directly."); blocking.Complete(); }
            GameUtils.Subtitle("~g~External release fitted and tested: a control on the hull, either of them can reach it.", 5000);
        }

        /// <summary>The aftermath: the sub in the slip with its parts and its handle.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_kraken == null || !_kraken.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _kraken, new Vector3(-6f, -4f, 2f), _kraken, new Vector3(0f, 0f, 0.5f), 0.8f));
        }

        // ---------- world building ----------

        private Vector3 ReleasePoint() => Ctx.Locations.Position("M17.ReleasePoint");

        private bool SpawnWorkstations()
        {
            var benchModel = new Model("prop_tool_bench02");
            var itemModel = new Model("prop_tool_blowtorch");
            if (!GameUtils.RequestModel(benchModel) || !GameUtils.RequestModel(itemModel)) return false;
            int index = 0;
            foreach (var point in new[] { _weldPoints[0], _weldPoints[1], _weldPoints[2], ReleasePoint() })
            {
                var bench = Track(World.CreateProp(benchModel, MissionPlacement.Position(Ctx.Locations, "M17.Workbench" + (++index), point + new Vector3(-1.5f, 0f, 0f)), false, true));
                if (!RequireAssets(bench)) return false;
                bench.Heading = MissionPlacement.Heading(Ctx.Locations, "M17.Workbench" + index, 0f); bench.IsPositionFrozen = true;
                var workModel = index == 4 ? new Model("prop_cs_remote_01") : itemModel;
                if (!GameUtils.RequestModel(workModel)) return false;
                var item = Track(World.CreateProp(workModel, PropPlacement.OnTop(bench, benchModel, workModel), false, false));
                workModel.MarkAsNoLongerNeeded();
                if (!RequireAssets(item)) return false;
                item.IsPositionFrozen = true;
                _modules.Add(item);
            }
            benchModel.MarkAsNoLongerNeeded(); itemModel.MarkAsNoLongerNeeded();
            return true;
        }

        private void SpawnKraken()
        {
            var model = new Model("submersible2");
            if (!GameUtils.RequestModel(model)) return;

            _kraken = Track(World.CreateVehicle(model, MarineSites.ResolveOrThrow(Ctx.Locations, "M17.KrakenSpawn", 5f, 3f, 5f),
                Ctx.Locations.Heading("M17.KrakenSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_kraken == null || !_kraken.Exists()) return;

            _kraken.IsPersistent = true;
            _kraken.IsPositionFrozen = true;

            var blip = Track(_kraken.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Kraken";
        }

        /// <summary>The pickup gear Ice checks: a crate at his station.</summary>
        private void SpawnGear()
        {
            var model = new Model("prop_box_wood02a");
            if (!GameUtils.RequestModel(model)) return;
            _gear = Track(World.CreateProp(model, Ctx.Locations.Position("M17.GearSpawn"), true, false));
            model.MarkAsNoLongerNeeded();
        }

        /// <summary>The handle on the outside of the hull.</summary>
        private void SpawnRelease()
        {
            if (_kraken == null || !_kraken.Exists()) return;
            _release = _modules.Count >= 4 ? _modules[3] : null;
            if (_release == null || !_release.Exists()) return;
            _release.IsPersistent = true;
            GTA.Native.Function.Call(GTA.Native.Hash.SET_ENTITY_COLLISION, _release, false, false);
            StowPropStep.Stow(_release, _kraken, new Vector3(0f, -3.2f, 0.7f));
        }

        protected override void OnPassed()
        {
            if (_kraken != null && _kraken.Exists()) Release(_kraken);
            foreach (var part in _parts) if (part != null && part.Exists()) Release(part);
            if (_release != null && _release.Exists()) Release(_release);
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            if (_kraken != null && _kraken.Exists()) _kraken.IsPositionFrozen = false;
            _parts.Clear(); _modules.Clear();
        }
    }
}
