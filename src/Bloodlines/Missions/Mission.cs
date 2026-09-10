using System;
using System.Collections.Generic;
using Bloodlines.Core;
using GTA;

namespace Bloodlines.Missions
{
    public enum MissionStatus
    {
        Idle,
        Running,
        Passed,
        Failed,
        Aborted
    }

    /// <summary>
    /// Base class for every campaign mission.
    ///
    /// A mission is a stage machine driven off the mod's tick — not one long
    /// blocking routine — so the player can always switch character, pause or
    /// abort. Every entity a mission spawns is registered with <see cref="Track"/>
    /// and torn down on pass, fail, abort or mod reload; leaked mission peds are
    /// the classic way a script mod turns someone's save into a warzone.
    /// </summary>
    public abstract class Mission
    {
        private readonly List<Entity> _entities = new List<Entity>();
        private readonly Dictionary<Ped, string> _survivors = new Dictionary<Ped, string>();
        private readonly Dictionary<Entity, string> _requiredAssets = new Dictionary<Entity, string>();
        protected void RequireSurvivor(Ped ped, string reason) { if (ped != null) _survivors[ped] = reason; }

        /// <summary>
        /// A vehicle or prop the story needs for the whole mission, not just the stage
        /// that happens to be protecting it. Losing it fails the attempt immediately
        /// with a reason, instead of a later objective waiting forever on a wreck.
        /// </summary>
        protected void RequireAsset(Entity entity, string reason) { if (entity != null) _requiredAssets[entity] = reason; }

        private static bool AssetLost(Entity entity)
        {
            if (entity == null || !entity.Exists() || entity.IsDead) return true;
            return entity is Vehicle vehicle && !vehicle.IsDriveable;
        }
        private readonly List<Blip> _blips = new List<Blip>();

        protected MissionContext Ctx { get; private set; }

        public abstract string Id { get; }
        public abstract string Title { get; }

        public MissionStatus Status { get; private set; } = MissionStatus.Idle;

        public string FailReason { get; private set; }

        // Opt in only when the mission rebuilds entities, vehicles and objective state.
        public virtual bool SupportsCheckpointRestore => false;

        protected int Stage { get; private set; }

        /// <summary>Stage index, for the checkpoint manager and the QA harness.</summary>
        public int CurrentStage => Stage;
        public string CurrentObjective { get; protected set; } = "";
        public Crew.CrewSlot? RequiredSwitch { get; protected set; }

        protected int StageStartedAt { get; private set; }

        protected int SecondsInStage => (Game.GameTime - StageStartedAt) / 1000;

