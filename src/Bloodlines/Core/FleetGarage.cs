using System.Collections.Generic;
using Bloodlines.Missions;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Applies the campaign's fleet upgrades to the crew's vehicles at runtime.
    ///
    /// M11 installs a 700-HP turbine in the Granger, M35 brings home the half-track,
    /// M17 reinforces the Kraken — and once a save records that, the vehicle should
    /// behave that way for the rest of the campaign. Doing it in script means the
    /// upgrades work without the OpenIV asset pack installed; the pack (see
    /// assets/README.md) makes the same changes permanent and world-wide.
    ///
    /// Only vehicles the player is actually in are touched, and each one is upgraded
    /// once — reapplying mods every tick fights the engine and audibly stutters the
    /// engine sound.
    /// </summary>
    public sealed class FleetGarage
    {
        private readonly CampaignState _state;
        private readonly HashSet<int> _upgraded = new HashSet<int>();

        public FleetGarage(CampaignState state)
        {
            _state = state;
        }

        public void Update()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || !player.IsInVehicle()) return;

            var vehicle = player.CurrentVehicle;
            if (vehicle == null || !vehicle.Exists() || _upgraded.Contains(vehicle.Handle)) return;

            string model = vehicle.DisplayName;

            if (_state.FleetUpgrades.TryGetValue("grangerTurbineInstalled", out bool turbine) && turbine
                && (vehicle.Model == new Model("granger") || vehicle.Model == new Model("granger2")))
            {
                InstallTurbine(vehicle);
                _upgraded.Add(vehicle.Handle);
                GameUtils.Notify("~b~Turbine Granger 3600LX~s~ — 700 HP, plated.");
                Logger.Info("Applied the turbine profile to " + model + ".");
                return;
            }

            if (_state.FleetUpgrades.TryGetValue("krakenSubmarineReinforced", out bool kraken) && kraken
                && vehicle.Model == new Model("submersible2"))
            {
                vehicle.IsBulletProof = true;
                vehicle.CanTiresBurst = false;
                _upgraded.Add(vehicle.Handle);
                Logger.Info("Applied the reinforced hull profile to the Kraken.");
            }
        }

        private static void InstallTurbine(Vehicle vehicle)
        {
            vehicle.Mods.InstallModKit();
            Fit(vehicle, VehicleModType.Engine, 3);
            Fit(vehicle, VehicleModType.Transmission, 2);
            Fit(vehicle, VehicleModType.Brakes, 2);
            Fit(vehicle, VehicleModType.Armor, 4);
            Fit(vehicle, VehicleModType.Suspension, 3);

            vehicle.EnginePowerMultiplier = 45f;
            vehicle.EngineTorqueMultiplier = 1.6f;
            vehicle.CanTiresBurst = false;
            vehicle.IsBulletProof = false;
            Function.Call(Hash.SET_VEHICLE_ENVEFF_SCALE, vehicle, 1.0f);
        }

        private static void Fit(Vehicle vehicle, VehicleModType type, int requested)
        {
            int count = vehicle.Mods[type].Count;
            if (count > 0) vehicle.Mods[type].Index = System.Math.Min(requested, count - 1);
        }

        /// <summary>Forgets which vehicles were upgraded — used when the crew stands down.</summary>
        public void Reset()
        {
            _upgraded.Clear();
        }
    }
}
