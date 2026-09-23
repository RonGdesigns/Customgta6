using System;
using System.Collections.Generic;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// The Osprey the crew can buy once they have shot one down in BM01, and calls in when
    /// they want it.
    ///
    /// Nothing else in the campaign sells an aircraft: the dealer and the phone sell road
    /// vehicles, and hangar and berth storage are still planned. So this is the Osprey's own
    /// short path rather than a general one. It is bought once, from the phone's garage page,
    /// and after that it is called in: set down on open ground near whoever is playing, the
    /// way a vertical-takeoff aircraft would arrive. It keeps no customization between calls,
    /// because there is no hangar to keep one in.
    /// </summary>
    public static class OspreyHangar
    {
        public const string Model = "avenger";
        public const string Name = "Mammoth Avenger (the Osprey)";
        /// <summary>The top of the price ladder. Nothing else the crew can buy costs more.</summary>
        public const int Price = 500000;
        public const string Unlock = "BM01";
        public const string OwnedFlag = "ospreyOwned";
        /// <summary>How much open ground it needs to be set down on.</summary>
        public const float Clearance = 14f;

        private static Vehicle _out;

        public static bool ForSale(CampaignState state) => state != null && state.IsComplete(Unlock);
        public static bool Owned(CampaignState state) =>
            state != null && state.FleetUpgrades.TryGetValue(OwnedFlag, out bool owned) && owned;
        public static bool IsOut => _out != null && _out.Exists() && !_out.IsDead;

        /// <summary>Buy it. The message says what happened; only a real purchase is charged.</summary>
        public static string Buy(CampaignState state)
        {
            if (!ForSale(state)) return "Bring one down first.";
            if (Owned(state)) return "The Osprey is already the crew's.";
            if (state.CashOnHand < Price) return "Need $" + Price.ToString("N0") + " crew cash.";
            state.CashOnHand -= Price;
            state.FleetUpgrades[OwnedFlag] = true;
            state.Save();
            Logger.Info("Bought the Osprey for $" + Price + ".");
            return "The Osprey is the crew's. Call it in from this page.";
        }

        /// <summary>
        /// Set it down on open ground near the player. Refused when there is nowhere it will
        /// fit, rather than dropped into the trees or the water.
        /// </summary>
        public static string Deliver(CampaignState state, Func<Vector3, float?> surface, Func<Vector3, float> room)
        {
            if (!Owned(state)) return "The crew does not own an Osprey yet.";
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return "Nobody to deliver it to.";
            if (IsOut && _out.Occupants.Length > 0) return "The Osprey is already out, with somebody in it.";
            if (!FindPad(player.Position, surface, room, out var pad))
                return "No open ground near here big enough to set it down. Try a field, a lot or an airfield.";
            if (IsOut) GameUtils.SafeDelete(_out);
            var model = new Model(Model);
            try
            {
                if (!GameUtils.RequestModel(model)) return "The Osprey would not load.";
                float heading = DriveUpStep.HeadingBetween(pad, player.Position);
                _out = World.CreateVehicle(model, pad + new Vector3(0f, 0f, 1.5f), heading);
                if (_out == null || !_out.Exists()) { _out = null; return "The Osprey could not be set down."; }
                _out.IsPersistent = true;
                _out.LockStatus = VehicleLockStatus.Unlocked;
                var blip = _out.AddBlip();
                if (blip != null) { blip.Color = BlipColor.Blue; blip.Name = "Osprey"; }
                Logger.Info("Osprey delivered to " + pad + ".");
                return "The Osprey is down " + (int)pad.DistanceTo(player.Position) + " m away. It is marked on the map.";
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }

        /// <summary>
        /// The nearest open ground: rings of candidate points around the player, each one
        /// dropped onto its surface, kept only when it is dry and has
        /// <see cref="Clearance"/> meters of room all round.
        /// </summary>
        public static bool FindPad(Vector3 near, Func<Vector3, float?> surface, Func<Vector3, float> room, out Vector3 pad)
        {
            pad = Vector3.Zero;
            if (surface == null || room == null) return false;
            foreach (float radius in new[] { 35f, 55f, 80f })
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4;
                    var probe = near + new Vector3((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius, 0f);
                    float? floor = surface(new Vector3(probe.X, probe.Y, near.Z + 60f));
                    if (!floor.HasValue) continue;
                    var ground = new Vector3(probe.X, probe.Y, floor.Value);
                    var water = new OutputArgument();
                    if (Function.Call<bool>(Hash.GET_WATER_HEIGHT, ground.X, ground.Y, ground.Z + 3f, water) && water.GetResult<float>() > ground.Z) continue;
                    if (room(ground + new Vector3(0f, 0f, 1f)) < Clearance) continue;
                    pad = ground;
                    return true;
                }
            return false;
        }

        /// <summary>Its rows on the phone's garage page. Nothing shows until BM01 has been passed.</summary>
        public static List<PhoneEntry> Rows(CampaignState state, Func<bool> free, Func<Vector3, float?> surface, Func<Vector3, float> room)
        {
            var rows = new List<PhoneEntry>();
            if (!ForSale(state)) return rows;
            if (!Owned(state))
            {
                rows.Add(new PhoneEntry { Id = "osprey-buy", Title = Name, Subtitle = "$" + Price.ToString("N0"),
                    Body = Name + "\nTilt-rotor: hovers like a helicopter, flies like a plane.\n\nOnce bought, call it in from this page and it is set down on open ground near you. It keeps no customization between calls.",
                    Button = "Buy the Osprey", Quote = "osprey:" + Price,
                    Action = () => free != null && !free() ? "Finish the mission or transition before buying." : Buy(state) });
                return rows;
            }
            rows.Add(new PhoneEntry { Id = "osprey-call", Title = "Osprey", Subtitle = IsOut ? "Out - call again to move it" : "Call it in",
                Body = "The crew's Osprey. It needs about " + (int)(Clearance * 2) + " m of open ground to set down: a field, a lot or an airfield. Calling it again moves it to you, unless somebody is aboard.",
                Button = "Call the Osprey",
                Action = () => free != null && !free() ? "Finish the mission or transition before calling it in." : Deliver(state, surface, room) });
            return rows;
        }
    }
}
