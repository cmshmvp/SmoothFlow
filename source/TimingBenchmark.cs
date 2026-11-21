using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SmoothFlow {
internal static class TimingBenchmark {
    internal static int Run() {
        List<string> result=new List<string>();int exit=0;
        Application.EnableVisualStyles();
        using(Form hidden=new Form {Text="SmoothFlow timing benchmark",ShowInTaskbar=false,WindowState=FormWindowState.Minimized,Opacity=0}) {
            hidden.Shown+=delegate {
                hidden.Hide();
                Thread worker=new Thread(delegate() {
                    try {
                        // Exercise a window-owning, hidden process as in tray usage.
                        Thread.Sleep(5000);
                        using(AutoResetEvent signal=new AutoResetEvent(false)) {
                            Native.timeBeginPeriod(1);
                            try {
                                List<double> old=new List<double>();Stopwatch clock=Stopwatch.StartNew();double previous=clock.Elapsed.TotalMilliseconds;
                                for(int i=0;i<240;i++){signal.WaitOne(8);double now=clock.Elapsed.TotalMilliseconds;old.Add(now-previous);previous=now;}
                                Summarize(result,"0.1 relative WaitOne(8), hidden window",old,8);
                            } finally {Native.timeEndPeriod(1);}
                            using(FramePacer pacer=new FramePacer()) {
                                result.Add("high_resolution_timer="+pacer.HighResolution);
                                Stopwatch sameClock=Stopwatch.StartNew();double sameNext=8,samePrevious=0;List<double> sameIntervals=new List<double>();
                                for(int i=0;i<240;i++){pacer.WaitUntil(sameClock,sameNext,signal);double now=sameClock.Elapsed.TotalMilliseconds;sameIntervals.Add(now-samePrevious);samePrevious=now;sameNext+=8;if(sameNext<now)sameNext=now+8;}
                                Summarize(result,"0.2 absolute native timer, same 8 ms target",sameIntervals,8);
                                Stopwatch clock=Stopwatch.StartNew();double period=1000.0/240,next=period,previous=0;List<double> intervals=new List<double>();
                                for(int i=0;i<400;i++){pacer.WaitUntil(clock,next,signal);double now=clock.Elapsed.TotalMilliseconds;intervals.Add(now-previous);previous=now;next+=period;if(next<now)next=now+period;}
                                Summarize(result,"0.2 absolute native timer, hidden window",intervals,period);
                            }
                        }
                        result.Add("These are scheduler intervals, not measured WeChat rendering FPS.");
                    }catch(Exception ex){result.Add("ERROR "+ex);exit=1;}
                    finally {hidden.BeginInvoke((Action)delegate {hidden.Close();});}
                });worker.IsBackground=true;worker.Start();
            };
            Application.Run(hidden);
        }
        File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"timing-benchmark.txt"),result.ToArray());return exit;
    }
    static void Summarize(List<string> output,string label,List<double> data,double target) {
        double sum=0,max=0;int late=0;foreach(double value in data){sum+=value;max=Math.Max(max,value);if(value>target*1.5)late++;}
        data.Sort();output.Add(label);output.Add(String.Format(CultureInfo.InvariantCulture,"samples={0}, mean_ms={1:F3}, p50_ms={2:F3}, p95_ms={3:F3}, max_ms={4:F3}, over_1.5x={5}, deadline_drift_ms={6:F3}",data.Count,sum/data.Count,data[data.Count/2],data[(int)(data.Count*.95)],max,late,sum-target*data.Count));
    }
}
}
