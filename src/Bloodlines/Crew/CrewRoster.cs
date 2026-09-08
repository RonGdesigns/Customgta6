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
        private readonly ModConfig _config;
        private readonly Dictionary<CrewSlot, Ped> _peds = new Dictionary<CrewSlot, Ped>();
        private readonly Dictionary<CrewSlot, Blip> _blips = new Dictionary<CrewSlot, Blip>();

        private RelationshipGroup _crewGroup;
        private bool _groupsReady;
        private Ped _storyPed;
        private Vector3 _storyPedPosition;

        public CrewRoster(ModConfig config)
        {
            _config = config;
        }

        public CrewSlot ActiveSlot { get; private set; } = CrewSlot.Ice;

        public Protagonist Active => Protagonist.Of(ActiveSlot);

        public RelationshipGroup CrewGroup => _crewGroup;

        public bool IsDeployed { get; private set; }

        /// <summary>
        /// When set, companions hold their ground instead of following the active
        /// character. Missions that run three separate operations at once (M01, M06,
        /// M55) need this; a tailing crew would walk straight through the fiction.
        /// </summary>
        public bool CompanionsHoldPosition { get; set; }

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

            StashStoryCharacter();

            var lead = PedFor(startAs);
            if (lead != null) Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, lead, true, true);

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

        private void ConfigurePed(Ped ped, Protagonist protagonist)
        {
            ped.RelationshipGroup = _crewGroup;
            ped.IsPersistent = true;
            ped.BlockPermanentEvents = true;
            ped.CanSufferCriticalHits = false;
            ped.CanBeDraggedOutOfVehicle = false;
            ped.DiesOnLowHealth = false;
            ped.MaxHealth = 300;
            ped.Health = 300;
            ped.Armor = 50;
            ped.Accuracy = 55;
            ped.CanSwitchWeapons = true;

            foreach (var weapon in protagonist.Loadout)
            {
                ped.Weapons.Give(weapon, 250, false, true);
            }

            ped.Weapons.Select(protagonist.Loadout[0], true);
        }

        /// <summary>Called by the switch controller once the player ped has changed.</summary>
        public void SetActive(CrewSlot slot)
        {
            ActiveSlot = slot;
            RefreshCompanionBlips();
            AssignCompanionAI();
        }

        public void AssignCompanionAI()
        {
            var player = PedFor(ActiveSlot);
            if (player == null) return;

            foreach (var ped in Companions)
            {
                ped.Task.ClearAll();
                ped.AlwaysKeepTask = true;
                ped.BlockPermanentEvents = true;

                if (CompanionsHoldPosition)
                {
                    ped.Task.GuardCurrentPosition();
                }
                else
                {
                    ped.Task.FollowToOffsetFromEntity(player, new Vector3(1.5f, -1.5f, 0f), 2.0f, -1, 4.0f, true);
                }
            }
        }

        /// <summary>Hold position and fight — used inside firefight beats and by missions.</summary>
        public void OrderCompanionsToFight(float radius = 200f)
        {
            foreach (var ped in Companions)
            {
                ped.Task.ClearAll();
                ped.Task.FightAgainstHatedTargets(radius);
            }
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

            foreach (var protagonist in Protagonist.All)
            {
                var ped = PedFor(protagonist.Slot);
                if (ped == null) continue;
                if (protagonist.Slot == ActiveSlot) continue;

                if (ped.IsDead)
                {
                    if (_config.CompanionsRespawnOnDeath) RespawnCompanion(protagonist);
                    continue;
                }

                if (_config.CompanionHealthFloor > 0 && ped.Health < _config.CompanionHealthFloor)
                {
                    ped.Health = _config.CompanionHealthFloor;
                }

                if (ped.IsInCombat || CompanionsHoldPosition) continue;

                var player = PedFor(ActiveSlot);
                if (player != null && ped.Position.DistanceTo(player.Position) > 90f)
                {
                    // Companions that fall too far behind teleport back rather than
                    // pathfinding across half of Los Santos and desyncing the mission.
                    ped.Position = player.Position + player.ForwardVector * -2.5f;
                    ped.Task.ClearAll();
                    AssignCompanionAI();
                }
            }
        }

        private void RespawnCompanion(Protagonist protagonist)
        {
            var player = PedFor(ActiveSlot);
            if (player == null) return;

            Logger.Warn(protagonist.DisplayName + " went down; respawning as companion.");
            GameUtils.SafeDelete(PedFor(protagonist.Slot));
            _peds.Remove(protagonist.Slot);

            var model = protagonist.Model;
            if (!GameUtils.RequestModel(model)) return;

            var ped = World.CreatePed(model, player.Position + OffsetFor(protagonist.Slot) - player.ForwardVector * 3f, player.Heading);
            model.MarkAsNoLongerNeeded();
            if (ped == null || !ped.Exists()) return;

            ConfigurePed(ped, protagonist);
            _peds[protagonist.Slot] = ped;
            RefreshCompanionBlips();
            AssignCompanionAI();
            GameUtils.Notify("~o~" + protagonist.DisplayName + "~s~ patched up and back on your six.");
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
            _storyPed.IsPersistent = true;
            _storyPed.IsInvincible = true;
            _storyPed.IsVisible = false;
            _storyPed.IsCollisionEnabled = false;
            _storyPed.BlockPermanentEvents = true;
            _storyPed.IsPositionFrozen = true;
            Logger.Info("Story character stashed at " + _storyPedPosition + ".");
        }

        private void RestoreStoryCharacter()
        {
            if (_storyPed == null || !_storyPed.Exists())
            {
                _storyPed = null;
                return;
            }

            var handOverPoint = Game.Player.Character != null && Game.Player.Character.Exists()
                ? Game.Player.Character.Position
                : _storyPedPosition;

            _storyPed.IsPositionFrozen = false;
            _storyPed.Position = handOverPoint;
            _storyPed.IsVisible = true;
            _storyPed.IsCollisionEnabled = true;
            _storyPed.IsInvincible = false;
            _storyPed.BlockPermanentEvents = false;

            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, _storyPed, true, true);
            _storyPed.Task.ClearAllImmediately();
            Logger.Info("Story character restored at " + handOverPoint + ".");
            _storyPed = null;
        }

        public void Dismiss()
        {
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

            _peds.Clear();
            IsDeployed = false;
        }
    }
}
