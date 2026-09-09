using System;
using System.Collections.Generic;
using System.Reflection;
using GTA;
using GTA.Math;

namespace Bloodlines.Core
{
    // Use the installed Enhanced SDK's public handling API when present. Keep
    // the 3.6 compile target; never guess build-specific memory offsets.
    public sealed class TravelHandling
    {
        private sealed class Change
        {
            public PropertyInfo Parent, Field, Address, Valid;
            public IntPtr Pointer;
            public object Original, Applied;
        }
        private readonly List<Change> _changes=new List<Change>();
        public void Apply(HandlingData data,Model model)
        {
            if(model.IsPlane||model.IsHelicopter)
            {
                Adjust(data,"FlyingHandlingData","ThrustFallOff",false);
                Adjust(data,"FlyingHandlingData","VectorSpeedResistance",true);
            }
            if(model.IsBoat)Adjust(data,"BoatHandlingData","DragCoefficient",false);
        }
        private void Adjust(HandlingData data,string parent,string name,bool vector)
        {
            var getter=typeof(HandlingData).GetProperty(parent);var child=getter?.GetValue(data,null);
            if(child==null)return;
            var type=child.GetType();var valid=type.GetProperty("IsValid");var address=type.GetProperty("MemoryAddress");var field=type.GetProperty(name);
            if(valid==null||address==null||field==null||!field.CanWrite||!(bool)valid.GetValue(child,null))return;
            object original=field.GetValue(child,null),applied;
            if(vector)
            {
                if(!(original is Vector3 v)||!Positive(v.Y))return;
                // Keep lateral and vertical damping, lift, thrust and steering.
                applied=new Vector3(v.X,v.Y*.5f,v.Z);
            }
            else {if(!(original is float f)||!Positive(f))return;applied=(float)original*.5f;}
            var change=new Change{Parent=getter,Field=field,Valid=valid,Address=address,Pointer=(IntPtr)address.GetValue(child,null),Original=original,Applied=applied};
            _changes.Add(change); // Retain baseline even if a setter throws after writing.
            field.SetValue(child,applied,null);
        }
        private static bool Positive(float n) => n>0 && !float.IsNaN(n) && !float.IsInfinity(n);
        public bool Restore(HandlingData live)
        {
            foreach(var c in _changes.ToArray())
            {
                var child=c.Parent.GetValue(live,null);
                if(child==null||!(bool)c.Valid.GetValue(child,null)||(IntPtr)c.Address.GetValue(child,null)!=c.Pointer)continue;
                // A later mod owns values that no longer match our edit.
                if(c.Field.GetValue(child,null).Equals(c.Applied))c.Field.SetValue(child,c.Original,null);
                _changes.Remove(c);
            }
            return _changes.Count==0;
        }
    }
}
