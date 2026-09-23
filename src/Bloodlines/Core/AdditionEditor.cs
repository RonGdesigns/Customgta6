using System;
using System.Collections.Generic;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>
    /// The choices behind the survey editor's "Add to this mission" page: what is being
    /// placed, which model, how many, armed with what, on whose side, holding or patrolling.
    ///
    /// It decides nothing about the world. The menu reads it to draw its rows and to know
    /// which stand-in to hang in front of the camera, and hands it a position when the
    /// author drops something; this writes the addition down. Keeping it free of the camera
    /// and the ghost is what lets a test drive every choice without a game running.
    /// </summary>
    public sealed class AdditionEditor
    {
        public string Mission { get; private set; }
        public bool IsOpen => Mission != null;

        public AdditionKind Kind { get; private set; } = AdditionKind.Enemy;
        private int _enemy, _vehicle, _prop, _weapon = 2, _side;
        public int Count { get; private set; } = 2;
        public float Radius { get; private set; } = MissionAdditions.DefaultRadius;
        public bool Patrol { get; private set; }

        /// <summary>Changes whenever the stand-in has to be a different model; the menu respawns it on a change.</summary>
        public int PreviewVersion { get; private set; }

        public string Man => MissionAdditions.EnemyModels[_enemy];
        public string VehicleModel => MissionAdditions.VehicleModels[_vehicle];
        public string PropModel => MissionAdditions.PropModels[_prop];
        public string Weapon => MissionAdditions.Weapons[_weapon];
        public string Side => MissionAdditions.Sides[_side];

        /// <summary>The thing the stand-in shows: the man, the vehicle, or the prop.</summary>
        public string PreviewModel => Kind == AdditionKind.Vehicle ? VehicleModel : Kind == AdditionKind.Prop ? PropModel : Man;

        /// <summary>What a drop will write, in words, for the top row of the page.</summary>
        public string Describe()
        {
            switch (Kind)
            {
                case AdditionKind.Vehicle: return MissionAdditions.Label(VehicleModel) + ", " + Count + " aboard" + (Patrol ? ", patrolling" : "");
                case AdditionKind.Prop: return MissionAdditions.Label(PropModel);
                default: return Count + (Count == 1 ? " man" : " men") + " with " + Weapon + (Patrol ? ", patrolling" : "");
            }
        }

        public void Open(string mission)
        {
            Mission = string.IsNullOrWhiteSpace(mission) ? null : mission.Trim();
            PreviewVersion++;
        }

        public void Close() { Mission = null; }

        public void CycleKind(int delta)
        {
            var kinds = (AdditionKind[])Enum.GetValues(typeof(AdditionKind));
            Kind = kinds[Wrap(Array.IndexOf(kinds, Kind) + delta, kinds.Length)];
            PreviewVersion++;
        }

        /// <summary>The next model of whatever is being placed. This is the "load a different prop" button.</summary>
        public void CycleModel(int delta)
        {
            switch (Kind)
            {
                case AdditionKind.Vehicle: _vehicle = Wrap(_vehicle + delta, MissionAdditions.VehicleModels.Length); break;
                case AdditionKind.Prop: _prop = Wrap(_prop + delta, MissionAdditions.PropModels.Length); break;
                default: _enemy = Wrap(_enemy + delta, MissionAdditions.EnemyModels.Length); break;
            }
            PreviewVersion++;
        }

        /// <summary>Who rides in a vehicle. The stand-in is the vehicle, so this does not respawn it.</summary>
        public void CycleCrew(int delta) { _enemy = Wrap(_enemy + delta, MissionAdditions.EnemyModels.Length); if (Kind == AdditionKind.Enemy) PreviewVersion++; }
        public void CycleWeapon(int delta) { _weapon = Wrap(_weapon + delta, MissionAdditions.Weapons.Length); }
        public void CycleSide(int delta) { _side = Wrap(_side + delta, MissionAdditions.Sides.Length); }
        public void AdjustCount(int delta) { Count = Math.Max(1, Math.Min(MissionAdditions.MaxCount, Count + delta)); }
        public void AdjustRadius(float delta) { Radius = Math.Max(MissionAdditions.MinRadius, Math.Min(MissionAdditions.MaxRadius, Radius + delta)); }
        public void TogglePatrol() { Patrol = !Patrol; }

        public IReadOnlyList<Addition> Placed => IsOpen ? MissionAdditions.For(Mission) : new List<Addition>();

        /// <summary>
        /// Write down what is described, at this spot, facing this way, and save the file.
        /// Null when nothing was written: no mission open, the mission is full, or the
        /// file could not be saved.
        /// </summary>
        public Addition Drop(Vector3 at, float heading)
        {
            if (!IsOpen) return null;
            var addition = new Addition
            {
                Mission = Mission, Kind = Kind, Position = at, Heading = Normalize(heading),
                Model = Kind == AdditionKind.Prop ? PropModel : Man,
                Vehicle = Kind == AdditionKind.Vehicle ? VehicleModel : "",
                Count = Kind == AdditionKind.Prop ? 1 : Count,
                Radius = Kind == AdditionKind.Enemy ? Radius : 0f,
                Weapon = Weapon, Side = Side,
                Patrol = Kind != AdditionKind.Prop && Patrol,
            };
            return MissionAdditions.Add(addition) ? addition : null;
        }

        private static int Wrap(int value, int length)
        {
            if (length <= 0) return 0;
            value %= length;
            return value < 0 ? value + length : value;
        }

        private static float Normalize(float degrees)
        {
            degrees %= 360f;
            return degrees < 0f ? degrees + 360f : degrees;
        }
    }
}
