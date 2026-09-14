using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;

namespace Bloodlines.Missions
{
    /// <summary>What MissionManager needs to know without knowing a specific heist class.</summary>
    public interface IContinuousOperation
    {
        string OperationTitle { get; }
        string PhaseId { get; }
        string FinalPhaseId { get; }
        bool ContainsPhase(string id);
        void CommitResult(MissionCatalog catalog);
    }

    /// <summary>
    /// One attempt's shared world for an uninterrupted operation. Entity references
    /// are session-only; nothing here is serialized. Phases can bind named entities
    /// and scalar values, and a replacement entity with the same key is refused.
    /// </summary>
    public class ContinuousOperationState
    {
        private readonly MissionContext _context;
        private readonly Dictionary<int, Entity> _owned = new Dictionary<int, Entity>();
        private readonly HashSet<int> _keep = new HashSet<int>();
        private readonly Dictionary<string, Entity> _named = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, float> _power = new Dictionary<int, float>();
        private readonly Dictionary<int, bool> _invincible = new Dictionary<int, bool>();
        private bool _disposed;

        public ContinuousOperationState(string operationId, MissionContext context)
        {
            OperationId = string.IsNullOrWhiteSpace(operationId) ? throw new ArgumentException("Operation id is required.", nameof(operationId)) : operationId;
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public string OperationId { get; }
        public string PhaseId { get; internal set; }
        public bool Continuing { get; internal set; }
        public int OwnedCount => _owned.Count;
        public IEnumerable<string> BoundKeys => _named.Keys;

        public void Own(Entity entity)
        {
            if (entity == null || !entity.Exists() || _owned.ContainsKey(entity.Handle)) return;
            _owned[entity.Handle] = entity;
            _invincible[entity.Handle] = entity.IsInvincible;
            if (entity is Vehicle vehicle)
            {
                var property = typeof(Vehicle).GetProperty("EnginePowerMultiplier");
                _power[entity.Handle] = property != null && property.CanRead ? Convert.ToSingle(property.GetValue(vehicle)) : 1f;
            }
            entity.IsPersistent = true;
        }

        public bool Owns(Entity entity) => entity != null && _owned.ContainsKey(entity.Handle);

        public void KeepAfterSuccess(Entity entity)
        {
            if (entity == null) return;
            Own(entity);
            _keep.Add(entity.Handle);
        }

        public T Bind<T>(string key, T entity) where T : Entity
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Binding key is required.", nameof(key));
            if (entity == null || !entity.Exists() || entity.IsDead)
                throw new InvalidOperationException(OperationId + ": required " + key + " is unavailable.");
            if (_named.TryGetValue(key, out var previous) && previous.Handle != entity.Handle)
                throw new InvalidOperationException(OperationId + ": attempted to replace live " + key + ".");
            Own(entity);
            _named[key] = entity;
            return entity;
        }

        public T Get<T>(string key) where T : Entity =>
            key != null && _named.TryGetValue(key, out var entity) ? entity as T : null;

        public T Require<T>(string key) where T : Entity
        {
            var entity = Get<T>(key);
            if (entity == null || !entity.Exists() || entity.IsDead || entity is Vehicle vehicle && !vehicle.IsDriveable)
                throw new InvalidOperationException(OperationId + ": the " + key + " was lost. Restart the entire operation.");
            return entity;
        }

        public void SetValue<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Value key is required.", nameof(key));
            _values[key] = value;
        }

        public bool TryValue<T>(string key, out T value)
        {
            if (key != null && _values.TryGetValue(key, out var raw) && raw is T typed)
            { value = typed; return true; }
            value = default(T);
            return false;
        }

