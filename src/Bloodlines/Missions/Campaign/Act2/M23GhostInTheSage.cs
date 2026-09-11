using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M23 — "Ghost in the Sage". Grand Senora radar facility, 08:00, desert dust.
    ///
    /// Act II opens with the crew homeless. A Cold War radar installation full of
    /// cartel squatters becomes the new base — three exterior storage bays for the heavy
    /// rigs, a generator room, and nobody within twenty miles.
    ///
    /// Structurally this is the mirror of M03: the mission that gives the act its
    /// home. Where M03 built the foundry with a crane and a hauler, this one takes a
    /// bunker off people who are already living in it.
    /// </summary>
    public sealed class M23GhostInTheSage : ComposedMission
    {
        private readonly List<Ped> _squatters = new List<Ped>();
        private readonly List<Vector3> _bays = new List<Vector3>();

        private Vector3 _approach;
        private Vector3 _door;
        private Vector3 _generator;

        public override string Id => "M23";
        public override string Title => "Ghost in the Sage";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _approach = Ctx.Locations.Position("M23.DomeApproach");
            _door = Ctx.Locations.Position("M23.BunkerDoor");
            _generator = Ctx.Locations.Position("M23.Generator");
            _bays.Add(Ctx.Locations.Position("M23.BayOne"));
            _bays.Add(Ctx.Locations.Position("M23.BayTwo"));
            _bays.Add(Ctx.Locations.Position("M23.BayThree"));

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _approach, Ctx.Locations.Heading("M23.DomeApproach")))
            {
                return false;
            }

            ApplyBibleSetting();
            SpawnSquatters();
            Station(CrewSlot.Guess, _approach + new Vector3(-15f, -8f, 0f));
            Station(CrewSlot.Gohan, _approach + new Vector3(15f, -8f, 0f));
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Breach the dome",
                    new ReachZoneObjective("Ice — get up to the radar dome walkway.", () => _door, 10f))
                .OwnedBy(CrewSlot.Ice)
                
                .WithCues("M23_S1_01_ICE");

            yield return new MissionStage("Clear the radar yard",
                    new KillTargetsObjective("Clear the cartel squatters out.", () => _squatters))
                .OnEnter(context =>
                {
                    foreach (var squatter in _squatters)
                    {
                        if (squatter != null && squatter.Exists()) squatter.Task.FightAgainstHatedTargets(100f);
                    }
                });

            yield return new MissionStage("Secure the bays",
                    new MultiHoldObjective("Guess — check the three storage bays.", _bays, 5, 4f,
                        "Clearing the bay"))
                .OwnedBy(CrewSlot.Guess)
                .AfterCues("M23_S1_02_GUESS");

            yield return new MissionStage("Power up",
                    new MissionInteraction("Gohan — bring the marked generator online.", () => _generator, 10, 4f))
                .OwnedBy(CrewSlot.Gohan)
                
                .OnExit(context =>
                {
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    GameUtils.Subtitle("~g~We have a command center in the desert.", 6000);
                })
                .AfterCues("M23_S1_03_GOHAN");
        }

        private void SpawnSquatters()
        {
            var models = new[] { "g_m_y_mexgoon_01", "g_m_y_mexgoon_03", "g_m_y_mexgang_01" };
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < 9; i++)
            {
                var model = new Model(models[i % models.Length]);
                if (!GameUtils.RequestModel(model)) continue;

                var post = i < 4
                    ? _door + new Vector3(-8f + i * 5f, 8f, 0f)
                    : _bays[(i - 4) % _bays.Count] + new Vector3((i % 2) * 4f - 2f, 5f, 0f);

                var squatter = World.CreatePed(model, post, 180f);
                model.MarkAsNoLongerNeeded();
                if (squatter == null || !squatter.Exists()) continue;

                squatter.RelationshipGroup = cartel;
                squatter.IsPersistent = true;
                squatter.BlockPermanentEvents = true;
                squatter.Accuracy = 30;
                squatter.Weapons.Give(i % 3 == 0 ? WeaponHash.AssaultRifle : WeaponHash.MicroSMG, 200, true, true);
                squatter.Task.GuardCurrentPosition();

                _squatters.Add(Track(squatter));
            }
        }

        protected override void OnCleanup()
        {
            _squatters.Clear();
        }
    }
}
