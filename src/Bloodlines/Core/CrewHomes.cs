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
        /// <summary>The crew's current tier, from the campaign: starter rooms, the Eclipse penthouses after M27, the Diamond penthouse after M47.</summary>
        public ApartmentTier Tier => ApartmentTiers.Current(_state);
        public bool LuxuryUnlocked => Tier != ApartmentTier.Starter;
        /// <summary>The active brother's residence at the current tier.</summary>
        public Residence Current => ApartmentTiers.For(_crew.ActiveSlot, Tier);
        public string ResidenceName => Current.Name;
        public string Progression => ApartmentTiers.Progression(Tier);
        public Vector3 SavePosition => Apartment.Inside || Apartment.Busy ? Apartment.ExitPosition : Game.Player.Character.Position;
        /// <param name="previewNext">QA only: the next tier's residence without unlocking it.</param>
        public void EnterApartment(bool previewNext = false)
        {
            if (!CanUse(3f) || Apartment.Inside || Apartment.Busy || Game.Player.Character.IsInVehicle()) return;
            var tier = Tier;
            if (previewNext) { var next = ApartmentTiers.Next(tier); if (!next.HasValue) return; tier = next.Value; }
            var residence = ApartmentTiers.For(_crew.ActiveSlot, tier);
            var location = _locations.Get(residence.InteriorKey);
            if (location == null) { GameUtils.Notify("~y~Apartment location is missing: " + residence.InteriorKey); return; }
            Logger.Info("Home: entering " + residence.Name + " (" + residence.Tier + ", " + residence.Floors + " floor" + (residence.Floors == 1 ? "" : "s") + ").");
            Apartment.Begin(location.Position, residence.Ipl, true, residence.Probe, location.Heading, residence.EntitySets);
        }
        /// <summary>The room survey for the active brother's residence, in walking order: the entry first, then the spots.</summary>
        public string[] RoomSurveyKeys => ApartmentTiers.RoomSurveyKeys(Current);
        private static readonly string[] RoomSpots = { "Wardrobe", "Bed", "Locker", "Door" };
        /// <summary>
        /// A spot in the current room once Ron has surveyed it on foot; null while it
        /// is still a desk estimate (or has no estimate at all), when it stays folded
        /// into the entry marker rather than sending the player into a wall.
        /// </summary>
        public MissionLocation RoomSpot(string name)
        {
            var spot = _locations.Get(Current.RoomPrefix + "." + name);
            return spot != null && spot.Status == LocationStatus.Surveyed ? spot : null;
        }
        public Action OpenWardrobe { get; set; }
        private void UpdateRoomSpots(Ped player, bool atEntry)
        {
            foreach (var name in RoomSpots)
            {
                var spot = RoomSpot(name);
                if (spot == null) continue;
                GameUtils.DrawObjectiveMarker(spot.Position, Color.FromArgb(90, 100, 210, 160), 0.6f);
                if (atEntry || !GameUtils.IsWithin(player.Position, spot.Position, 1.6f)) continue;
                GameUtils.Subtitle("E / D-pad Right: " + RoomPrompt(name), 500);
                if (!Game.IsControlJustPressed(GTA.Control.Context)) return;
                switch (name)
                {
                    case "Wardrobe": (OpenWardrobe ?? OpenMenu)?.Invoke(); break;
                    case "Bed": Rest(); break;
                    case "Locker": RestockLocker(); break;
                    default: ExitApartment(); break;
                }
                return;
            }
        }
        private static string RoomPrompt(string name) =>
            name == "Wardrobe" ? "wardrobe" : name == "Bed" ? "rest and save" : name == "Locker" ? "personal weapon locker" : "leave the apartment";
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
        public Vector3? Position(CrewSlot slot) => _state.IsUnlocked(Unlocks[(int)slot]) ? (_locations.Get(ApartmentTiers.For(slot, Tier).EntranceKey) ?? _locations.Get("Home." + slot))?.Position : null;
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
                // The same entry marker opens services and a clearly labeled exit.
                GameUtils.DrawObjectiveMarker(Apartment.InteriorPosition, Color.FromArgb(110, 100, 210, 160));
                bool atEntry = GameUtils.IsWithin(player.Position, Apartment.InteriorPosition, 3f);
                if (atEntry)
                {
                    GameUtils.Subtitle("E / D-pad Right: apartment - wardrobe, rest, locker, exit.", 500);
                    if (Game.IsControlJustPressed(GTA.Control.Context)) OpenMenu?.Invoke();
                }
                UpdateRoomSpots(player, atEntry);
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
                    blip.Name = hero.DisplayName + " - " + ApartmentTiers.For(hero.Slot, Tier).Short; _blips[hero.Slot] = blip;
                }
                else if (blip.Position != position.Value)
                { blip.Position = position.Value; blip.Name = hero.DisplayName + " - " + ApartmentTiers.For(hero.Slot, Tier).Short; }
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
