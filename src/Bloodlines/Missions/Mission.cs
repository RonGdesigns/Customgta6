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
        private readonly List<Blip> _blips = new List<Blip>();

        protected MissionContext Ctx { get; private set; }

        public abstract string Id { get; }
        public abstract string Title { get; }

        public MissionStatus Status { get; private set; } = MissionStatus.Idle;

        public string FailReason { get; private set; }

        protected int Stage { get; private set; }

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
                OnUpdate();
            }
            catch (Exception ex)
            {
                Logger.Error("Mission " + Id + " threw during update", ex);
                Fail("Script error — see Bloodlines.log");
            }
        }

        protected void Advance()
        {
            Stage++;
            StageStartedAt = Game.GameTime;
            Logger.Debug(Id + " -> stage " + Stage);
        }

        protected void GoToStage(int stage)
        {
            Stage = stage;
            StageStartedAt = Game.GameTime;
            Logger.Debug(Id + " -> stage " + stage);
        }

        protected void Objective(string text)
        {
            GameUtils.Subtitle("~y~" + text, 5000);
        }

        public void Pass()
        {
            if (Status != MissionStatus.Running) return;
            Status = MissionStatus.Passed;
            Logger.Info("Mission passed: " + Id);
            Cleanup();
        }

        public void Fail(string reason)
        {
            if (Status != MissionStatus.Running) return;
            Status = MissionStatus.Failed;
            FailReason = reason;
            Logger.Info("Mission failed: " + Id + " — " + reason);
            Cleanup();
        }

        public void Abort()
        {
            if (Status != MissionStatus.Running) return;
            Status = MissionStatus.Aborted;
            Logger.Info("Mission aborted by player: " + Id);
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

        public void Cleanup()
        {
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
                GameUtils.SafeDelete(entity);
            }
            _entities.Clear();

            Ctx?.Switching?.SetUnlocked();
        }

        /// <summary>Spawn the world, set the first objective. Return false to reject the start.</summary>
        protected abstract bool OnStart();

        /// <summary>Called every tick while running. Keep it non-blocking.</summary>
        protected abstract void OnUpdate();

        /// <summary>Undo anything the mission changed that is not a tracked entity.</summary>
        protected virtual void OnCleanup()
        {
        }
    }
}
