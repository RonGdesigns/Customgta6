using System.Collections.Generic;
using Bloodlines.Missions;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Applies the campaign's fleet upgrades to the crew's vehicles at runtime.
    ///
    /// M11's Granger reward supplies plating, brakes, suspension and tires.
    /// Engine/torque boosts were removed for the requested stock-power driving
    /// profile. WorldTuning owns the higher road-car gearing ceiling. M17 still
    /// reinforces the Kraken hull independently.
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
                GameUtils.Notify("~b~Turbine Granger 3600LX~s~ — reinforced fleet package.");
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
            Fit(vehicle, VehicleModType.Brakes, 2);
            Fit(vehicle, VehicleModType.Armor, 4);
            Fit(vehicle, VehicleModType.Suspension, 3);

            // Top-speed extension belongs to WorldTuning; do not boost acceleration.
            vehicle.EnginePowerMultiplier = 0f;
            vehicle.EngineTorqueMultiplier = 1f;
            vehicle.CanTiresBurst = false;
            vehicle.IsBulletProof = false;
            Function.Call(Hash.SET_VEHICLE_ENVEFF_SCALE, vehicle, 1.0f);
        }

        private static void Fit(Vehicle vehicle, VehicleModType type, int requested)
        {
            int count = vehicle.Mods[type].Count;
            if (count > 0) vehicle.Mods[type].Index = System.Math.Min(requested, count - 1);
        }

        /// <summary>
        /// SM03's prize. The save flag existed with nothing reading it, so the benefit
        /// is now concrete: any car or motorcycle repaired at Guess's chop bay leaves
        /// with the race transmission fitted. Returns what was done for the
        /// notification, or null when the flag is not earned or the vehicle cannot take it.
        /// </summary>
        public string FitChopBay(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return null;
            if (!_state.FleetUpgrades.TryGetValue("racingTransmissionInstalled", out bool racing) || !racing) return null;
            if (!(vehicle.Model.IsCar || vehicle.Model.IsBike)) return null;
            vehicle.Mods.InstallModKit();
            int count = vehicle.Mods[VehicleModType.Transmission].Count;
            if (count <= 0) return null;
            int target = System.Math.Min(2, count - 1);
            if (vehicle.Mods[VehicleModType.Transmission].Index >= target) return null;
            vehicle.Mods[VehicleModType.Transmission].Index = target;
            Logger.Info("Fitted the SM03 race transmission to " + vehicle.DisplayName + ".");
            return "Race transmission fitted (Midnight Drift prize).";
        }

        /// <summary>Forgets which vehicles were upgraded — used when the crew stands down.</summary>
        public void Reset()
        {
            _upgraded.Clear();
        }
    }
}
