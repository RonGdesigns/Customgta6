using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    public sealed class SM04DeadDropQuarry : DesertOperation
    {
        public override string Id => "SM04";
        public override string Title => "Dead Drop Quarry";
        private Ped _first, _second;
        private readonly Prop[] _radios = new Prop[2];
        private readonly bool[] _recovered = new bool[2];
        private bool _returned;
        public int RadiosRecovered => (_recovered[0] ? 1 : 0) + (_recovered[1] ? 1 : 0);
        public IReadOnlyList<Prop> Radios => _radios;

        protected override bool Setup()
        {
            _returned = false; Array.Clear(_recovered, 0, 2);
            if (!MissionSites.Prepare(Ctx.Locations, Id) || !Ctx.Crew.DeploySolo(CrewSlot.Ice, At("SM04.Approach"), Ctx.Locations.Heading("SM04.Approach"))) return false;
            _first = Guard(At("SM04.NestOne"), WeaponHash.SniperRifle);
            _second = Guard(At("SM04.NestTwo"), WeaponHash.SniperRifle);
            if (!RequireAssets(_first, _second)) return false;
            Game.Player.Character.Weapons.Give(WeaponHash.SniperRifle, 60, true, true);
            // The radios belong to fixed observation posts; the marksmen may leave cover.
            _radios[0] = WorkProp("prop_cs_hand_radio", At("SM04.NestOne"));
            _radios[1] = WorkProp("prop_cs_hand_radio", At("SM04.NestTwo"));
            if (!RequireAssets(_radios[0], _radios[1])) return false;
            Ctx.Cutscenes.Play(new SceneSpec { MissionId = Id, Phase = "approach", Title = "Two listening posts",
                Reason = "Show the two marksmen and their physical radios. Ice works alone; his brothers check in by radio.",
                Blocking = new SceneBlocking().Then(ShotStep.Low(2200, _first, 5, 3, 2))
                    .Then(ShotStep.Low(2200, _second, 5, -3, 2))
                    .Then(new ShotStep(1800, _radios[0], new Vector3(-1, -1, .7f), _radios[0], Vector3.Zero)) });
            RequireSurvivor(Ctx.Crew.PedFor(CrewSlot.Ice), "Ice is down. Restart this solo mission.");
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Identify the nests", new ReachZoneObjective("Ice: reach the yellow quarry overlook. Locate both marksmen before moving down to their radios.", () => At("SM04.Overlook"), 6))
                .PlayedBy(CrewSlot.Ice).WithCues("SM04_S1_01_ICE");
            yield return new MissionStage("Clear both listening posts", new KillTargetsObjective("Ice: eliminate BOTH red-marked marksmen. Use the ridge as cover.", () => new[] { _first, _second }))
                .PlayedBy(CrewSlot.Ice).OnEnter(c => Attack(new[] { _first, _second })).AfterCues("SM04_S1_02_ICE");
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                yield return new MissionStage("Recover radio " + (i + 1),
                    new MissionInteraction("Ice: recover radio " + (i + 1) + " of 2 from the marked listening post", () => _radios[index].Position, 2, 2.5f, animation: MissionInteraction.ReachInside),
                    new ProtectObjective("", () => _radios[index], "A patrol radio was destroyed before Ice recovered it."))
                    .PlayedBy(CrewSlot.Ice).OnExit(c => Recover(index));
            }
            yield return new MissionStage("Return with both radios", new ReachZoneObjective("Ice: take BOTH radios back to the yellow quarry approach. Gohan will copy their frequencies over the radio.", () => At("SM04.Approach"), 12))
                .PlayedBy(CrewSlot.Ice).OnExit(c => ReturnRadios());
            yield return new MissionStage("Frequencies copied", new ConditionObjective("Listen while Gohan confirms both recovered radio channels.", () => _returned && !Ctx.Cutscenes.IsActive))
                .PlayedBy(CrewSlot.Ice).AfterCues("SM04_S2_03_ICE");
        }
        private void Recover(int index)
        {
            if (_recovered[index]) return;
            var radio = _radios[index];
            if (!RequireAssets(radio) || radio.IsDead || !_first.IsDead || !_second.IsDead) throw new InvalidOperationException("Clear both posts and recover the intact radio.");
            _recovered[index] = true; radio.IsVisible = false;
            GTA.Native.Function.Call(GTA.Native.Hash.SET_ENTITY_COLLISION, radio, false, false);
            Ctx.State?.SetCargo("quarryRadio" + (index + 1), "Ice");
            Radio("ICE", RadiosRecovered + " of 2 radios bagged. Returning when I have both.", "SM04_RECOVER_" + index);
        }
        private void ReturnRadios()
        {
            RequiredScene("radios", "Both channels", "Ice remains at the quarry exit while Gohan copies the recovered frequencies over radio.",
                new SceneBlocking().Then(ShotStep.Watching(2600, Game.Player.Character, Game.Player.Character))
                    .Then(new VerifySceneStep("Both radios recovered", () => RadiosRecovered == 2 && Game.Player.Character.Position.DistanceTo(At("SM04.Approach")) <= 12f, () => _returned = true)));
        }
        protected override void OnUpdate() { if (!Ctx.Cutscenes.IsActive) base.OnUpdate(); }
        protected override void OnPassed()
        {
            if (!_returned || RadiosRecovered != 2) throw new InvalidOperationException("Both radios must reach the exit before completion.");
            Ctx.State?.SetCargo("quarryRadio1", "crewRadioArchive");
            Ctx.State?.SetCargo("quarryRadio2", "crewRadioArchive");
        }
    }
}
