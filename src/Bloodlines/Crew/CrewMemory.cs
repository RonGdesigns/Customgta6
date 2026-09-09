using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Missions;
using GTA;
using GTA.Native;
namespace Bloodlines.Crew
{
    /// <summary>Per-character free-roam vitals and ammunition. Mission deployments start fresh.</summary>
    public sealed class CrewMemory
    {
        private readonly CampaignState _state;
        private int _nextCapture;
        public CrewMemory(CampaignState state) { _state=state; }
        public void Update(CrewRoster crew, bool freeRoam)
        {
            if (!freeRoam || !crew.IsDeployed || Game.GameTime < _nextCapture) return;
            _nextCapture = Game.GameTime + 30000;
            Capture(crew); _state.Save();
        }
        public void Capture(CrewRoster crew)
        {
            foreach(var hero in Protagonist.All)
            {
                var ped=crew.PedFor(hero.Slot);
                // Recovery owns downed heroes. Never persist a dead or unavailable ped
                // as a healthy template, nor inject death state at the next deployment.
                if(ped==null||!ped.Exists()||ped.IsDead)continue;
                var ammo=new Dictionary<string,object>();
                var owned=_state.Weapons.TryGetValue(hero.Slot.ToString(),out var weapons)?weapons:new HashSet<uint>();
                foreach(var hash in owned.Concat(hero.Loadout.Select(w=>(uint)w)).Distinct())
                    if(Function.Call<bool>(Hash.IS_WEAPON_VALID,hash)&&Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON,ped,hash,false))
                        ammo[hash.ToString()]=Math.Max(0,Math.Min(9999,Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON,ped,hash)));
                _state.CharacterMemory[hero.Slot.ToString()]=new Dictionary<string,object>{
                    {"health",ped.Health},{"armor",ped.Armor},{"ammo",ammo}};
            }
        }
        public void Restore(CrewRoster crew)
        {
            foreach(var hero in Protagonist.All)
            {
                var ped=crew.PedFor(hero.Slot);
                if(ped==null||!ped.Exists()||ped.IsDead||!_state.CharacterMemory.TryGetValue(hero.Slot.ToString(),out var value))continue;
                var record=Json.Object(value);
                int health=Math.Max(101,Math.Min(CrewDurability.Health,Json.Int(record,"health",CrewDurability.Health)));
                int armor=Math.Max(0,Math.Min(CrewDurability.Armor,Json.Int(record,"armor",CrewDurability.Armor)));
                CrewDurability.RestoreAfterSwitch(ped,health,armor);
                var ammo=Json.Object(record.TryGetValue("ammo",out var a)?a:null);
                foreach(var pair in ammo)
                    if(uint.TryParse(pair.Key,out var hash)&&int.TryParse(pair.Value?.ToString(),out var count)&&
                        Function.Call<bool>(Hash.IS_WEAPON_VALID,hash)&&Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON,ped,hash,false))
                        Function.Call(Hash.SET_PED_AMMO,ped,hash,Math.Max(0,Math.Min(9999,count)),false);
            }
        }
    }
}
