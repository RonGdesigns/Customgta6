using System;
using System.Drawing;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Ron's arrival: the campaign's first playable minutes, before M01.
    ///
    /// LSIA curb → Ron checks his phone, walks to the car he was told would be
    /// waiting, gets in → control returns with him already seated → the player
    /// drives across the city to his starter apartment → he gets out, walks to the
    /// door, reads the Terminal Island job → time-cut to M01's cold open.
    ///
    /// Stock-only by design: the Guess ped, a Primo (the same car M01's intro puts
    /// him in), the terminal frontage, and the apartment entrance the home system
    /// already owns. The moving parts are <see cref="SceneBlocking"/> steps, so
    /// skipping either scene leaves Ron exactly where watching it would.
    ///
    /// Not a Mission: it has no failure state worth a retry menu. Losing the car
    /// only means finding another ride home, and dying hands off to the ordinary
    /// solo recovery, after which the drive simply continues.
    /// </summary>
    public sealed class PrologueSequence
    {
        public enum Phase { Idle, Arrival, Drive, Homecoming, Finished }

        private const string CarModel = "primo";
        private const float ArriveRadius = 9f;

        private readonly CrewRoster _crew;
        private readonly CutsceneDirector _cutscenes;
        private readonly LocationBook _locations;
        private readonly CampaignState _state;
        private readonly Func<Vector3?> _home;

        private Vehicle _car;
        private Blip _carBlip;
        private Vector3 _homePoint;
        private int _lastHint;
        private bool _carWarned;

        public PrologueSequence(CrewRoster crew, CutsceneDirector cutscenes, LocationBook locations, CampaignState state, Func<Vector3?> home)
        {
            _crew = crew;
            _cutscenes = cutscenes;
            _locations = locations;
            _state = state;
            _home = home;
        }

        public Phase Current { get; private set; } = Phase.Idle;
        public bool IsActive => Current != Phase.Idle && Current != Phase.Finished;

        /// <summary>Runs once the arrival scene has ended (or was skipped) — the host starts M01.</summary>
        public Action Finished { get; set; }

        public Vehicle Car => _car;

        /// <summary>
        /// Deploys Ron alone at the airport and plays the arrival. False means the
        /// airport or home locations could not be resolved; the caller falls back to
        /// starting M01 the ordinary way.
        /// </summary>
        public bool Begin()
        {
            if (IsActive) return false;
            var home = _home?.Invoke();
            if (!home.HasValue)
            {
                Logger.Error("Prologue: Guess's home location is missing; starting M01 without the arrival.");
                return false;
            }
            if (!MissionSites.Ground(_locations, "Prologue.LSIACurb", "Prologue.LSIACar")) return false;

            var curb = _locations.Get("Prologue.LSIACurb");
            var spot = _locations.Get("Prologue.LSIACar");
            if (!_crew.DeploySolo(CrewSlot.Guess, curb.Position, curb.Heading)) return false;

            var guess = Game.Player.Character;
            _car = SpawnCar(spot.Position, spot.Heading);
            if (_car == null)
            {
                Logger.Error("Prologue: the arrival car could not be created.");
                Cancel();
                return false;
            }
            _homePoint = home.Value;
            _carWarned = false;
            GameUtils.SetClock(23, 10);
            GameUtils.SetWeather("Clear");

            // Ron stops at the curb, checks the phone, then walks to the car and
            // gets in. Control comes back with him in the driver's seat whether the
            // player watched every line or skipped on the first.
            var doorSide = _car.Position + LeftOf(_car) * 1.9f;
            var blocking = new SceneBlocking()
                .Then(new WaitStep(1200, guess))
                .Then(new UsePhoneStep(guess, 4200))
                .Then(new LookAtStep(guess, _car, 900))
                .Then(new WalkToStep(guess, doorSide, 1.6f))
                .Then(new EnterVehicleStep(guess, _car, VehicleSeat.Driver));

            Current = Phase.Arrival;
            if (!_cutscenes.Play("M01", "prologue", "Los Santos", null, null, blocking))
            {
                Logger.Warn("Prologue: arrival scene unavailable; placing Ron in the car directly.");
                blocking.Complete();
            }
            Logger.Info("Prologue started at LSIA; home is " + _homePoint + ".");
            return true;
        }

        public void Update()
        {
            if (!IsActive || _cutscenes.IsActive) return;
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;

            switch (Current)
            {
                case Phase.Arrival:
                    Current = Phase.Drive;
                    GameUtils.Notify("~o~Drive home.~s~ The apartment is marked on the map.");
                    break;

                case Phase.Drive:
                    UpdateDrive(player);
                    break;

                case Phase.Homecoming:
                    Finish();
                    break;
            }
        }

        private void UpdateDrive(Ped player)
        {
            bool carAlive = _car != null && _car.Exists() && _car.IsDriveable;
            if (!carAlive && !_carWarned)
            {
                _carWarned = true;
                GameUtils.Notify("~y~The car is done. Any ride home will do.");
                GameUtils.SafeDelete(_carBlip);
                _carBlip = null;
            }
            var ride = player.CurrentVehicle;
            ObjectiveMarkers.Navigation(_homePoint, null, ride != null && ride.Exists() ? ride : null);
            GameUtils.DrawObjectiveMarker(_homePoint, Color.FromArgb(140, 240, 205, 60), 3f);
            if (Game.GameTime - _lastHint > 4000)
            {
                _lastHint = Game.GameTime;
                GameUtils.Subtitle(ride != null ? "~y~Drive to your apartment." : "~y~Get back in the car and drive to your apartment.", 3500);
            }

            if (!GameUtils.IsWithinFlat(player.Position, _homePoint, ArriveRadius)) return;
            if (ride != null && ride.Exists() && ride.Speed > 1.5f) return;

            // Home: get out, walk to the door, read the job. Same end state on skip.
            var blocking = new SceneBlocking();
            if (ride != null && ride.Exists()) blocking.Then(new ExitVehicleStep(player));
            blocking.Then(new WalkToStep(player, _homePoint, 1.4f)).Then(new UsePhoneStep(player, 3200));
            Current = Phase.Homecoming;
            if (!_cutscenes.Play("M01", "arrival", "Home", null, null, blocking))
            {
                Logger.Warn("Prologue: homecoming scene unavailable; finishing its blocking directly.");
                blocking.Complete();
            }
        }

        /// <summary>Marks the arrival played and hands the campaign to M01's cold open.</summary>
        private void Finish()
        {
            Current = Phase.Finished;
            _state.PrologueComplete = true;
            _state.Save();
            ReleaseCar();
            ObjectiveMarkers.Clear();
            Logger.Info("Prologue complete; handing off to M01.");
            try { Finished?.Invoke(); }
            catch (Exception ex) { Logger.Error("Prologue handoff to M01 failed", ex); }
            Current = Phase.Idle;
        }

        /// <summary>The abort hold: skip the arrival entirely but still count it as played.</summary>
        public void Skip()
        {
            if (!IsActive) return;
            Logger.Info("Prologue skipped by the player.");
            _cutscenes.Skip();
            Finish();
        }

        /// <summary>Tear down without completing — mod reload or a failed start.</summary>
        public void Cancel()
        {
            if (Current == Phase.Idle) return;
            _cutscenes.Stop();
            if (_car != null && _car.Exists()) GameUtils.SafeDelete(_car);
            _car = null;
            GameUtils.SafeDelete(_carBlip);
            _carBlip = null;
            ObjectiveMarkers.Clear();
            Current = Phase.Idle;
        }

        private void ReleaseCar()
        {
            GameUtils.SafeDelete(_carBlip);
            _carBlip = null;
            if (_car != null && _car.Exists()) GameUtils.SafeRelease(_car);
            _car = null;
        }

        private Vehicle SpawnCar(Vector3 position, float heading)
        {
            var model = new Model(CarModel);
            if (!GameUtils.RequestModel(model)) return null;
            var car = World.CreateVehicle(model, position, heading);
            model.MarkAsNoLongerNeeded();
            if (car == null || !car.Exists()) return null;
            car.IsPersistent = true;
            car.PlaceOnGround();
            _carBlip = car.AddBlip();
            if (_carBlip != null)
            {
                _carBlip.Sprite = BlipSprite.PersonalVehicleCar;
                _carBlip.Color = BlipColor.Orange;
                _carBlip.Name = "Ron's car";
            }
            return car;
        }

        private static Vector3 LeftOf(Vehicle vehicle)
        {
            var forward = vehicle.ForwardVector;
            return new Vector3(-forward.Y, forward.X, 0f);
        }
    }
}
