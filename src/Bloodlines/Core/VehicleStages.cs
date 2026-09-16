using System;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Tuning past the last part the game sells.
    ///
    /// GTA's mod slots have fixed level counts — engine 0 to 3, transmission and brakes 0
    /// to 2, suspension 0 to 3 — and there is no top-speed slot at all, which is why no
    /// mod shop has ever sold you one. Engine raises power and transmission raises how
    /// fast the gears change; neither moves terminal velocity, which comes from handling
    /// data.
    ///
    /// So a stage here is ours, not a fake Rockstar part. It is bought after the game's own
    /// top part is fitted, it belongs to one owned car rather than to a model, and it is
    /// applied through per-entity natives every frame that car is in the world.
    ///
    /// **Two stages, not four, and the reason is worth keeping.** Power and top speed have
    /// per-entity natives: <c>SET_VEHICLE_CHEAT_POWER_INCREASE</c> and
    /// <c>SET_VEHICLE_MAX_SPEED</c> take a vehicle handle and affect that car alone.
    /// Braking and grip have none — the SDK offers <c>SET_VEHICLE_REDUCE_GRIP</c> and
    /// <c>SET_REDUCED_SUSPENSION_FORCE</c>, which only take capability away. Raising either
    /// means writing the model's shared handling data, and that reaches every instance of
    /// that model in the world including traffic. The rule against it is already in this
    /// project's own contracts, written after a per-car handling edit went wrong once.
    ///
    /// **Whether the terminal speed visibly moves is a road test.** This raises the entity
    /// cap and the torque; what the car then does against drag is the game's business, and
    /// nothing here should be described as a measured top speed.
    /// </summary>
    public static class VehicleStages
    {
        /// <summary>What a stage raises.</summary>
        public enum Stage
        {
            /// <summary>The car's own speed ceiling, above the one every car already gets.</summary>
            FinalDrive,
            /// <summary>Engine output past the last EMS upgrade the shop sells.</summary>
            Engine,
        }

        /// <summary>The game's mod slot whose top level has to be fitted first.</summary>
        public const int TransmissionSlot = 13, EngineSlot = 11;

        /// <summary>How much of the car's own ceiling a final drive adds.</summary>
        public const float DriveGain = .12f;
        /// <summary>How much engine output a stage adds over the fitted maximum.</summary>
        public const float PowerGain = .15f;

        public static string Name(Stage stage) =>
            stage == Stage.FinalDrive ? "Final drive - Stage 5" : "Engine - Stage 5";

        public static string Detail(Stage stage) =>
            stage == Stage.FinalDrive
                ? "Raises this car's own speed ceiling. Needs the top transmission fitted."
                : "Output past the last EMS upgrade. Needs the top engine fitted.";

        public static int Price(Stage stage) => stage == Stage.FinalDrive ? 14000 : 12000;

        /// <summary>The slot whose maximum has to be bought before the stage is offered.</summary>
        public static int Requires(Stage stage) => stage == Stage.FinalDrive ? TransmissionSlot : EngineSlot;

        public static int Level(OwnedVehicle car, Stage stage) =>
            car == null ? 0 : stage == Stage.FinalDrive ? car.StageDrive : car.StagePower;

        public static void Set(OwnedVehicle car, Stage stage, int level)
        {
            if (car == null) return;
            level = level < 0 ? 0 : level > 1 ? 1 : level;
            if (stage == Stage.FinalDrive) car.StageDrive = level; else car.StagePower = level;
        }

        /// <summary>
        /// Whether this car has earned the stage: the game's own part in the matching slot
        /// is at its highest index. A stage is the step after the shop runs out, not a
        /// shortcut past it.
        /// </summary>
        public static bool Eligible(Vehicle vehicle, Stage stage)
        {
            if (vehicle == null || !vehicle.Exists()) return false;
            try
            {
                int slot = Requires(stage);
                int count = Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, vehicle, slot);
                if (count <= 0) return false;
                return Function.Call<int>(Hash.GET_VEHICLE_MOD, vehicle, slot) >= count - 1;
            }
            catch (Exception ex) { Logger.Warn("Checking stage eligibility: " + ex.Message); return false; }
        }

        /// <summary>Why a stage cannot be bought yet, or null when it can.</summary>
        public static string Refusal(Vehicle vehicle, OwnedVehicle car, Stage stage)
        {
            if (vehicle == null || !vehicle.Exists()) return "No car here to work on.";
            if (car == null) return "A stage belongs to a car the crew owns. Store this one in a garage first.";
            if (Level(car, stage) > 0) return null;
            if (!Eligible(vehicle, stage))
                return "Fit the top " + (stage == Stage.FinalDrive ? "transmission" : "engine") + " first.";
            return null;
        }

        /// <summary>The multiplier a fitted final drive puts on this car's speed ceiling.</summary>
        public static float DriveFactor(int level) => 1f + DriveGain * Math.Max(0, Math.Min(1, level));
        /// <summary>The multiplier a fitted engine stage puts on this car's power.</summary>
        public static float PowerFactor(int level) => 1f + PowerGain * Math.Max(0, Math.Min(1, level));

        /// <summary>
        /// What the stages on one live car come to, for the tuning loop. Injected by the
        /// host from the garage records, so this module never has to know what a garage is
        /// and the tuning loop never has to know what a save file is.
        /// </summary>
        public static Func<Vehicle, Stage, int> Fitted;

        public static int FittedOn(Vehicle vehicle, Stage stage)
        {
            if (vehicle == null || !vehicle.Exists() || Fitted == null) return 0;
            try { return Fitted(vehicle, stage); }
            catch (Exception ex) { Logger.Warn("Reading a fitted stage: " + ex.Message); return 0; }
        }
    }
}