        public bool Begin(MissionContext context)
        {
            Ctx = context;
            Stage = 0;
            StageStartedAt = Game.GameTime;
            FailReason = null;

            try
            {
                if (!OnStart())
                {
                    Cleanup();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Mission " + Id + " failed to start", ex);
                Cleanup();
                return false;
            }

            Status = MissionStatus.Running;
            Logger.Info("Mission started: " + Id + " — " + Title);
            return true;
        }

        public void Tick()
        {
            if (Status != MissionStatus.Running) return;

            try
            {
                foreach (var survivor in _survivors)
                    if (!survivor.Key.Exists() || survivor.Key.IsDead) { Fail(survivor.Value); return; }
                foreach (var asset in _requiredAssets)
                    if (AssetLost(asset.Key)) { Fail(asset.Value); return; }
                OnUpdate();
            }
            catch (Exception ex)
            {
                Logger.Error("Mission " + Id + " threw during update", ex);
                Fail("Script error — see Bloodlines.log");
            }
        }

        /// <summary>
        /// Moves to the next stage, commits a checkpoint, and queues the bible's
        /// dialogue for that stage. Stage numbering follows the cue ids (S1, S2, …),
        /// so stage 0 in code is the mission's S1 block.
        /// </summary>
        protected void Advance()
        {
            Stage++;
            StageStartedAt = Game.GameTime;
            Logger.Debug(Id + " -> stage " + Stage);
            Ctx?.Checkpoints?.Commit(Id, Stage);
        }

        protected void GoToStage(int stage)
        {
            Stage = stage;
            StageStartedAt = Game.GameTime;
            Logger.Debug(Id + " -> stage " + stage);
        }

        protected void Objective(string text)
        {
            CurrentObjective = text;
            GameUtils.Subtitle("~y~" + text, 5000);
        }

        /// <summary>Fires one written line from the bible, by its cue id.</summary>
        protected void Say(string cueId)
        {
            Ctx?.Dialogue?.Play(cueId);
        }

        /// <summary>Fires every line the bible assigns to a stage, in order.</summary>
        protected void SayStage(int stage)
        {
            Ctx?.Dialogue?.PlayStage(Id, stage);
        }

        /// <summary>
        /// Jumps to a stage after a checkpoint restore or a QA warp. Missions that
        /// need to rebuild world state for a stage override <see cref="OnStageEntered"/>.
        /// </summary>
        public void JumpToStage(int stage)
        {
            if (Status != MissionStatus.Running) return;

            GoToStage(stage);
            try
            {
                OnStageEntered(stage);
            }
            catch (Exception ex)
            {
                Logger.Error("Mission " + Id + " failed entering stage " + stage, ex);
            }
        }

        public void Pass()
        {
            if (Status != MissionStatus.Running) return;
            Status = MissionStatus.Passed;
            Logger.Info("Mission passed: " + Id);
            // Runs while the world still exists, so a chapter can record where its
            // vehicles, cargo and people are before cleanup takes them.
            try { OnPassed(); }
            catch (Exception ex) { Logger.Error("Mission " + Id + " handoff record failed", ex); }
            Cleanup();
        }

        /// <summary>Called on pass before cleanup. Override to hand state to the next chapter.</summary>
        protected virtual void OnPassed()
        {
        }

        /// <summary>QA: complete the objective the player is on, through its own completion, so stage exit effects run. Returns its label, or null.</summary>
        public virtual string CompleteCurrentObjective() => null;

        public void Fail(string reason)
        {
            if (Status != MissionStatus.Running) return;
            Status = MissionStatus.Failed;
            FailReason = reason;
            Logger.Info("Mission failed: " + Id + " — " + reason);
            Ctx?.Dialogue?.Clear();
            Cleanup();
        }

        public void Abort()
        {
            if (Status != MissionStatus.Running) return;
            Status = MissionStatus.Aborted;
            Logger.Info("Mission aborted by player: " + Id);
            Ctx?.Dialogue?.Clear();
            Cleanup();
        }

        protected T Track<T>(T entity) where T : Entity
        {
            if (entity != null) _entities.Add(entity);
            return entity;
        }

        protected Blip Track(Blip blip)
        {
            if (blip != null) _blips.Add(blip);
            return blip;
        }

        /// <summary>Hands an entity back to the world — it survives mission teardown.</summary>
        protected void Release(Entity entity)
        {
            _entities.Remove(entity);
            GameUtils.SafeRelease(entity);
        }

        protected virtual void StopObjectives() { }
        public void Cleanup()
        {
            try { StopObjectives(); } catch (Exception ex) { Logger.Error(Id + " objective shutdown", ex); }
            ObjectiveMarkers.Clear();
            try { Ctx?.Cutscenes?.Stop(); } catch (Exception ex) { Logger.Error(Id + " scene shutdown", ex); }
            try
            {
                OnCleanup();
            }
            catch (Exception ex)
            {
                Logger.Error("Mission " + Id + " cleanup threw", ex);
            }

            foreach (var blip in _blips) GameUtils.SafeDelete(blip);
            _blips.Clear();

            var playerPed = Game.Player.Character;
            foreach (var entity in _entities)
            {
                if (entity == null || !entity.Exists()) continue;
                if (playerPed != null && entity.Handle == playerPed.Handle) continue;
                // A passed flight/boat mission must not delete the transport under
                // the player or companions before its aftermath starts. Hand occupied
                // transports back to the world; the engine can reclaim them later.
                if (entity is Vehicle vehicle)
                {
                    bool occupied = playerPed != null && playerPed.Exists() && playerPed.IsInVehicle(vehicle);
                    if (Ctx?.Crew != null)
                        foreach (var protagonist in Crew.Protagonist.All)
                        {
                            var ped = Ctx.Crew.PedFor(protagonist.Slot);
                            if (ped != null && ped.Exists() && ped.IsInVehicle(vehicle)) occupied = true;
                        }
                    if (occupied) { GameUtils.SafeRelease(entity); continue; }
                }
                if (entity.IsDead) GameUtils.SafeRelease(entity);
                else GameUtils.SafeDelete(entity);
            }
            _entities.Clear();
            _survivors.Clear();
            _requiredAssets.Clear();

            if (Ctx?.Crew != null)
            {
                Ctx.Crew.CompanionAI.ReleaseAll();
                Ctx.Crew.CompanionsHoldPosition = false;
                Ctx.Crew.AssignCompanionAI();
            }
            Ctx?.Switching?.SetUnlocked();
        }

        /// <summary>This mission's entry in the bible, or null if the data is missing.</summary>
        protected MissionInfo Info
        {
            get
            {
                return Ctx?.Data?.Mission(Id);
            }
        }

        /// <summary>
        /// Applies the time of day and weather the bible specifies for this mission.
        /// Setting is not decoration here: half these missions are written around
        /// darkness, fog or rain doing the concealment work.
        /// </summary>
        protected void ApplyBibleSetting()
        {
            var info = Info;
            if (info == null) return;

            info.ParseClock(out int hour, out int minute);
            if (hour >= 0) GameUtils.SetClock(hour, minute);

            GameUtils.SetWeather(info.Weather);
            Logger.Debug(Id + " setting: " + info.Time + " / " + info.Weather);
        }

        /// <summary>Spawn the world, set the first objective. Return false to reject the start.</summary>
        protected abstract bool OnStart();

        /// <summary>Called every tick while running. Keep it non-blocking.</summary>
        protected abstract void OnUpdate();

        /// <summary>Undo anything the mission changed that is not a tracked entity.</summary>
        protected virtual void OnCleanup()
        {
        }

        /// <summary>
        /// Called when a stage is entered out of sequence — a checkpoint restore or a
        /// QA stage warp. Rebuild whatever that stage assumes exists.
        /// </summary>
        protected virtual void OnStageEntered(int stage)
        {
        }
    }
}
