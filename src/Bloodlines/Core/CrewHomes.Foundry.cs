using System;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    public sealed partial class CrewHomes
    {
        private bool _foundryVisit;
        private Blip _foundryBlip;
        public bool FoundryVisit => _foundryVisit;
        public bool FoundryUnlocked => _state.IsUnlocked("cypressFoundry");
        public Vector3? FoundryEntrance => FoundryUnlocked ? (Vector3?)MissionPlacement.Position(_locations,
            "Hideout.Foundry.Entrance", _locations.Position("Base.CypressFlats") + new Vector3(0f, -12f, 0f)) : null;
        private static Residence FoundryResidence => new Residence
        {
            Name = "Cypress Foundry headquarters", Short = "Foundry HQ",
            EntranceKey = "Hideout.Foundry.Entrance", InteriorKey = "Hideout.Foundry.Interior",
            RoomPrefix = "Hideout.Foundry.Room",
            // REQUEST_IPL uses the registered runtime name, not CMapData.name's trailing underscore.
            Ipl = "bkr_biker_interior_placement_interior_1_biker_dlc_int_02_milo",
            Probe = new Vector3(998.4809f, -3164.711f, -38.90733f),
            EntitySets = new[] { "walls_01", "lower_walls_default", "furnishings_02", "decorative_02", "gun_locker", "no_mod_booth" }
        };
        public void RouteFoundry()
        {
            var point = FoundryEntrance;
            if (!point.HasValue) { GameUtils.Notify("~y~Secure the foundry in M03 first. It must still be held by the crew."); return; }
            Function.Call(Hash.SET_NEW_WAYPOINT, point.Value.X, point.Value.Y);
            GameUtils.Notify("Cypress Foundry: enter on foot at the headquarters marker.");
        }
        public void EnterFoundry()
        {
            var player = Game.Player.Character; var entrance = FoundryEntrance;
            if (!_crew.IsDeployed || Allowed?.Invoke() == false || !entrance.HasValue || Apartment.Inside || Apartment.Busy ||
                player == null || !player.Exists() || player.IsDead || player.IsInVehicle() || player.IsInCombat ||
                Game.Player.WantedLevel > 0 || !GameUtils.IsWithin(player.Position, entrance.Value, 3f))
            { GameUtils.Notify("~y~Enter the held foundry on foot between jobs, after losing the police."); return; }
            var residence = FoundryResidence; var location = _locations.Get(residence.InteriorKey);
            if (location == null) { GameUtils.Notify("~y~Foundry interior location is missing."); return; }
            // Register the DLC map parts in Story Mode before requesting this room.
            // IPL lookup alone never registered an interior on the tested Enhanced build.
            // Through DlcMaps: this was the third place in the mod calling that native on
            // its own, which is how the Paleto yacht came to depend on the bunker having
            // done it first without anything saying so.
            DlcMaps.EnsureRegistered();
            _foundryVisit = Apartment.Begin(location.Position, residence.Ipl, true, residence.Probe, location.Heading,
                residence.EntitySets, new[] { "walls_02", "furnishings_01", "decorative_01", "no_gun_locker", "mod_booth" });
            if (_foundryVisit) Logger.Info("Foundry HQ: loading the furnished industrial clubhouse and weapon locker.");
        }
        public Action OpenPlanningBoard { get; set; }
        public void ReviewFoundryPlan()
        {
            if ((_foundryVisit || _bunkerVisit) && Apartment.Inside && CanUse(55f)) (OpenPlanningBoard ?? RouteNextLead)?.Invoke();
        }
        private void UpdateFoundry(Ped player)
        {
            var point = FoundryEntrance;
            if (!point.HasValue) { ClearFoundryBlip(); return; }
            if (_foundryBlip == null || !_foundryBlip.Exists())
            {
                _foundryBlip = World.CreateBlip(point.Value);
                if (_foundryBlip != null) { _foundryBlip.Sprite = BlipSprite.Safehouse; _foundryBlip.Color = BlipColor.Green; _foundryBlip.Name = "Cypress Foundry - crew headquarters"; }
            }
            else _foundryBlip.Position = point.Value;
            if (!GameUtils.IsWithinFlat(player.Position, point.Value, 60f)) return;
            GameUtils.DrawObjectiveMarker(point.Value, Color.FromArgb(130, 220, 150, 50));
            if (player.IsInVehicle() || !GameUtils.IsWithin(player.Position, point.Value, 3f)) return;
            GameUtils.Subtitle("E / D-pad Right: enter Foundry HQ - planning, rest, wardrobe and weapon locker.", 500);
            if (Game.IsControlJustPressed(GTA.Control.Context)) EnterFoundry();
        }
        private void ClearFoundryBlip() { GameUtils.SafeDelete(_foundryBlip); _foundryBlip = null; }
    }
}
