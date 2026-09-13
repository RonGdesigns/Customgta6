using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace Bloodlines.Core
{
    public sealed partial class SurveyMode
    {
        public MissionLocation Draft { get; private set; }
        private MissionLocation _undoPlacement;
        public bool IsEditing => Draft != null;
        public bool CanUndoPlacement => _undoPlacement != null;
        public bool PlacementMenuOpen { get; set; }
        public IEnumerable<MissionLocation> PlacementLocations => _book.All;
        private MissionLocation _draftOrigin;
        public string PlacementProgress => IsEditing ? (_index + 1) + "/" + _queue.Count + " - " + Draft.Key : "finished";
        public bool PlacementDirty => IsEditing && _draftOrigin != null && (Draft.Position != _draftOrigin.Position ||
            Draft.Heading != _draftOrigin.Heading || Draft.SpawnRadius != _draftOrigin.SpawnRadius || Draft.SpawnCount != _draftOrigin.SpawnCount);

        private static MissionLocation CopyPlacement(MissionLocation p) => new MissionLocation { Key=p.Key,Position=p.Position,Heading=p.Heading,
            Kind=p.Kind,Status=p.Status,DistrictHint=p.DistrictHint,IsEditorSlot=p.IsEditorSlot,SpawnRadius=p.SpawnRadius,SpawnCount=p.SpawnCount };
        private static void ApplyPlacement(MissionLocation from, MissionLocation to)
        { to.Position=from.Position;to.Heading=from.Heading;to.Status=from.Status;to.SpawnRadius=from.SpawnRadius;to.SpawnCount=from.SpawnCount; }

        public bool BeginPlacement(string key)
        {
            var location = _book.Get(key);
            if (location == null) return false;
            Start(new[] { key });
            ResetPlacementDraft();
            return true;
        }
        public bool BeginPlacementTour(string prefix)
        {
            var keys = _book.All.Where(l => l.Key.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l.Key, StringComparer.Ordinal).Select(l => l.Key).ToArray();
            if (keys.Length == 0) return false;
            Start(keys); ResetPlacementDraft(); return true;
        }
        public void ResetPlacementDraft()
        {
            if (Current == null || IsTeleporting) return;
            Draft = CopyPlacement(Current);
            if (MissionPlacement.HasGroup(Draft.Key) && !MissionPlacement.HasFormation(Draft))
            { Draft.SpawnRadius = MissionPlacement.DefaultRadius(Draft.Key); Draft.SpawnCount = MissionPlacement.DefaultCount(Draft.Key); }
            _draftOrigin = CopyPlacement(Draft);
        }
        public bool MovePlacement(int delta, bool teleport = true)
        {
            if (!IsEditing || IsTeleporting) return false;
            if (PlacementDirty) { GameUtils.Notify("~y~Save or discard this draft before moving to another spot."); return false; }
            int index = _index + delta;
            if (index < 0 || index >= _queue.Count) { GameUtils.Notify("~g~" + (index < 0 ? "First" : "Last") + " placement. Use Finish to return to the list."); return false; }
            Select(index); ResetPlacementDraft(); if (teleport) TeleportToCurrent(); return true;
        }
        public void SaveAndNextPlacement() { if (SavePlacement(true)) MovePlacement(1); }
        public void PlaceAtPlayer()
        {
            var player = Game.Player.Character;
            if (!IsEditing || IsTeleporting || player == null || !player.Exists() || player.IsDead) return;
            Entity source = player.CurrentVehicle != null ? (Entity)player.CurrentVehicle : player;
            Draft.Position = source.Position;
            Draft.Heading = source.Heading;
        }
        public void AdjustPlacement(float radiusDelta, int countDelta, float headingDelta, float heightDelta = 0f)
        {
            if (!IsEditing || IsTeleporting) return;
            if (MissionPlacement.HasGroup(Draft.Key))
            { Draft.SpawnRadius=MissionPlacement.ClampRadius(Draft.SpawnRadius+radiusDelta);Draft.SpawnCount=MissionPlacement.ClampCount(Draft.SpawnCount+countDelta); }
            Draft.Heading = (Draft.Heading + headingDelta) % 360f;
            if (Draft.Heading < 0f) Draft.Heading += 360f;
            Draft.Position += new Vector3(0f,0f,heightDelta);
        }
        public bool SavePlacement(bool keepEditing = false)
        {
            if (!IsEditing || IsTeleporting) return false;
            _lastCaptureFrame = Game.GameTime;
            var target = _book.Get(Draft.Key);
            var previous = CopyPlacement(target);
            Draft.Status = LocationStatus.Surveyed;
            ApplyPlacement(Draft, target);
            if (!Write()) { ApplyPlacement(previous,target); return false; }
            _undoPlacement = previous;
            string key=Draft.Key;
            Logger.Info("Placement saved: " + key + " at " + Draft.Position + ", heading " + Draft.Heading +
                ", count " + Draft.SpawnCount + ", radius " + Draft.SpawnRadius);
            if (keepEditing) { Select(_index); ResetPlacementDraft(); } else Stop();
            GameUtils.Notify("~g~Saved " + key + ".~s~ Applies on the next mission start. Survey file backed up.");
            return true;
        }
        public bool UndoPlacement()
        {
            if (_undoPlacement == null || IsEditing) return false;
            var target = _book.Get(_undoPlacement.Key);
            var current = CopyPlacement(target);
            ApplyPlacement(_undoPlacement,target);
            if (!Write()) { ApplyPlacement(current,target); return false; }
            _undoPlacement=null;
            GameUtils.Notify("~g~Last placement restored. Restart the mission to use it.");
            return true;
        }
        private void DrawPlacement()
        {
            var p = Draft.Position;
            bool group = MissionPlacement.HasGroup(Draft.Key);
            float radius = group ? Draft.SpawnRadius : 1f;
            for (int i=0;i<48;i++)
            {
                double a=i*Math.PI/24,b=(i+1)*Math.PI/24;
                Function.Call(Hash.DRAW_LINE,p.X+(float)Math.Cos(a)*radius,p.Y+(float)Math.Sin(a)*radius,p.Z+.12f,
                    p.X+(float)Math.Cos(b)*radius,p.Y+(float)Math.Sin(b)*radius,p.Z+.12f,255,185,45,230);
            }
            if (group) for (int i=0;i<Draft.SpawnCount;i++)
                GameUtils.DrawObjectiveMarker(MissionPlacement.GroupPoint(Draft,i),Color.FromArgb(150,255,90,60),.35f);
            GameUtils.DrawObjectiveMarker(p,Color.Gold,.6f);
            double angle=Draft.Heading*Math.PI/180;
            Function.Call(Hash.DRAW_LINE,p.X,p.Y,p.Z+.4f,p.X-(float)Math.Sin(angle)*3f,p.Y+(float)Math.Cos(angle)*3f,p.Z+.4f,70,220,255,255);
            if (PlacementMenuOpen) return;
            new ContainerElement(new PointF(40,435),new SizeF(650,158),Color.FromArgb(225,18,20,24)).Draw();
            new TextElement("Placement "+(_index+1)+"/"+_queue.Count+": "+MissionPlacement.Label(Draft),new PointF(50,441),.32f,Color.White).Draw();
            new TextElement(Draft.Key+" | heading "+Draft.Heading.ToString("0")+" | Z "+p.Z.ToString("0.00"),new PointF(50,468),.27f,Color.Gold).Draw();
            new TextElement(group ? "Radius "+radius.ToString("0.0")+"m | "+Draft.SpawnCount+" enemies (dots preview placement)" : "Point / facing preview. No enemy-count override for this item.",new PointF(50,492),.26f,Color.White).Draw();
            new TextElement("X: place here | A: save | B: cancel | F7: visit saved spot",new PointF(50,516),.26f,Color.White).Draw();
            new TextElement("Hold RT/LT: radius | D-pad Up/Down: count | Left/Right: turn",new PointF(50,540),.26f,Color.White).Draw();
            new TextElement("Keyboard: Space place | Enter save | Esc cancel | +/- radius | PgUp/Dn height",new PointF(50,564),.25f,Color.White).Draw();
        }
    }
}
