using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
namespace Bloodlines.Missions.Campaign
{
    public sealed class SM04DeadDropQuarry : DesertOperation
    {
        public override string Id => "SM04";public override string Title=>"Dead Drop Quarry";
        private Ped _first,_second;
        protected override bool Setup()
        {
            if(!MissionSites.Prepare(Ctx.Locations,Id)||!Ctx.Crew.DeploySolo(CrewSlot.Ice,At("SM04.Approach"),0))return false;
            _first=Guard(At("SM04.NestOne"),WeaponHash.SniperRifle);_second=Guard(At("SM04.NestTwo"),WeaponHash.SniperRifle);
            Game.Player.Character.Weapons.Give(WeaponHash.SniperRifle,60,true,true);return RequireAssets(_first,_second);
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Identify the nests",new ReachZoneObjective("Ice: reach the yellow quarry overlook. Both marksmen will be marked red.",()=>At("SM04.Overlook"),6)).PlayedBy(CrewSlot.Ice).WithCues("SM04_S1_01_ICE");
            yield return new MissionStage("First marksman",new KillTargetsObjective("Ice: eliminate the first red-marked marksman from cover.",()=>new[]{_first})).PlayedBy(CrewSlot.Ice).OnEnter(c=>Attack(new[]{_first,_second})).AfterCues("SM04_S1_02_ICE");
            yield return new MissionStage("Second marksman",new KillTargetsObjective("Ice: eliminate the remaining marksman before collecting the radios.",()=>new[]{_second})).PlayedBy(CrewSlot.Ice);
            yield return new MissionStage("First radio",new MissionInteraction("Ice: collect the patrol radio at the first marked nest",()=>At("SM04.NestOne"),3)).PlayedBy(CrewSlot.Ice);
            yield return new MissionStage("Second radio",new MissionInteraction("Ice: collect the other marksman's radio",()=>At("SM04.NestTwo"),3)).PlayedBy(CrewSlot.Ice).AfterCues("SM04_S2_03_ICE");
            yield return new MissionStage("Leave the quarry",new ReachZoneObjective("Ice: take both radios back to the yellow approach marker.",()=>At("SM04.Approach"),12)).PlayedBy(CrewSlot.Ice);
        }
    }
}