        public void Dispose(bool successful)
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var entity in _owned.Values.Reverse())
            {
                try
                {
                    if (entity == null || !entity.Exists()) continue;
                    var player = Game.Player.Character;
                    if (player != null && player.Exists() && player.Handle == entity.Handle) continue;
                    if (_context.Crew != null && Protagonist.All.Any(h => _context.Crew.PedFor(h.Slot)?.Handle == entity.Handle)) continue;
                    if (entity is Vehicle vehicle)
                    {
                        vehicle.IsPositionFrozen = false;
                        if (_invincible.TryGetValue(vehicle.Handle, out bool protectedBefore)) vehicle.IsInvincible = protectedBefore;
                        if (_power.TryGetValue(vehicle.Handle, out float power)) vehicle.EnginePowerMultiplier = power;
                        bool occupied = player != null && player.Exists() && player.IsInVehicle(vehicle) ||
                            _context.Crew != null && Protagonist.All.Any(h => _context.Crew.PedFor(h.Slot)?.IsInVehicle(vehicle) == true);
                        if (occupied || successful && _keep.Contains(entity.Handle) || entity.IsDead)
                        { GameUtils.SafeRelease(entity); continue; }
                    }
                    if (successful && _keep.Contains(entity.Handle) || entity.IsDead) GameUtils.SafeRelease(entity);
                    else GameUtils.SafeDelete(entity);
                }
                catch (Exception ex) { Logger.Error(OperationId + " world cleanup", ex); }
            }
            _owned.Clear();
            _keep.Clear();
            _named.Clear();
            // Mission.Pass cleans the parent before MissionManager commits campaign
            // progress. Successful scalar result values therefore remain readable
            // until CommitResult runs; failed/aborted attempts discard them here.
            if (!successful) _values.Clear();
            _power.Clear();
            _invincible.Clear();
        }
    }

    /// <summary>
    /// Base class for operation phases authored for the generic parent. Tracking a
    /// phase entity also registers it with the operation and marks it preserved from
    /// the child mission's final cleanup; the operation parent remains the one owner
    /// that decides whether it is deleted or released.
    /// </summary>
    public abstract class ContinuousPhaseMission : ComposedMission
    {
        protected new T Track<T>(T entity) where T : Entity
        {
            var tracked = base.Track(entity);
            if (tracked != null && Ctx?.ActiveOperation != null)
            {
                Ctx.ActiveOperation.Own(tracked);
                base.Preserve(tracked);
            }
            return tracked;
        }

        protected new void Preserve(Entity entity)
        {
            base.Preserve(entity);
            Ctx?.ActiveOperation?.KeepAfterSuccess(entity);
        }

        protected new void Release(Entity entity)
        {
            if (entity == null) return;
            if (Ctx?.ActiveOperation != null)
            {
                Ctx.ActiveOperation.KeepAfterSuccess(entity);
                base.Preserve(entity);
                return;
            }
            base.Release(entity);
        }

        protected new void ApplyBibleSetting()
        {
            if (OperationOwned && Ctx?.ActiveOperation?.Continuing == true) return;
            base.ApplyBibleSetting();
        }
    }

    /// <summary>
    /// Reusable parent for one-sitting operations. Internal Mission classes are
    /// ordinary phase scripts, but no MissionManager Start/Finish, campaign save,
    /// payout, loadout reset or free-roam gap occurs at an internal phase boundary.
    /// </summary>
    public abstract class ContinuousOperation : Mission, IContinuousOperation
    {
        private readonly List<Mission> _phases = new List<Mission>();
        private Mission _phase;
        private int _phaseIndex;
        private int _seenScene;
        private SceneBlocking _outro;

        protected abstract IReadOnlyList<string> OperationPhaseIds { get; }
        public abstract string OperationTitle { get; }
        protected abstract Mission CreatePhase(string phaseId);
        public abstract void CommitResult(MissionCatalog catalog);
        protected virtual string OperationId => GetType().Name;
        protected virtual ContinuousOperationState CreateState(MissionContext context) => new ContinuousOperationState(OperationId, context);
        protected virtual bool ValidateActive(Mission phase, out string reason) { reason = null; return true; }
        protected virtual bool ValidatePhaseEnd(Mission phase, out string reason) { reason = null; return true; }
        protected virtual void BeforePhaseStart(Mission phase, bool continuing) { }
        protected virtual void BeforeOperationCleanup() { }

        public override string Id => OperationPhaseIds[0];
        public override string Title => OperationTitle;
        public string PhaseId => OperationPhaseIds[_phaseIndex];
        public string FinalPhaseId => OperationPhaseIds[OperationPhaseIds.Count - 1];
        public Mission Phase => _phase;
        public ContinuousOperationState WorldState { get; private set; }
        public override bool AllowsCheckpointCapture => false;
        public override bool SupportsCheckpointRestore => false;

        public bool ContainsPhase(string id) => OperationPhaseIds.Any(p => string.Equals(p, id, StringComparison.OrdinalIgnoreCase));

        protected override bool OnStart()
        {
            if (OperationPhaseIds == null || OperationPhaseIds.Count == 0) throw new InvalidOperationException("Continuous operation has no phases.");
            if (Ctx.ActiveOperation != null) throw new InvalidOperationException("Another continuous operation is already active: " + Ctx.ActiveOperation.OperationId);
            WorldState = CreateState(Ctx) ?? throw new InvalidOperationException("Continuous operation did not create shared state.");
            Ctx.ActiveOperation = WorldState;
            _seenScene = Ctx.Cutscenes?.FinishedSequence ?? 0;
            _phaseIndex = 0;
            Ctx.Handoffs.Clear();
            return BeginPhase(false);
        }

        private bool BeginPhase(bool continuing)
        {
            WorldState.Continuing = continuing;
            WorldState.PhaseId = PhaseId;
            Ctx.Switching?.SetUnlocked();
            _phase = CreatePhase(PhaseId);
            if (_phase == null) throw new InvalidOperationException("No phase factory for " + PhaseId + ".");
            _phase.OperationOwned = true;
            _phases.Add(_phase);
            BeforePhaseStart(_phase, continuing);
            if (!_phase.Begin(Ctx)) return false;
            Logger.Info(OperationTitle + " phase entered: " + PhaseId + (continuing ? " (same live world)" : " (new whole-operation attempt)"));
            CurrentObjective = _phase.CurrentObjective;
            return true;
        }

        protected override void OnUpdate()
        {
            if (Ctx.Cutscenes?.IsActive == true) return;
            if (Ctx.Cutscenes != null && Ctx.Cutscenes.FinishedSequence != _seenScene)
            {
                _seenScene = Ctx.Cutscenes.FinishedSequence;
                if (Ctx.Cutscenes.LastRequired && Ctx.Cutscenes.LastOutcome != SceneOutcome.Completed && Ctx.Cutscenes.LastOutcome != SceneOutcome.Skipped)
                { Fail("A required operation action did not complete. Restart the entire " + OperationTitle + "."); return; }
            }
            if (!ValidateActive(_phase, out string lost)) { Fail(lost ?? "The live operation state became invalid."); return; }
            if (_phase.Status == MissionStatus.Running)
            {
                _phase.Tick();
                CurrentObjective = _phase.CurrentObjective;
                RequiredSwitch = _phase.RequiredSwitch;
                int stage = _phaseIndex * 100 + _phase.CurrentStage;
                if (CurrentStage != stage) GoToStage(stage);
                return;
            }
            if (_phase.Status != MissionStatus.Passed)
            { Fail(_phase.FailReason ?? "The operation was interrupted. Restart the entire " + OperationTitle + "."); return; }

            // Inner joins do not wait for radio speech to drain; the line may keep
            // playing over the next playable phase. Only the final result waits.
            if (_phaseIndex == OperationPhaseIds.Count - 1 && Ctx.Dialogue?.HasPending == true) return;
            if (!ValidatePhaseEnd(_phase, out string invalid)) { Fail(invalid ?? "The phase result is incomplete."); return; }
            _phase.RetireOperationPhase();
            if (_phaseIndex == OperationPhaseIds.Count - 1)
            {
                _outro = _phase.OutroBlocking();
                Pass();
                return;
            }

            _phaseIndex++;
            RequiredSwitch = null;
            if (!BeginPhase(true)) Fail("The next part could not start. Restart the entire " + OperationTitle + ".");
        }

        protected override void OnPassed()
        {
            if (!string.Equals(PhaseId, FinalPhaseId, StringComparison.OrdinalIgnoreCase) ||
                _phase == null || _phase.Status != MissionStatus.Passed || !ValidatePhaseEnd(_phase, out _))
                throw new InvalidOperationException(OperationTitle + " has not reached its final verified result.");
        }

        public override SceneBlocking OutroBlocking() => _outro;
        public override string CompleteCurrentObjective() => _phase?.CompleteCurrentObjective();

        protected override void OnCleanup()
        {
            try
            {
                BeforeOperationCleanup();
                for (int i = _phases.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (_phases[i].Status == MissionStatus.Running) _phases[i].Abort();
                        _phases[i].Cleanup();
                    }
                    catch (Exception ex) { Logger.Error(OperationTitle + " phase cleanup", ex); }
                }
                WorldState?.Dispose(Status == MissionStatus.Passed);
            }
            finally
            {
                Ctx.Handoffs.Clear();
                if (Ctx.ActiveOperation == WorldState) Ctx.ActiveOperation = null;
                _phases.Clear();
            }
        }
    }
}
