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
        private readonly List<string> _report=new List<string>();
        /// <summary>What Apply found: each property as applied (original -> new) or "unsupported on this runtime".</summary>
        public string Report => _report.Count == 0 ? "flight handling: nothing requested" : "flight handling: " + string.Join("; ", _report);
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
            if(child==null){_report.Add(parent+"."+name+" unsupported on this runtime");return;}
            var type=child.GetType();var valid=type.GetProperty("IsValid");var address=type.GetProperty("MemoryAddress");var field=type.GetProperty(name);
            if(valid==null||address==null||field==null||!field.CanWrite||!(bool)valid.GetValue(child,null)){_report.Add(parent+"."+name+" unsupported on this runtime");return;}
            object original=field.GetValue(child,null),applied;
            if(vector)
            {
                if(!(original is Vector3 v)||!Positive(v.Y)){_report.Add(name+" skipped (value "+original+")");return;}
                // Keep lateral and vertical damping, lift, thrust and steering.
                applied=new Vector3(v.X,v.Y*.5f,v.Z);
            }
            else {if(!(original is float f)||!Positive(f)){_report.Add(name+" skipped (value "+original+")");return;}applied=(float)original*.5f;}
            _report.Add(name+" "+original+" -> "+applied);
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
