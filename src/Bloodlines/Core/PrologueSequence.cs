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
    /// drives across the city to his starter apartment → he gets out and walks to
    /// the door → inside, the message with the Terminal Island job arrives and he
    /// reads it → time-cut to M01's cold open. If the room never loads, the job is
    /// read at the door instead and the cut still happens.
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
        public enum Phase { Idle, Arrival, Drive, Homecoming, Interior, Finished }

        private const string CarModel = "primo";
        private const float ArriveRadius = 9f;

        private readonly CrewRoster _crew;
        private readonly CutsceneDirector _cutscenes;
        private readonly LocationBook _locations;
        private readonly CampaignState _state;
        private readonly Func<Vector3?> _home;
        private readonly ApartmentAccess _apartment;

        private Vehicle _car;
        private bool _callStarted;
        private Blip _carBlip;
        private Vector3 _homePoint;
        private int _lastHint;
        private bool _carWarned;
        private bool _sceneStarted, _finished;
        private int _homecomingFailures;

        /// <summary>Result of <see cref="PlaceForColdOpen"/>. Only Placed means the player is at the dock.</summary>
        public enum Placement { Placed, StillSeated, NoGround }

        /// <param name="apartment">The home system's room access; null plays the job at the door with no interior.</param>
        public PrologueSequence(CrewRoster crew, CutsceneDirector cutscenes, LocationBook locations, CampaignState state, Func<Vector3?> home, ApartmentAccess apartment = null)
        {
            _crew = crew;
            _cutscenes = cutscenes;
            _locations = locations;
            _state = state;
            _home = home;
            _apartment = apartment;
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
            _finished = false;
            _homecomingFailures = 0;
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
            _sceneStarted = _cutscenes.Play("M01", "prologue", "Los Santos", null, null, blocking);
            if (!_sceneStarted)
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
                    // However the scene ended, the drive is self-healing: a Ron who is
                    // not in the car is told to get back in it. Only the log records
                    // that the arrival did not play out.
                    if (_sceneStarted && _cutscenes.LastOutcome != SceneOutcome.Completed && _cutscenes.LastOutcome != SceneOutcome.Skipped)
                        Logger.Warn("Prologue: arrival scene ended by " + _cutscenes.LastOutcome + "; the drive continues from where Ron stands.");
                    _sceneStarted = false;
                    Current = Phase.Drive;
                    GameUtils.Notify("~o~Drive home.~s~ The apartment is marked on the map.");
                    break;

                case Phase.Drive:
                    UpdateDrive(player);
                    break;

                case Phase.Homecoming:
                    // A scene that was canceled or failed is not a homecoming. Go back
                    // to the drive; arriving at the door again retries, and the second
                    // attempt places Ron directly rather than trusting the scene.
                    if (_sceneStarted && _cutscenes.LastOutcome != SceneOutcome.Completed && _cutscenes.LastOutcome != SceneOutcome.Skipped)
                    {
                        _homecomingFailures++;
                        _sceneStarted = false;
                        Logger.Warn("Prologue: homecoming scene ended by " + _cutscenes.LastOutcome + "; the drive resumes at the door (attempt " + _homecomingFailures + ").");
                        Current = Phase.Drive;
                        _lastHint = 0;
                        break;
                    }
                    _sceneStarted = false;
                    BeginInterior();
                    break;

                case Phase.Interior:
                    UpdateInterior(player);
                    break;
            }
        }

        /// <summary>
        /// Through the door: the home system streams the starter room with Ron held
        /// behind a fade, and the message arrives once he is inside. No room, or no
        /// home system at all, and the job is read where he stands.
        /// </summary>
        private void BeginInterior()
        {
            var room = _apartment == null ? null : _locations.Get("Apartment.Starter.Interior");
            if (room == null || _apartment.Inside || _apartment.Busy)
            {
                if (_apartment != null && room == null) Logger.Warn("Prologue: no starter interior location; the job is read at the door.");
                PlayCall(atDoor: true);
                return;
            }
            if (!_apartment.Begin(room.Position, null, true))
            {
                Logger.Warn("Prologue: the apartment could not be entered; the job is read at the door.");
                PlayCall(atDoor: true);
                return;
            }
            Current = Phase.Interior;
            _callStarted = false;
        }

        private void UpdateInterior(Ped player)
        {
            if (_apartment != null && _apartment.Busy) return;
            if (!_callStarted)
            {
                if (_apartment == null || !_apartment.Inside)
                {
                    // Entry timed out: the home system already returned control at the
                    // door, so the message is read there.
                    Logger.Warn("Prologue: the apartment did not load; the job is read at the door.");
                    GameUtils.Notify("~y~The apartment did not load.~s~ The job reads at the door.");
                    PlayCall(atDoor: true);
                    return;
                }
                PlayCall(atDoor: false);
                return;
            }
            // The scene has ended; the host does not tick the prologue while one runs.
            if (_sceneStarted && _cutscenes.LastOutcome != SceneOutcome.Completed && _cutscenes.LastOutcome != SceneOutcome.Skipped)
                Logger.Warn("Prologue: the call scene ended by " + _cutscenes.LastOutcome + "; the job counts as read.");
            _sceneStarted = false;
            Finish();
        }

        /// <summary>
        /// The message. Inside: Ron crosses the room, stops, the phone comes out, and
        /// the last line lands on his face. At the door: the phone only. Skipping
        /// puts the phone away on the same mark. Whatever ends the scene, the job
        /// counts as read; the host cuts to the dock from wherever he stands.
        /// </summary>
        private void PlayCall(bool atDoor)
        {
            _callStarted = true;
            Current = Phase.Interior;
            var guess = Game.Player.Character;
            var blocking = new SceneBlocking();
            if (!atDoor)
            {
                var across = guess.Position + guess.ForwardVector * 2f;
                blocking.DialogueAfterStep = 2;
                blocking.Then(new WalkToStep(guess, across, 0.9f)).Then(new WaitStep(1400, guess));
            }
            blocking.Then(new UsePhoneStep(guess, 4200))
                .Then(new ShotStep(3600, guess, new Vector3(1.7f, 0.5f, 1.55f), guess, new Vector3(0f, 0f, 1.45f), -0.3f));
            _sceneStarted = _cutscenes.Play("M01", "call", "The job", null, null, blocking);
            if (_sceneStarted) return;
            Logger.Warn("Prologue: the call scene is unavailable; the job counts as read.");
            blocking.Complete();
            Finish();
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
            // After a canceled or failed homecoming the scene is not trusted again:
            // the end state is placed directly, and if even that cannot unseat Ron
            // the drive keeps asking him to get out at the door.
            bool direct = _homecomingFailures > 0;
            _sceneStarted = !direct && _cutscenes.Play("M01", "arrival", "Home", null, null, blocking);
            if (!_sceneStarted)
            {
                if (!direct) Logger.Warn("Prologue: homecoming scene unavailable; finishing its blocking directly.");
                blocking.Complete();
                if (!blocking.Succeeded)
                {
                    Logger.Warn("Prologue: Ron could not be placed at the door; the drive waits for him to get out.");
                    GameUtils.Subtitle("~y~Get out at the apartment door.", 3500);
                    _lastHint = Game.GameTime;
                    return;
                }
            }
            Current = Phase.Homecoming;
        }

        /// <summary>Marks the arrival played and hands the campaign to M01's cold open.</summary>
        private void Finish()
        {
            if (_finished) return;
            _finished = true;
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
            if (_apartment != null && (_apartment.Inside || _apartment.Busy)) _apartment.Cancel();
            if (_car != null && _car.Exists()) GameUtils.SafeDelete(_car);
            _car = null;
            GameUtils.SafeDelete(_carBlip);
            _carBlip = null;
            ObjectiveMarkers.Clear();
            Current = Phase.Idle;
        }

        /// <summary>
        /// Put the player at the dock for M01's cold open: out of any vehicle first,
        /// deterministically, then repositioned with collision confirmed. Nothing is
        /// moved unless both halves succeed: a player the engine will not unseat
        /// stays exactly where they are, and a dock that never streams collision
        /// puts them back where they started. The caller decides what to say.
        /// </summary>
        public static Placement PlaceForColdOpen(Ped player, Vector3 dock)
        {
            if (player == null || !player.Exists()) return Placement.NoGround;
            if (!ExitVehicleStep.ForceOut(player))
            {
                Logger.Warn("Prologue: the player could not be unseated before the cold open; nothing was moved.");
                return Placement.StillSeated;
            }
            var origin = player.Position;
            float heading = player.Heading;
            Function.Call(Hash.REQUEST_COLLISION_AT_COORD, dock.X, dock.Y, dock.Z);
            player.Position = dock;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, dock.X, dock.Y, dock.Z);
                if (Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, player)) return Placement.Placed;
                Script.Wait(100);
            }
            Logger.Warn("Prologue: no collision streamed in at the dock; the player was returned to " + origin + ".");
            player.Position = origin;
            player.Heading = heading;
            return Placement.NoGround;
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
