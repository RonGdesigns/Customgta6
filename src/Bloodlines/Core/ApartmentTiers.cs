using System;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>Where the crew lives: three tiers, each an upgrade on the last.</summary>
    public enum ApartmentTier { Starter, Luxury, Top }

    /// <summary>One residence: the interior it teleports into, the door on the street, and what makes the room furnished.</summary>
    public sealed class Residence
    {
        public ApartmentTier Tier;
        public CrewSlot? Slot;
        public string Name = "";
        public string Short = "";
        public string InteriorKey = "";
        public string EntranceKey = "";
        public string RoomPrefix = "";
        public string Ipl;
        public string[] EntitySets = new string[0];
        public Vector3? Probe;
        public int Floors = 1;
    }

    /// <summary>
    /// The residences and their unlocks (Ron, September 10: every brother's starter
    /// room its own layout; the luxury tier big; a top tier above it with more than
    /// one floor). The starter rooms are three stock interiors that exist in Story
    /// Mode; the luxury tier is the three Eclipse Towers penthouse floors; the top
    /// tier is the Diamond penthouse, two levels, shared by the crew.
    /// </summary>
    public static class ApartmentTiers
    {
        public const string LuxuryUnlock = "M27";
        public const string TopUnlock = "M47";

        private static readonly string[] DiamondSets =
        {
            "Set_Pent_Tint_Shell", "Set_Pent_Pattern_09", "Set_Pent_Spa_Bar_Open", "Set_Pent_Media_Bar_Open",
            "Set_Pent_Dealer", "Set_Pent_Arcade_Modern", "Set_Pent_Clutter_01", "Set_Pent_Bar_Clutter",
            "Set_Pent_Bar_Party_0", "Set_Pent_Bar_Light_0", "Set_Pent_Lounge_Open", "Set_Pent_Spa_Open",
            "Set_Pent_Cinema_Open", "Set_Pent_Guest_Open", "Set_Pent_Office_Open", "Set_Pent_Bar_Open"
        };

        public static ApartmentTier Current(CampaignState state)
        {
            if (state == null) return ApartmentTier.Starter;
            if (state.IsComplete(TopUnlock)) return ApartmentTier.Top;
            if (state.IsComplete(LuxuryUnlock)) return ApartmentTier.Luxury;
            return ApartmentTier.Starter;
        }

        public static ApartmentTier? Next(ApartmentTier tier) =>
            tier == ApartmentTier.Starter ? ApartmentTier.Luxury : tier == ApartmentTier.Luxury ? (ApartmentTier?)ApartmentTier.Top : null;

        public static string UnlockOf(ApartmentTier tier) =>
            tier == ApartmentTier.Luxury ? LuxuryUnlock : tier == ApartmentTier.Top ? TopUnlock : "";

        /// <summary>What the next step up is, for the home menu.</summary>
        public static string Progression(ApartmentTier tier)
        {
            var next = Next(tier);
            if (!next.HasValue) return "Top tier: the Diamond penthouse";
            return next == ApartmentTier.Luxury ? "Eclipse Towers penthouses unlock after " + LuxuryUnlock : "The Diamond penthouse unlocks after " + TopUnlock;
        }

        public static Residence For(CrewSlot slot, ApartmentTier tier)
        {
            switch (tier)
            {
                case ApartmentTier.Top:
                    return new Residence
                    {
                        Tier = tier, Slot = null, Name = "The Diamond penthouse, two floors", Short = "The Diamond",
                        InteriorKey = "Apartment.Top.Interior", EntranceKey = "Apartment.Top.Entrance", RoomPrefix = "Apartment.Room.Top",
                        Ipl = "vw_casino_penthouse", EntitySets = DiamondSets, Probe = new Vector3(976.636f, 70.295f, 115.164f), Floors = 2
                    };
                case ApartmentTier.Luxury:
                    // Each penthouse occupies a different floor; no overlapping themes are loaded.
                    return new Residence
                    {
                        Tier = tier, Slot = slot, Name = "Eclipse Towers - " + Protagonist.Of(slot).DisplayName + " penthouse", Short = "Eclipse Towers",
                        InteriorKey = "Apartment.Luxury." + slot, EntranceKey = "Apartment.Luxury.Entrance", RoomPrefix = "Apartment.Room.Luxury." + slot,
                        Ipl = slot == CrewSlot.Ice ? "apa_v_mp_h_01_a" : slot == CrewSlot.Gohan ? "apa_v_mp_h_01_b" : "apa_v_mp_h_01_c",
                        // Room-center probes identify the requested floor when a doorway's coordinate lookup returns zero.
                        Probe = slot == CrewSlot.Ice ? new Vector3(-787.7805f, 334.9232f, 215.8384f) : slot == CrewSlot.Gohan
                            ? new Vector3(-773.2258f, 322.8252f, 194.8862f) : new Vector3(-787.7805f, 334.9232f, 186.1134f)
                    };
                default:
                    // Three different rooms: a studio, a one-bedroom, a house.
                    string name = slot == CrewSlot.Ice ? "Little Seoul studio" : slot == CrewSlot.Gohan ? "Richards Majestic one-bedroom" : "Forum Drive house";
                    return new Residence
                    {
                        Tier = tier, Slot = slot, Name = Protagonist.Of(slot).DisplayName + " - " + name, Short = name,
                        InteriorKey = "Apartment.Starter.Interior." + slot, EntranceKey = "Apartment.Starter." + slot, RoomPrefix = "Apartment.Room." + slot
                    };
            }
        }

        /// <summary>The room survey for a residence, in walking order: the entry first, then the spots.</summary>
        public static string[] RoomSurveyKeys(Residence residence) => new[]
        {
            residence.InteriorKey, residence.RoomPrefix + ".Door", residence.RoomPrefix + ".Message",
            residence.RoomPrefix + ".Wardrobe", residence.RoomPrefix + ".Bed", residence.RoomPrefix + ".Locker"
        };
    }
}
