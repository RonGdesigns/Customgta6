using System;
using System.Collections.Generic;
using System.Drawing;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Exterior home access points: rest, campaign save and personal weapon locker.</summary>
    public sealed class CrewHomes
    {
        private readonly CrewRoster _crew;
        private readonly CampaignState _state;
        private readonly LocationBook _locations;
        private readonly WeaponProgression _weapons;
        private readonly Dictionary<CrewSlot, Blip> _blips = new Dictionary<CrewSlot, Blip>();
        private static readonly string[] Unlocks = { "canalLogisticsLoft", "littleSeoulStudio", "burroHeightsChopShop" };
        private int _nextRest;
        public ApartmentAccess Apartment { get; private set; }
        public bool LuxuryUnlocked => _state.IsComplete("M27");
        public string ResidenceName => LuxuryUnlocked ? "Eclipse Towers - " + _crew.Active.DisplayName + " suite" : _crew.Active.DisplayName + " - furnished starter apartment";
        public string Progression => LuxuryUnlocked ? "Luxury apartment available" : "Luxury apartments unlock after M27";
        public Vector3 SavePosition => Apartment.Inside || Apartment.Busy ? Apartment.ExitPosition : Game.Player.Character.Position;
        public void EnterApartment(bool previewLuxury = false)
        {
            if (!CanUse(3f) || Apartment.Inside || Apartment.Busy || Game.Player.Character.IsInVehicle()) return;
            bool luxury = LuxuryUnlocked || previewLuxury;
            string key = luxury ? "Apartment.Luxury." + _crew.ActiveSlot : "Apartment.Starter.Interior";
            var location = _locations.Get(key);
            if (location == null) { GameUtils.Notify("~y~Apartment location is missing."); return; }
            // Each penthouse occupies a different floor; no overlapping themes are loaded.
            string ipl = luxury ? (_crew.ActiveSlot == CrewSlot.Ice ? "apa_v_mp_h_01_a" : _crew.ActiveSlot == CrewSlot.Gohan ? "apa_v_mp_h_01_b" : "apa_v_mp_h_01_c") : null;
            // Room-centre probes identify the requested floor when a doorway's
            // coordinate lookup returns zero. Teleport still uses LocationBook.
            Vector3? probe = !luxury ? (Vector3?)null : _crew.ActiveSlot == CrewSlot.Ice
                ? new Vector3(-787.7805f, 334.9232f, 215.8384f) : _crew.ActiveSlot == CrewSlot.Gohan
                ? new Vector3(-773.2258f, 322.8252f, 194.8862f) : new Vector3(-787.7805f, 334.9232f, 186.1134f);
            Apartment.Begin(location.Position, ipl, true, probe);
        }
        public void ExitApartment() { if (Apartment.Inside && !Apartment.Busy) Apartment.Begin(Apartment.ExitPosition, null, false); }
        public void StopApartment() { Apartment.Cancel(); }
        public void UpdateTransition() { Apartment.Update(); }
        public Action OpenMenu { get; set; }
        public Action RouteNextLead { get; set; }
        public Action<Vehicle> ApplyFleetUpgrade { get; set; }
        public Func<bool> Allowed { get; set; }
        public string WorkbenchName => _crew.ActiveSlot == CrewSlot.Ice ? "Restock weapons and armor" :
            _crew.ActiveSlot == CrewSlot.Gohan ? "Review the next verified lead" : "Repair the nearby vehicle";
        private bool CanUse(float radius)
        {
            var player = Game.Player.Character; var home = Position(_crew.ActiveSlot);
            if (!_crew.IsDeployed || Allowed?.Invoke() == false || !home.HasValue || player == null || !player.Exists() || player.IsDead ||
                !(Apartment.Inside ? GameUtils.IsWithin(player.Position, Apartment.InteriorPosition, 55f) : GameUtils.IsWithin(player.Position, home.Value, radius)) || Game.Player.WantedLevel > 0 || player.IsInCombat)
            { GameUtils.Notify("~y~Use your own home between jobs, after losing the police."); return false; }
            return true;
        }
        public void UseWorkbench()
        {
            if (!CanUse(35f)) return;
            var player = Game.Player.Character;
            if (_crew.ActiveSlot == CrewSlot.Ice)
            { _weapons.SaveCrew(_crew); _weapons.Apply(_crew.ActiveSlot, player, true); player.Armor = CrewDurability.Armor; GameUtils.Notify("~b~Personal locker restocked. Armor replaced."); }
            else if (_crew.ActiveSlot == CrewSlot.Gohan) RouteNextLead?.Invoke();
            else
            {
                var vehicle = player.CurrentVehicle ?? Game.Player.LastVehicle;
                if (vehicle == null || !vehicle.Exists() || vehicle.IsDead || vehicle.Position.DistanceTo(player.Position) > 15f ||
                    !(vehicle.Model.IsCar || vehicle.Model.IsBike) || vehicle.Speed > 1f)
                { GameUtils.Notify("~y~Park a car or motorcycle beside the chop bay first."); return; }
                vehicle.Repair(); vehicle.Wash(); ApplyFleetUpgrade?.Invoke(vehicle);
                GameUtils.Notify("~o~Repaired at Guess's chop bay.");
            }
        }
        public void RestockLocker()
        {
            if (!CanUse(35f)) return;
            _weapons.SaveCrew(_crew); _weapons.Apply(_crew.ActiveSlot, Game.Player.Character, true);
            GameUtils.Notify("~g~Personal weapon locker restocked.");
        }
        public CrewHomes(CrewRoster crew, CampaignState state, LocationBook locations, WeaponProgression weapons)
        { _crew = crew; _state = state; _locations = locations; _weapons = weapons; Apartment = new ApartmentAccess(crew); }
        public Vector3? Position(CrewSlot slot) => _state.IsUnlocked(Unlocks[(int)slot]) ? (_locations.Get(LuxuryUnlocked ? "Apartment.Luxury.Entrance" : "Apartment.Starter." + slot) ?? _locations.Get("Home." + slot))?.Position : null;
        public void RouteHome()
        {
            var point = Position(_crew.ActiveSlot);
            if (point.HasValue) { Function.Call(Hash.SET_NEW_WAYPOINT, point.Value.X, point.Value.Y); GameUtils.Notify(_crew.Active.DisplayName + ": " + ResidenceName); }
        }
        public void Update(bool available)
        {
            if (!available || !_crew.IsDeployed) { Clear(); return; }
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;
            if (Apartment.Inside)
            {
                // The same entry marker opens services and a clearly labelled exit.
                GameUtils.DrawObjectiveMarker(Apartment.InteriorPosition, Color.FromArgb(110, 100, 210, 160));
                if (GameUtils.IsWithin(player.Position, Apartment.InteriorPosition, 3f))
                {
                    GameUtils.Subtitle("E / D-pad Right: apartment - wardrobe, rest, locker, exit.", 500);
                    if (Game.IsControlJustPressed(GTA.Control.Context)) OpenMenu?.Invoke();
                }
                return;
            }
            foreach (var hero in Protagonist.All)
            {
                var position = Position(hero.Slot);
                if (LuxuryUnlocked && hero.Slot != _crew.ActiveSlot) position = null;
                if (!position.HasValue) { if (_blips.TryGetValue(hero.Slot, out var old)) { GameUtils.SafeDelete(old); _blips.Remove(hero.Slot); } continue; }
                if (!_blips.TryGetValue(hero.Slot, out var blip) || !blip.Exists())
                {
                    blip = World.CreateBlip(position.Value); if (blip == null) continue;
                    blip.Sprite = BlipSprite.Safehouse; blip.Color = hero.BlipColor;
                    blip.Name = hero.DisplayName + (LuxuryUnlocked ? " - Eclipse Towers" : " - apartment"); _blips[hero.Slot] = blip;
                }
                else if (blip.Position != position.Value)
                { blip.Position = position.Value; blip.Name = hero.DisplayName + " - Eclipse Towers"; }
            }
            var home = Position(_crew.ActiveSlot);
            if (!home.HasValue || !GameUtils.IsWithinFlat(player.Position, home.Value, 60f)) return;
            GameUtils.DrawObjectiveMarker(home.Value, Color.FromArgb(110, 100, 210, 160));
            if (!GameUtils.IsWithin(player.Position, home.Value, 3f) || player.IsInVehicle()) return;
            if (Game.Player.WantedLevel > 0 || player.IsInCombat)
            { GameUtils.Subtitle("Lose the police and leave combat before resting at home.", 500); return; }
            GameUtils.Subtitle("E / D-pad Right: home - enter apartment, wardrobe, workbench.", 500);
            if (Game.GameTime < _nextRest || !Game.IsControlJustPressed(GTA.Control.Context)) return;
            if (OpenMenu != null) { OpenMenu(); return; }
            Rest();
        }
        public void Rest()
        {
            if (!CanUse(3f) || Game.Player.Character.IsInVehicle() || Game.GameTime < _nextRest) return;
            var player = Game.Player.Character;
            _nextRest = Game.GameTime + 10000;
            bool control = Game.Player.CanControlCharacter;
            try
            {
                Game.Player.CanControlCharacter = false; GameUtils.FadeOut(350); Script.Wait(400);
                _weapons.SaveCrew(_crew); _weapons.Apply(_crew.ActiveSlot, player, true);
                player.Health = CrewDurability.Health;
                Function.Call(Hash.ADD_TO_CLOCK_TIME, 6, 0, 0);
                _state.RecordPosition(_crew.ActiveSlot, SavePosition); _state.Save();
            }
            finally { Game.Player.CanControlCharacter = control; GameUtils.FadeIn(350); }
            GameUtils.Notify("~g~Rested and saved at " + ResidenceName + ".");
        }
        public void Clear() { foreach (var blip in _blips.Values) GameUtils.SafeDelete(blip); _blips.Clear(); }
    }
}
