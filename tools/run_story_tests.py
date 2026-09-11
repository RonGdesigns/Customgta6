"""Exercise production switching, scenes, dialogue, dispatch and save code without GTA."""
from pathlib import Path
import subprocess, tempfile
import build_roslyn
ROOT=Path(__file__).resolve().parents[1]
def main():
    output=ROOT/'build/story-tests';output.mkdir(parents=True,exist_ok=True)
    framework=Path(build_roslyn.fetch_package(*build_roslyn.REFERENCE_ASSEMBLIES))/'build/.NETFramework/v4.8'
    args=['/nologo','/nostdlib+','/target:exe','/langversion:9.0','/warnaserror+',f'/out:"{output/"StoryTests.exe"}"']
    args += [f'/reference:"{framework/name}"' for name in ['mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll']]
    sources=['Core/ApartmentAccess.cs','Core/ApartmentTiers.cs','Core/InteriorMapper.cs','Core/MissionSites.cs','Core/MissionObjectiveHud.cs','Crew/CrewDurability.cs','Missions/Objectives/MissionInteraction.cs','Missions/Campaign/Act1/M03CypressFoundry.cs','Missions/Campaign/Act1/M04SeveredWire.cs','Missions/Campaign/Act1/M05TidalLock.cs','Missions/Campaign/Act1/M06CleanSweep.cs','Missions/ComposedMission.cs','Missions/Objectives/MissionStage.cs','Missions/ProximityHack.cs','Missions/Campaign/Act1/M02LooseStrands.cs','Missions/Objectives/AssignedWorkObjective.cs','Core/AbilityChord.cs','Core/AbilityMeterLayout.cs','Crew/CrewAppearance.cs','Core/CharacterWheel.cs','Core/ControllerNavigation.cs','Core/ProloguePlacement.cs','Missions/Campaign/Act1/M01GhostInTheDockyard.cs','Crew/SwitchController.cs','Crew/Protagonist.cs','Core/CutsceneDirector.cs','Core/DialogueDirector.cs','Core/WaveTiming.cs','Core/DataTable.cs','Core/CampaignData.cs','Core/LocationBook.cs','Core/Json.cs','Core/MissionMarkers.cs','Core/ObjectiveMarkers.cs','Missions/Mission.cs','Missions/Objectives/Objective.cs','Missions/MissionManager.cs','Missions/CampaignState.cs']
    sources += ['Core/WeaponMarket.cs','Core/ShopService.Customization.cs','Core/TravelHandling.cs','Core/MissionHandoff.cs','Core/ShopService.cs','Core/WorldTuning.cs','Crew/TacticalResponse.cs','Crew/CrewMemory.cs','Missions/Objectives/TrailerDeliveryObjective.cs','Core/CampaignDispatches.cs','Core/StoryVehicles.cs','Core/CrewHomes.cs','Crew/WeaponProgression.cs','Crew/CrewDriving.cs','Crew/CompanionLife.cs','Crew/PersonalWanted.cs','Crew/MilitaryResponse.cs','Missions/Objectives/StandardObjectives.cs','Missions/Objectives/AdvancedObjectives.cs','Core/SceneBlocking.cs','Core/PrologueSequence.cs','Core/FleetGarage.cs','Missions/OperationHandoff.cs','Missions/Objectives/TechnicalChoiceObjective.cs','Core/RoadHandling.cs','Core/ControlDiagnostics.cs','Missions/HeliInsertion.cs','Core/VisualAtmosphere.cs','Core/CrewVan.cs','Core/SceneSteps.cs','Core/PropPlacement.cs','Missions/RoleTrack.cs','Missions/Objectives/FlowObjectives.cs','Missions/MissionContextCard.cs','Abilities/Ability.cs','Abilities/SlipstreamReflex.cs']
    sources += [str(p.relative_to(ROOT/'src/Bloodlines')).replace('\\','/') for p in (ROOT/'src/Bloodlines/Missions/Campaign').rglob('*.cs')]
    sources = list(dict.fromkeys(sources))
    args += [f'"{ROOT/"src/Bloodlines"/name}"' for name in sources]
    main_source=(ROOT/'src/Bloodlines/BloodlinesMain.cs').read_text(encoding='utf-8')
    start=main_source.index('        private void HandleControllerSwitch()')
    end=main_source.index('        private void HandleAbortHold()',start)
    controller=output/'ControllerHarness.cs'
    controller.write_text('using System;using GTA;using Bloodlines.Core;using Bloodlines.Crew;public sealed class MenuProbe{public bool IsOpen;}public sealed class HomeProbe{public ApartmentAccess Apartment=new ApartmentAccess(new CrewRoster());}public sealed class ControllerHarness{public Bloodlines.Abilities.AbilityController _abilities=new Bloodlines.Abilities.AbilityController();public CharacterWheel _characterWheel=new CharacterWheel("missing-test-images");public ModConfig _config=new ModConfig();public CrewRoster _crew;public SwitchController _switching;public HomeProbe _homes=new HomeProbe();public MenuProbe _menu=new MenuProbe();private CrewSlot? _controllerSelection;private bool _controllerWheelHeld;public void Tick(){HandleControllerSwitch();}'+main_source[start:end]+'}',encoding='utf-8')
    args.append(f'"{controller}"')
    ability=(ROOT/'src/Bloodlines/Abilities/AbilityController.cs').read_text(encoding='utf-8')
    first=ability.index('        public void HandleController(bool blocked)');last=ability.index('        public bool IsActive',first)
    harness=output/'AbilityInputHarness.cs'
    harness.write_text('using GTA;using Bloodlines.Core;using Bloodlines.Crew;public class AbilityInputHarness{public ModConfig _config=new ModConfig();public CrewRoster _crew=new CrewRoster();private AbilityChord _chord=new AbilityChord();public int Toggles;private void Toggle(){Toggles++;}'+ability[first:last]+'}',encoding='utf-8')
    args.append(f'"{harness}"')
    args += [f'"{p}"' for p in sorted((ROOT/'tests/story').glob('*.cs'))]
    rsp=output/'tests.rsp';rsp.write_text('\n'.join(args),encoding='utf-8')
    subprocess.run([build_roslyn.find_csc(),'/noconfig','@'+str(rsp)],check=True)
    with tempfile.TemporaryDirectory(dir=output) as temp:
        subprocess.run([str(output/'StoryTests.exe'),str(ROOT/'data'),temp],check=True)
if __name__=='__main__':main()
