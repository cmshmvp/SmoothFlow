using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
namespace SmoothFlow {
internal static class Tests {
 static List<string> log=new List<string>();static void Check(bool ok,string label){if(!ok)throw new Exception(label);log.Add("PASS "+label);}
 static int Sum(Motion m,int finish){int n=0;for(int t=0;t<=finish;t++)n+=m.Step(false,t);return n;}
 static void CheckFineSteps(){
  const double period=1000.0/240;
  foreach(int count in new int[]{1,3}){
   // A single detent and three detents 50 ms apart from the same wheel.
   double[] last={0,0},gap={0,0},error={0,0};
   int[] total={0,0};
   Motion[] modes={new Motion(false),new Motion(true)};
   ScrollOptions options=new ScrollOptions{Reverse=false};
   for(int frame=0;frame<145;frame++){
    double now=frame*period;
    double[] ideal={0,0};
    for(int i=0;i<count;i++){
     double start=i*12*period;
     double t=Math.Max(0,Math.Min(1,(now-start)/options.Duration));
     double impulse=120*(i==0?1:1+(options.AccelerationMax-1)*(1-50.0/180));
     ideal[0]+=impulse*(1-Math.Pow(1-t,5)*(1+5*t));
     double u=Math.Max(0,(t-.4)/.6);
     double shifted=t-.8*u*u*(1-u)*(1-u);
     ideal[1]+=impulse*(1-Math.Pow(1-shifted,5)*(1+5*shifted));
    }
    for(int mode=0;mode<2;mode++){
     Motion m=modes[mode];
     if(frame%12==0 && frame/12<count)m.Add(120,false,new IntPtr(1),now,options);
     int delta=m.Step(false,now);
     if(delta<0)throw new Exception("unexpected reverse packet");
     if(delta>0){gap[mode]=Math.Max(gap[mode],now-last[mode]);last[mode]=now;}
     total[mode]+=delta;
     double positionError=Math.Abs(ideal[mode]-total[mode]);
     error[mode]=Math.Max(error[mode],positionError);
    }
   }
   Check(error[1]<=0.500001 && error[1]<error[0] && total[1]==total[0],
         "nearest-unit position and preserved total, detents="+count);
   Check(gap[1]<gap[0],"reduced longest output gap, detents="+count);
   log.Add(String.Format(CultureInfo.InvariantCulture,
     "TRACE detents={0} total={1} longest_gap_ms={2:F1}->{3:F1} peak_position_error_units={4:F3}->{5:F3}",
     count,total[1],gap[0],gap[1],error[0],error[1]));
  }
  ScrollOptions fractional=new ScrollOptions{Distance=0.2625,AccelerationMax=1,Reverse=true};
  Motion fine=new Motion();int x=0,y=0;
  for(int i=0;i<8;i++){
   fine.Add(120,false,new IntPtr(1),i*500,fractional);
   fine.Add(-120,true,new IntPtr(1),i*500,fractional);
   for(int dt=0;dt<500;dt+=7){int v=fine.Step(false,i*500+dt),h=fine.Step(true,i*500+dt);
    if(v>0||h<0||v!=-h)throw new Exception("axis symmetry or direction");y+=v;x+=h;}
  }
  Check(x==252&&y==-252,"preserve fractional totals and mirrored axes across idle gaps");
  fine.Add(120,false,new IntPtr(1),5000,fractional);fine.Step(false,5050);
  fine.Add(-120,false,new IntPtr(1),5050,fractional);
  Check(fine.Step(false,5100)>0,"reverse immediately without old tail");
  fine.Add(120,false,new IntPtr(2),5110,fractional);fine.Clear();
  Check(!fine.Active&&fine.Step(false,5200)==0,"cancel clears pending travel");
  Check(FramePacer.FollowingDeadline(8,19,4)==20 && FramePacer.FollowingDeadline(8,12,4)==16,
        "missed ticks advance to next deadline on original grid");
 }
 static void CheckTail(int duration,double formerLast){
  ScrollOptions options=new ScrollOptions{Duration=duration,Reverse=false};
  Motion motion=new Motion();
  motion.Add(120,false,new IntPtr(1),0,options);
  int total=0;double last=0,maxGap=0;
  for(int frame=0;frame<=150;frame++){
   double now=frame*1000.0/240;
   int delta=motion.Step(false,now);
   total+=delta;
   double t=Math.Min(1,now/duration);
   if(t<=0.4 && total!=(int)Math.Floor(120*(1-Math.Pow(1-t,5)*(1+5*t))+0.5))
    throw new Exception("early travel changed");
   if(delta<0)throw new Exception("tail moved backwards");
   if(delta>0){maxGap=Math.Max(maxGap,now-last);last=now;}
  }
  Check(total==120 && !motion.Active && last-formerLast>=12 && last-formerLast<=25 && maxGap<30,
        String.Format(CultureInfo.InvariantCulture,"single detent {0} ms: final output {1:F1} -> {2:F1} ms, max gap {3:F1} ms",duration,formerLast,last,maxGap));
 }
 internal static int Run(){string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test-results.txt");try{
  Settings s=new Settings();Check(s.Distance==1&&s.Duration==380&&s.AccelerationMax==1.3&&s.AccelerationDelta==180&&s.TailRatio==4,"restore accepted 0.2 settings 1.00 / 380 / 1.3");Check(s.Easing&&s.Reverse&&s.Horizontal&&!s.ShiftHorizontal,"restore 0.2 behavior switches");
  string[] rows=File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tests","legacy-0.2-trace.csv"));string current="";Motion motion=null;ScrollOptions options=null;int compared=0;
  for(int i=1;i<rows.Length;i++){
   string[] p=rows[i].Split(',');if(p[0]!=current){if(current!="")log.Add("PASS archived 0.2 trajectory: "+current);current=p[0];motion=new Motion(false);options=new ScrollOptions{Distance=current=="fractional"?.255:1,Duration=380,AccelerationMax=1.3,Easing=current!="cubic",Reverse=true};}
   double time=Double.Parse(p[1],CultureInfo.InvariantCulture);int delta=Int32.Parse(p[2]);if(p[5]=="1")motion.Clear();if(delta!=0)motion.Add(delta,p[3]=="1",new IntPtr(Int32.Parse(p[4])),time,options);
   int v=motion.Step(false,time),h=motion.Step(true,time);if(v!=Int32.Parse(p[6])||h!=Int32.Parse(p[7]))throw new Exception("legacy trajectory mismatch: "+current+" at "+time);compared++;
  }
  log.Add("PASS archived 0.2 trajectory: "+current);Check(compared==2080,"2080 archived frame pairs match bit for bit");
  Motion m=new Motion();s.Reverse=false;s.AccelerationMax=1;m.Add(120,false,new IntPtr(1),0,s);Check(Sum(m,450)==120,"distance 1 is exactly original wheel amount, no pixel conversion");
  m.Clear();s.Distance=2;m.Add(120,false,new IntPtr(1),0,s);Check(Sum(m,450)==240,"distance multiplier remains adjustable");
  Settings cfg=new Settings{DefaultEnabled=false};cfg.Profiles.Add(new AppProfile{ProcessName="Weixin",Distance=2,Enabled=true});Check(cfg.Allows("Weixin.exe",false)&&!cfg.Allows("notepad",false),"keep per-app enable rules");Settings copy=cfg.Snapshot();cfg.Profiles[0].Distance=3;Check(copy.OptionsFor("Weixin").Distance==2,"keep isolated per-app parameters");
  s.Distance=Double.NaN;s.AccelerationMax=7;s.Duration=-1;s.TailRatio=3;s.AccelerationDelta=70;s.ShiftHorizontal=true;s.Validate();Check(s.Distance==1&&s.AccelerationMax==3&&s.Duration==80&&s.TailRatio==4&&s.AccelerationDelta==180&&!s.ShiftHorizontal,"unsupported 0.3 options cannot change restored engine");Check(Marshal.SizeOf(typeof(Native.Input))==40,"x64 INPUT ABI");
  CheckFineSteps();
  CheckTail(380,287.5);
  CheckTail(430,325);
  int passed=log.FindAll(delegate(string line){return line.StartsWith("PASS ");}).Count;
  log.Add("RESULT "+passed+" passed; 0 failed");File.WriteAllLines(file,log.ToArray());return 0;
 }catch(Exception e){log.Add("FAIL "+e.Message);File.WriteAllLines(file,log.ToArray());return 1;}}
}
}
