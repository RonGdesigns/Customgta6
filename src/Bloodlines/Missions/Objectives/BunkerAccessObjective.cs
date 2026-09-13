using Bloodlines.Core;
using GTA;
using GTA.Math;
namespace Bloodlines.Missions.Objectives
{
    /// <summary>Entry/exit completes only after the shared interior service confirms arrival.</summary>
    public sealed class BunkerAccessObjective : Objective
    {
        private readonly ApartmentAccess _access;
        private readonly bool _enter;
        private readonly Vector3 _point;
        private bool _requested;
        public BunkerAccessObjective(ApartmentAccess access,bool enter,Vector3 point) : base(enter ?
            "Gohan: walk to the bunker entrance and press E / D-pad Right to check inside." :
            "Gohan: use the exit marker where you entered this room. Press E / D-pad Right to return to the crew outside.")
        { _access=access;_enter=enter;_point=point; }
        public override Vector3? AssignmentPosition => _point;
        public override void Update(MissionContext context)
        {
            if(_access.Busy)return;
            if(_requested)
            {
                if(_access.Inside==_enter)Complete();
                else Fail("Bunker access could not load safely. Your position was restored; restart M23 and check Bloodlines.log.");
                return;
            }
            if(!IsOwnerActive(context))return;
            var ped=Game.Player.Character;
            GameUtils.DrawObjectiveMarker(_point,System.Drawing.Color.FromArgb(140,100,210,160));
            ObjectiveMarkers.Navigation(_point, RequiredCharacter);
            if(ped==null||!ped.Exists()||ped.IsInVehicle()||!GameUtils.IsWithin(ped.Position,_point,2.5f))return;
            if(!Game.IsControlJustPressed(Control.Context))return;
            _requested=_enter ? BunkerSite.Enter(_access,context.Locations) : _access.Begin(_access.ExitPosition,null,false);
            if(!_requested)Fail("Bunker access was unavailable. Restart M23 from outside.");
        }
    }
}
