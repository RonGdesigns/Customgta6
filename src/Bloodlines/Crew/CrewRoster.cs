using System.Collections.Generic;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>
    /// Owns the three protagonist peds: spawning, relationship groups, companion AI
    /// and cleanup. Exactly one of them is the player ped at any moment; the other
    /// two are AI companions held to a health floor rather than made invincible.
    /// </summary>
    /// <summary>Where a character starts a deployment.</summary>
    public struct PedPlacement
    {
        public PedPlacement(Vector3 position, float heading)
        {
            Position = position;
            Heading = heading;
        }

        public Vector3 Position { get; }
        public float Heading { get; }
    }

    public sealed class CrewRoster
    {
        /// <summary>Armor every protagonist spawns and revives with.</summary>
        private const int StartingArmor = CrewDurability.Armor;
        private readonly HashSet<int> _shielded = new HashSet<int>();

        public WeaponProgression Arsenal { get; set; }
        private readonly ModConfig _config;
        private readonly Dictionary<CrewSlot, Ped> _peds = new Dictionary<CrewSlot, Ped>();
        private readonly Dictionary<CrewSlot, Blip> _blips = new Dictionary<CrewSlot, Blip>();

        private readonly CompanionController _companions;
        private readonly CompanionRecovery _recovery = new CompanionRecovery();
        private RelationshipGroup _crewGroup;
        private bool _groupsReady;
        private Ped _storyPed;
        private Vector3 _storyPedPosition;
        private float _storyPedHeading;
        public PedPlacement? RecoveryOrigin => _storyPed == null ? (PedPlacement?)null : new PedPlacement(_storyPedPosition, _storyPedHeading);

        public CrewRoster(ModConfig config)
        {
            _config = config;
            _companions = new CompanionController(config);
            _companions.Driver.IsRendezvous = vehicle => !_companions.MissionActive && !_companions.IndependentFreeRoam && PedFor(ActiveSlot) != null && !PedFor(ActiveSlot).IsInVehicle(vehicle);
            _companions.Driver.FollowDestination = vehicle =>
            {
                if (_companions.IndependentFreeRoam && !_companions.MissionActive) return null;
                var leader = PedFor(ActiveSlot);
                return leader != null && leader.Exists() && !leader.IsInVehicle(vehicle)
                    ? (Vector3?)(leader.Position - leader.ForwardVector * (leader.IsInVehicle() ? 18f : 8f)) : null;
            };
            _companions.IsCrewMember = ped =>
            {
                foreach (var member in _peds.Values)
                    if (member != null && member.Exists() && member.Handle == ped.Handle) return true;
                return false;
            };
        }

        /// <summary>The companion state machine — missions can take direct control through it.</summary>
        public CompanionController CompanionAI => _companions;

        public CrewSlot ActiveSlot { get; private set; } = Protagonist.StartingSlot;

        public Protagonist Active => Protagonist.Of(ActiveSlot);

        public RelationshipGroup CrewGroup => _crewGroup;

        public bool IsDeployed { get; private set; }

        /// <summary>
        /// When set, companions hold their ground instead of following the active
        /// character. Missions that run three separate operations at once (M01, M06,
        /// M55) need this; a tailing crew would walk straight through the fiction.
        /// </summary>
        public bool CompanionsHoldPosition
        {
            get => _companions.HoldPosition;
            set => _companions.HoldPosition = value;
        }

        /// <summary>True while only one character is deployed (a solo mission).</summary>
        public bool IsSolo { get; private set; }

        /// <summary>
        /// Where this deployment started, or null when nothing is deployed. The death
        /// controller regroups here when a mission has no checkpoint to fall back on:
        /// it is a spot the game has already accepted three peds standing at, which no
        /// hand-written respawn coordinate can promise.
        /// </summary>
        public PedPlacement? DeployOrigin { get; private set; }

        public Ped PedFor(CrewSlot slot)
        {
            return _peds.TryGetValue(slot, out var ped) && ped != null && ped.Exists() ? ped : null;
        }

        public IEnumerable<Ped> Companions
        {
            get
            {
                foreach (var pair in _peds)
                {
                    if (pair.Key == ActiveSlot) continue;
                    if (pair.Value != null && pair.Value.Exists()) yield return pair.Value;
                }
            }
        }

        private void EnsureRelationshipGroups()
        {
            if (_groupsReady) return;

            _crewGroup = World.AddRelationshipGroup("BLOODLINES_CREW");
            _crewGroup.SetRelationshipBetweenGroups(_crewGroup, Relationship.Companion, true);
            _crewGroup.SetRelationshipBetweenGroups(new RelationshipGroup(Game.GenerateHash("PLAYER")), Relationship.Companion, true);
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");
            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            _crewGroup.SetRelationshipBetweenGroups(cartel, Relationship.Hate, true);
            _crewGroup.SetRelationshipBetweenGroups(aegis, Relationship.Hate, true);
            cartel.SetRelationshipBetweenGroups(aegis, Relationship.Neutral, true);

            _groupsReady = true;
            Logger.Info("Relationship groups registered (crew / cartel / aegis).");
        }

        /// <summary>
        /// Puts the crew on the map together. The player becomes <paramref name="startAs"/>;
        /// the other two spawn beside them as companions.
        /// </summary>
        public bool Deploy(CrewSlot startAs, Vector3 origin, float heading)
        {
            var placements = new Dictionary<CrewSlot, PedPlacement>();
            foreach (var protagonist in Protagonist.All)
            {
                placements[protagonist.Slot] = new PedPlacement(origin + OffsetFor(protagonist.Slot), heading);
            }

            return Deploy(startAs, placements);
        }

        /// <summary>
        /// Puts one character on the map alone — the nine solo missions, where the
        /// other two are not just idle but absent from the fiction entirely.
        /// </summary>
        public bool DeploySolo(CrewSlot slot, Vector3 position, float heading)
        {
            EnsureRelationshipGroups();
            Dismiss();

            var protagonist = Protagonist.Of(slot);
            var model = protagonist.Model;
            if (!GameUtils.RequestModel(model))
            {
                Logger.Error("Could not stream " + protagonist.DisplayName + " for a solo deployment.");
                return false;
            }

            var ped = World.CreatePed(model, position, heading);
            model.MarkAsNoLongerNeeded();
            if (ped == null || !ped.Exists())
            {
                Logger.Error("CreatePed returned nothing for " + protagonist.DisplayName + ".");
                return false;
            }

            ConfigurePed(ped, protagonist);
            _peds[slot] = ped;
            ActiveSlot = slot;
            IsDeployed = true;
            IsSolo = true;
            DeployOrigin = new PedPlacement(position, heading);

            StashStoryCharacter();
            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, ped, true, true);
            GameUtils.AssertPlayerControl("solo deployment");
            ProtectCrew(ped);
            CrewDurability.RestoreAfterSwitch(ped, CrewDurability.Health, StartingArmor);

            Logger.Info("Solo deployment: " + protagonist.DisplayName + " at " + position + ".");
            return true;
        }

        /// <summary>
        /// Puts each character down at their own start point — the split-approach
        /// deployment the opening mission is built on.
        /// </summary>
        public bool Deploy(CrewSlot startAs, IDictionary<CrewSlot, PedPlacement> placements)
        {
            EnsureRelationshipGroups();
            Dismiss();

            foreach (var protagonist in Protagonist.All)
            {
                if (!placements.TryGetValue(protagonist.Slot, out var placement))
                {
                    Logger.Error("No placement supplied for " + protagonist.DisplayName + ".");
                    Dismiss();
                    return false;
                }

                var model = protagonist.Model;
                if (!GameUtils.RequestModel(model))
                {
                    Logger.Error("Could not stream " + protagonist.DisplayName + " (" + protagonist.ModelName + ").");
                    Dismiss();
                    return false;
                }

                var ped = World.CreatePed(model, placement.Position, placement.Heading);
                model.MarkAsNoLongerNeeded();

                if (ped == null || !ped.Exists())
                {
                    Logger.Error("CreatePed returned nothing for " + protagonist.DisplayName + ".");
                    Dismiss();
                    return false;
                }

                ConfigurePed(ped, protagonist);
                _peds[protagonist.Slot] = ped;
            }

            ActiveSlot = startAs;
            IsDeployed = true;
            IsSolo = false;
            DeployOrigin = placements[startAs];

            StashStoryCharacter();

            var lead = PedFor(startAs);
            if (lead != null) { Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, lead, true, true); GameUtils.AssertPlayerControl("crew deployment"); ProtectCrew(lead); CrewDurability.RestoreAfterSwitch(lead, CrewDurability.Health, StartingArmor); }

            RefreshCompanionBlips();
            AssignCompanionAI();

            Logger.Info("Crew deployed as " + Active.DisplayName + ".");
            return true;
        }

        private static Vector3 OffsetFor(CrewSlot slot)
        {
            switch (slot)
            {
                case CrewSlot.Gohan: return new Vector3(1.6f, 0.6f, 0f);
                case CrewSlot.Guess: return new Vector3(-1.6f, 0.6f, 0f);
                default: return Vector3.Zero;
            }
        }

        private void ProtectCrew(Ped ped)
        {
            if (ped == null || !ped.Exists()) return;
            ped.MaxHealth = CrewDurability.Health;
            ped.CanSufferCriticalHits = false;
            ped.RelationshipGroup = _crewGroup;
            // Friendly targeting is blocked by relationships and the companion target filter.
            // Do not make the player immune to their own fire or explosive splash.
            ped.IsFireProof = false; ped.IsExplosionProof = false;
            Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, ped, false, false);
            Function.Call(Hash.SET_ENTITY_CAN_BE_DAMAGED_BY_RELATIONSHIP_GROUP, ped, true, _crewGroup.Hash);
        }

        private void ConfigurePed(Ped ped, Protagonist protagonist)
        {
            CrewAppearance.Apply(ped, protagonist.Slot);
            ProtectCrew(ped);
            ped.IsPersistent = true;
            ped.BlockPermanentEvents = true;
            ped.CanSufferCriticalHits = false;
            ped.CanBeDraggedOutOfVehicle = false;
            ped.DiesOnLowHealth = false;
            ped.MaxHealth = CrewDurability.Health;
            ped.Health = CrewDurability.Health;
            ped.Armor = StartingArmor;
            ped.Accuracy = 55;
            ped.CanSwitchWeapons = true;
            Function.Call(Hash.SET_PED_CONFIG_FLAG, ped, 184, true); // Stay in the selected passenger seat.

            foreach (var weapon in protagonist.Loadout)
            {
                if (Function.Call<bool>(Hash.IS_WEAPON_VALID, (uint)weapon))
                    ped.Weapons.Give(weapon, 250, false, true);
            }

            Arsenal?.Apply(protagonist.Slot, ped);
            var primary = protagonist.Loadout[0];
            if (!Function.Call<bool>(Hash.IS_WEAPON_VALID, (uint)primary))
            {
                primary = WeaponHash.CarbineRifle;
                ped.Weapons.Give(primary, 250, false, true);
                Logger.Warn(protagonist.Handle + " starting rifle unavailable; supplied stock Carbine Rifle.");
            }
            ped.Weapons.Select(primary, true);
        }

        /// <summary>Called by the switch controller once the player ped has changed.</summary>
        public void SetActive(CrewSlot slot)
        {
            var departing = ActiveSlot;
            if (departing != slot && !_companions.HoldPosition && _companions.StateOf(departing) != CompanionState.Scripted)
                _companions.Driver.Arm(departing, PedFor(departing));
            _companions.Driver.Forget(slot);
            _companions.Convoy.Forget(slot);
            ActiveSlot = slot;
            ProtectCrew(PedFor(slot));
            var incoming = PedFor(slot);
            if (incoming != null && !incoming.IsInVehicle()) incoming.Task.ClearAllImmediately();
            RefreshCompanionBlips();
            AssignCompanionAI();
        }

        public void AssignCompanionAI()
        {
            var player = PedFor(ActiveSlot);
            if (player == null) return;

            foreach (var protagonist in Protagonist.All)
            {
                if (protagonist.Slot == ActiveSlot) continue;

                var ped = PedFor(protagonist.Slot);
                if (ped == null) continue;

                // Force a fresh decision rather than leaving a stale task in place.
                _companions.Refresh(protagonist.Slot);
                _companions.Update(protagonist.Slot, ped, player);
            }
        }

        /// <summary>Hold position and fight — used inside firefight beats and by missions.</summary>
        public void OrderCompanionsToFight(float radius = 200f)
        {
            // All companion combat uses the same roster-aware target filter.
            AssignCompanionAI();
        }

        private void RefreshCompanionBlips()
        {
            foreach (var protagonist in Protagonist.All)
            {
                if (_blips.TryGetValue(protagonist.Slot, out var existing)) GameUtils.SafeDelete(existing);
                _blips.Remove(protagonist.Slot);

                if (protagonist.Slot == ActiveSlot) continue;

                var ped = PedFor(protagonist.Slot);
                if (ped == null) continue;

                var blip = ped.AddBlip();
                blip.Sprite = BlipSprite.Standard;
                blip.Color = protagonist.BlipColor;
                blip.Scale = 0.85f;
                blip.IsShortRange = true;
                blip.Name = protagonist.DisplayName;
                _blips[protagonist.Slot] = blip;
            }
        }

        /// <summary>
        /// Per-tick upkeep. The design bible pinned companion health to a constant
        /// every frame, which reads as invincibility and kills all combat tension;
        /// this instead tops a companion back up only after it drops below the
        /// configured floor, so damage still registers and still matters.
        /// </summary>
        public void Update()
        {
            if (!IsDeployed) return;
            if (!_companions.MissionActive) _companions.Life.Wanted.Capture(ActiveSlot, Game.Player.WantedLevel);
            // One car, one heat (Ron, September 12): everyone riding with the active
            // brother carries the game's current level, mission or not, and any
            // pursuit of his own is dropped while he rides along.
            {
                var active = PedFor(ActiveSlot); var shared = active?.CurrentVehicle;
                if (shared != null && shared.Exists())
                    foreach (var pair in _peds)
                        if (pair.Key != ActiveSlot && pair.Value != null && pair.Value.Exists() && pair.Value.IsInVehicle(shared))
                            _companions.Life.RideAlong(pair.Key, Game.Player.WantedLevel);
            }
            // CHANGE_PLAYER_PED can reset the active actor's relationship group.
            foreach (var member in _peds.Values)
                if (member != null && member.Exists() && member.RelationshipGroup != _crewGroup) ProtectCrew(member);

            foreach (var protagonist in Protagonist.All)
            {
                var ped = PedFor(protagonist.Slot);
                if (protagonist.Slot == ActiveSlot)
                {
                    // The brother you play is mortal again the moment you take him.
                    if (ped != null && ped.Exists() && _shielded.Remove(ped.Handle)) ped.IsInvincible = false;
                    continue;
                }
                if (!_peds.ContainsKey(protagonist.Slot)) continue; // Solo missions never deployed this hero.

                if (ped == null || ped.IsDead)
                {
                    Vector3 recoveryPoint;
                    if (_recovery.TryGetDestination(protagonist.Slot, ped, PedFor(ActiveSlot),
                        _config.CompanionsRespawnOnDeath && !_companions.MissionActive, out recoveryPoint))
                        RespawnCompanion(protagonist, recoveryPoint);
                    continue;
                }

                _recovery.Forget(protagonist.Slot);
                // The brothers you are not controlling cannot be hurt. Their AI still
                // takes cover and fights; it just cannot lose them for you. The flag is
                // ours to own: scenes and recovery restore whatever they found, so it
                // is re-asserted every tick and taken off only by a switch or stand-down.
                if (_config.CompanionsInvincible && ped.Exists())
                {
                    if (!ped.IsInvincible) ped.IsInvincible = true;
                    _shielded.Add(ped.Handle);
                }
                if (_config.CompanionHealthFloor > 0 && ped.Health < _config.CompanionHealthFloor)
                {
                    ped.Health = _config.CompanionHealthFloor;
                }

                // Follow / combat / vehicle / hold / recovery all live in the state
                // machine, so there is one place to reason about what a companion is
                // doing and why.
                _companions.Update(protagonist.Slot, ped, PedFor(ActiveSlot));
            }
        }

        /// <summary>
        /// Recovers the active hero without moving or healing the other heroes.
        /// Resurrect first, then restore health and physical state. Only attach a
        /// different ped if player ownership actually changed; never self-handover.
        /// DeathController separately verifies collision, control and real movement.
        /// </summary>
        public bool ReviveActiveAt(Vector3 position, float heading)
        {
            if (!IsDeployed) return false;
            var active = PedFor(ActiveSlot);
            if (active == null || !active.Exists()) return false;
            _companions.Forget(ActiveSlot);
            if (active.IsDead) Function.Call(Hash.RESURRECT_PED, active);
            CrewDurability.RestoreAfterSwitch(active, CrewDurability.Health, StartingArmor);
            if (active.IsInVehicle())
            {
                Function.Call(Hash.TASK_LEAVE_VEHICLE, active, active.CurrentVehicle, 16);
                for (int attempt = 0; attempt < 10 && active.IsInVehicle(); attempt++) Script.Wait(25);
                if (active.IsInVehicle()) throw new System.InvalidOperationException("Could not leave the recovery vehicle.");
            }
            RecoveryMobility.Restore(active);
            active.ClearBloodDamage(); active.ClearLastWeaponDamage();
            active.IsInvincible = false; active.IsPersistent = true;
            active.Position = position; active.Heading = heading;
            Function.Call(Hash.REQUEST_COLLISION_AT_COORD, position.X, position.Y, position.Z);
            // A resurrected player normally still owns this exact ped. Avoid a
            // self-to-self handover through the engine's character-switch path.
            if (Game.Player.Character == null || Game.Player.Character.Handle != active.Handle)
                Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, active, false, true);
            Game.Player.IsInvincible = false;
            ProtectCrew(active);
            CrewDurability.RestoreAfterSwitch(active, CrewDurability.Health, StartingArmor);
            RecoveryMobility.Restore(active);
            Logger.Info("Active character recovered without regrouping teammates: " + Active.DisplayName + ".");
            return active.Exists() && !active.IsDead && Game.Player.Character.Handle == active.Handle;
        }

        /// <summary>Explicit whole-crew recovery helper; player death uses ReviveActiveAt.</summary>
        public bool ReviveAll()
        {
            if (!IsDeployed) return false;
            _companions.Driver.Clear();
            _companions.Convoy.Clear();

            var active = PedFor(ActiveSlot);
            if (active == null) return false;

            foreach (var protagonist in Protagonist.All)
            {
                var ped = PedFor(protagonist.Slot);
                if (ped == null) continue;

                if (ped.IsDead) Function.Call(Hash.RESURRECT_PED, ped);

                // A busted recovery arrives here alive but in handcuffs. Resurrect
                // does nothing for that, and a cuffed player cannot draw a weapon,
                // so the restraint is lifted explicitly.
                Function.Call(Hash.UNCUFF_PED, ped);
                Function.Call(Hash.SET_ENABLE_HANDCUFFS, ped, false);

                RecoveryMobility.Restore(ped);
                ped.ClearBloodDamage();
                ped.ClearLastWeaponDamage();
                ped.MaxHealth = CrewDurability.Health;
                ped.Health = CrewDurability.Health;
                ped.Armor = StartingArmor;
                ped.IsInvincible = false;
                ped.IsPersistent = true;
                ped.BlockPermanentEvents = true;
                ped.MaxHealth = CrewDurability.Health;
            ped.CanSufferCriticalHits = false;
            ped.RelationshipGroup = _crewGroup;
            // Friendly targeting is blocked by relationships and the companion target filter.
            // Do not make the player immune to their own fire or explosive splash.
            ped.IsFireProof = false; ped.IsExplosionProof = false;
            }

            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, active, true, true);
            Game.Player.IsInvincible = false;
            ProtectCrew(active);
            CrewDurability.RestoreAfterSwitch(active, CrewDurability.Health, StartingArmor);
            RecoveryMobility.Restore(active);

            RefreshCompanionBlips();
            AssignCompanionAI();
            Logger.Info("Crew revived; player back on " + Active.DisplayName + ".");
            return active.Exists() && !active.IsDead && Game.Player.Character.Handle == active.Handle;
        }

        /// <summary>
        /// Stands the whole crew back up at one point — the death controller's
        /// fallback when the running mission has no checkpoint, and its only move in
        /// free roam. Uses the same spawn offsets a deployment does, so the three of
        /// them never come back inside one another.
        /// </summary>
        public void RegroupAt(Vector3 position, float heading)
        {
            foreach (var protagonist in Protagonist.All)
            {
                var ped = PedFor(protagonist.Slot);
                if (ped == null) continue;

                if (ped.IsInVehicle())
                {
                    Function.Call(Hash.TASK_LEAVE_VEHICLE, ped, ped.CurrentVehicle, 16);
                    for (int attempt = 0; attempt < 10 && ped.IsInVehicle(); attempt++) Script.Wait(25);
                    if (ped.IsInVehicle()) throw new System.InvalidOperationException("Could not safely leave recovery vehicle.");
                }
                RecoveryMobility.Restore(ped);
                ped.Position = position + OffsetFor(protagonist.Slot);
                ped.Heading = heading;
            }

            AssignCompanionAI();
            Logger.Info("Crew regrouped at " + position + ".");
        }

        private void RespawnCompanion(Protagonist protagonist, Vector3 position)
        {
            var player = PedFor(ActiveSlot);
            if (player == null) return;

            Logger.Info(protagonist.DisplayName + " recovering away from the player after the downed timer.");
            var previous = PedFor(protagonist.Slot);

            var model = protagonist.Model;
            if (!GameUtils.RequestModel(model)) return;

            var ped = World.CreatePed(model, position, player.Heading);
            model.MarkAsNoLongerNeeded();
            if (ped == null || !ped.Exists()) return;

            ConfigurePed(ped, protagonist);
            _companions.Forget(protagonist.Slot);
            _companions.KeepRecoverySeparate(protagonist.Slot);
            _peds[protagonist.Slot] = ped;
            _recovery.Forget(protagonist.Slot);
            // Keep the old body's aftermath, and let this hero travel back normally.
            GameUtils.SafeRelease(previous);
            RefreshCompanionBlips();
            _companions.Update(protagonist.Slot, ped, player);
            GameUtils.Notify("~o~" + protagonist.DisplayName + "~s~ recovered. " +
                (_companions.IndependentFreeRoam ? "Back to their own plans." : "Making their way back to you."));
        }

        /// <summary>
        /// Parks whichever story character the player was using (Franklin, Michael,
        /// Trevor or a freemode ped) somewhere safe and unloseable for the duration.
        /// Without this the player is silently left as a gang ped for the rest of
        /// the save, which is not a trade anyone agreed to by pressing a hotkey.
        /// </summary>
        private void StashStoryCharacter()
        {
            var current = Game.Player.Character;
            if (current == null || !current.Exists()) return;

            foreach (var protagonist in Protagonist.All)
            {
                var ped = PedFor(protagonist.Slot);
                if (ped != null && ped.Handle == current.Handle) return; // already a crew ped
            }

            _storyPed = current;
            _storyPedPosition = current.Position;
            _storyPedHeading = current.Heading;
            _storyPed.IsPersistent = true;
            _storyPed.IsInvincible = true;
            _storyPed.IsVisible = false;
            _storyPed.IsCollisionEnabled = false;
            _storyPed.BlockPermanentEvents = true;
            _storyPed.IsPositionFrozen = true;
            Logger.Info("Story character stashed at " + _storyPedPosition + ".");
        }

        public void ReturnToStoryOrigin()
        {
            var player = Game.Player.Character;
            if (_storyPed != null && _storyPed.Exists() && player != null && player.Exists())
                player.Position = _storyPedPosition;
        }

        private void RestoreStoryCharacter()
        {
            if (_storyPed == null || !_storyPed.Exists())
            {
                _storyPed = null;
                return;
            }

            var handOverPoint = Game.Player.Character != null && Game.Player.Character.Exists() && !Game.Player.Character.IsDead
                ? Game.Player.Character.Position
                : _storyPedPosition;

            _storyPed.IsPositionFrozen = false;
            _storyPed.Position = handOverPoint;
            _storyPed.IsVisible = true;
            _storyPed.IsCollisionEnabled = true;
            _storyPed.IsInvincible = false;
            _storyPed.BlockPermanentEvents = false;

            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, _storyPed, true, true);
            GameUtils.AssertPlayerControl("the story character's return");
            _storyPed.Task.ClearAllImmediately();
            Logger.Info("Story character restored at " + handOverPoint + ".");
            _storyPed = null;
        }

        public void Dismiss()
        {
            if (IsDeployed) Arsenal?.SaveCrew(this);
            _companions.Life.Clear(clearHeat: true);
            _companions.Military.Clear();
            RestoreStoryCharacter();

            foreach (var blip in _blips.Values) GameUtils.SafeDelete(blip);
            _blips.Clear();

            var playerPed = Game.Player.Character;
            foreach (var pair in _peds)
            {
                var ped = pair.Value;
                if (ped == null || !ped.Exists()) continue;

                if (playerPed != null && playerPed.Exists() && ped.Handle == playerPed.Handle)
                {
                    // Never delete the ped the player is currently controlling — that
                    // happens when the story character could not be restored.
                    ped.MarkAsNoLongerNeeded();
                    continue;
                }

                GameUtils.SafeDelete(ped);
            }

            foreach (var protagonist in Protagonist.All) _companions.Forget(protagonist.Slot);

            _peds.Clear();
            _recovery.Clear();
            IsDeployed = false;
            IsSolo = false;
            DeployOrigin = null;
        }
    }
}
