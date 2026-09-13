using System;
using Bloodlines.Core;
using GTA;
namespace Bloodlines.Missions.Objectives
{
    public sealed class ConvoyOverwatchObjective : Objective
    {
        private readonly Func<Vehicle> _aircraft, _escort;
        private readonly Func<bool> _atAmbush;
        private int _entered, _lastTick, _trackedMs, _inRangeMs, _lostSince;
        private bool _acquired;
        public ConvoyOverwatchObjective(Func<Vehicle> aircraft, Func<Vehicle> escort, Func<bool> atAmbush)
            : base("Guess: follow the rear escort in the Frogger. Keep 60-350m away; track it for 25 seconds or until it reaches Ice.")
        { _aircraft=aircraft;_escort=escort;_atAmbush=atAmbush; }
        public override void Enter(MissionContext context)
        { base.Enter(context);_entered=_lastTick=Game.GameTime;_trackedMs=_inRangeMs=0;_lostSince=-1;_acquired=false; }
        public override void Update(MissionContext context)
        {
            var escort=_escort();var aircraft=_aircraft();var player=Game.Player.Character;
            if(escort==null||!escort.Exists()||escort.IsDead){Fail("The escort was lost with the transponder. Restart M09 and keep the truck intact.");return;}
            if(aircraft==null||!aircraft.Exists()||aircraft.IsDead){Fail("The Frogger was destroyed.");return;}
            int now=Game.GameTime, elapsed=Math.Max(0,Math.Min(1000,now-_lastTick));_lastTick=now;
            ObjectiveMarkers.Navigation(escort.Position,RequiredCharacter,aircraft);
            float distance=aircraft.Position.DistanceTo(escort.Position);
            bool flying=player!=null&&player.Exists()&&player.IsInVehicle(aircraft)&&player.SeatIndex==VehicleSeat.Driver&&aircraft.HeightAboveGround>=8f;
            bool inRange=IsOwnerActive(context)&&flying&&distance>=60f&&distance<=350f;
            bool arrived=_atAmbush();
            if(inRange)
            {
                _acquired=true;_lostSince=-1;_trackedMs+=elapsed;_inRangeMs+=elapsed;
                Label="Guess: track rear escort | "+(int)distance+"m (keep 60-350m) | "+Math.Min(25,_trackedMs/1000)+"/25s"+
                    (arrived?" | Convoy at Ice: confirming handoff.":" | Ice takes the driver next.");
                if(_trackedMs>=25000||(arrived&&_inRangeMs>=3000))
                { Logger.Info("M09 overwatch complete: "+(arrived?"convoy at ambush":"25 seconds tracked")+"; handoff to Ice.");Complete(); }
                return;
            }
            _inRangeMs=0;
            string instruction=!flying?"Guess: pilot the Frogger at least 8m above ground":distance<60f?"Guess: too close - back away from the escort":"Guess: get closer to the rear escort";
            if(!_acquired)
            {
                int left=Math.Max(0,180-(now-_entered)/1000);
                Label=instruction+" | "+(int)distance+"m; keep 60-350m | "+left+"s to acquire"+(arrived?" | Convoy passing Ice.":"");
                if(left==0)Fail("The convoy was not acquired. Follow the marked escort within 60-350m in the Frogger.");
            }
            else
            {
                if(_lostSince<0)_lostSince=now;
                int left=Math.Max(0,8-(now-_lostSince)/1000);
                Label=instruction+" | "+(int)distance+"m; keep 60-350m | restore contact in "+left+"s";
                if(left==0)Fail("The helicopter lost safe contact with the convoy. Keep 60-350m from the marked escort.");
            }
        }
    }
}
