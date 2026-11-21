using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
namespace SmoothFlow {
public class ScrollOptions {
 public double Distance=1; public int Duration=380; public int AccelerationDelta=180; public double AccelerationMax=1.3; public double TailRatio=4;
 public bool Easing=true,ShiftHorizontal=false,Horizontal=true,Reverse=true,Enabled=true;
 public void Validate(){Distance=Bound(Distance,.25,4,1);Duration=Math.Max(80,Math.Min(600,Duration));AccelerationDelta=180;AccelerationMax=Bound(AccelerationMax,1,3,1.3);TailRatio=4;ShiftHorizontal=false;}
 static double Bound(double x,double low,double high,double fallback){return Double.IsNaN(x)||Double.IsInfinity(x)?fallback:Math.Max(low,Math.Min(high,x));}
}
public class AppProfile:ScrollOptions {public string ProcessName="";public AppProfile Duplicate(){return (AppProfile)MemberwiseClone();}}
public class Settings:ScrollOptions {
 public bool FineSteps=true;
 public int SchemaVersion=4;public bool AutoStart=true,DefaultEnabled=true;public string Excluded="mstsc;parsecd;moonlight;gamebar;vmware;virtualboxvm";public List<AppProfile> Profiles=new List<AppProfile>();
 internal static readonly string FilePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.xml");
 public Settings Snapshot(){Settings s=(Settings)MemberwiseClone();s.Profiles=new List<AppProfile>();foreach(AppProfile p in Profiles)s.Profiles.Add(p.Duplicate());return s;}
 internal static string Normalize(string name){string s=(name??"").Trim().ToLowerInvariant();return s.EndsWith(".exe")?s.Substring(0,s.Length-4):s;}
 internal AppProfile Find(string name){string n=Normalize(name);foreach(AppProfile p in Profiles)if(Normalize(p.ProcessName)==n)return p;return null;}
 internal ScrollOptions OptionsFor(string name){AppProfile p=Find(name);return p==null?(ScrollOptions)this:p;}
 internal bool Allows(string name,bool own){if(own)return true;AppProfile p=Find(name);if(p!=null)return p.Enabled;foreach(string entry in (Excluded??"").Split(new char[]{';',',','\r','\n'},StringSplitOptions.RemoveEmptyEntries))if(Normalize(entry)==Normalize(name))return false;return DefaultEnabled;}
 internal static Settings Load(){try{string xml=File.ReadAllText(FilePath);if(!xml.Contains("<SchemaVersion>4</SchemaVersion>"))return new Settings();using(StringReader reader=new StringReader(xml)){Settings s=(Settings)new XmlSerializer(typeof(Settings)).Deserialize(reader);s.Validate();if(s.Profiles==null)s.Profiles=new List<AppProfile>();foreach(AppProfile p in s.Profiles)p.Validate();return s;}}catch{return new Settings();}}
 internal void Save(){string temp=FilePath+".tmp";using(FileStream f=File.Create(temp))new XmlSerializer(typeof(Settings)).Serialize(f,this);if(File.Exists(FilePath))File.Replace(temp,FilePath,null);else File.Move(temp,FilePath);}
}
internal class Motion {
 sealed class Impulse {internal double Start,Duration,Amount,Delivered;internal bool Soft;}
 sealed class Axis {internal List<Impulse> Items=new List<Impulse>();internal double Remainder,LastTime=-100000;internal int Direction;}
 internal bool FineSteps { get; set; }
 internal Motion(bool fineSteps=true){FineSteps=fineSteps;}
 readonly Axis[] axes={new Axis(),new Axis()};internal IntPtr Target{get;private set;}internal bool Active{get{return axes[0].Items.Count+axes[1].Items.Count>0;}}
 internal void Clear(){foreach(Axis a in axes)Reset(a);Target=IntPtr.Zero;}static void Reset(Axis a){a.Items.Clear();a.Remainder=0;a.LastTime=-100000;a.Direction=0;}
 // Preserve the exact arithmetic order and truncation of the accepted 0.2 engine.
 internal void Add(int delta,bool horizontal,IntPtr target,double now,ScrollOptions s){if(target!=Target){Clear();Target=target;}Axis a=axes[horizontal?1:0];int direction=Math.Sign(delta);if(direction==0)return;if(direction!=a.Direction)Reset(a);double gap=Math.Max(0,now-a.LastTime);double factor=1+(s.AccelerationMax-1)*Math.Max(0,1-gap/180.0);a.LastTime=now;a.Direction=direction;if(a.Items.Count>=128)Reset(a);a.Items.Add(new Impulse{Start=now,Duration=s.Duration,Amount=delta*s.Distance*factor*(s.Reverse?-1:1),Soft=s.Easing});}
 internal int Step(bool horizontal,double now){
  Axis axis=axes[horizontal?1:0];
  if(axis.Items.Count==0)return 0;
  double amount=axis.Remainder;
  int sign=Math.Sign(axis.Items[0].Amount);
  for(int i=axis.Items.Count-1;i>=0;i--){
   Impulse p=axis.Items[i];
   double t=Math.Max(0,Math.Min(1,(now-p.Start)/p.Duration));
   bool finished=t>=1;
   if(FineSteps && p.Soft && t>0.4){
    // Delay late travel by at most 5% of the duration. The offset and its
    // slope vanish at both ends, preserving the onset and final distance.
    double u=(t-0.4)/0.6;
    double blend=4*u*(1-u);
    t-=0.05*blend*blend;
   }
   double curve=p.Soft?t*t*(15+t*(-40+t*(45+t*(-24+5*t)))):1-Math.Pow(1-t,3);
   double value=p.Amount*curve;
   amount+=value-p.Delivered;
   p.Delivered=value;
   if(finished)axis.Items.RemoveAt(i);
  }
  // Round cumulative travel at the midpoint of each wheel unit. Resolve ties
  // in the travel direction so a stationary half-unit cannot oscillate.
  int result=FineSteps?sign*(int)Math.Floor(sign*amount+0.5):(int)Math.Truncate(amount);
  axis.Remainder=amount-result;
  return result;
 }
}
}
