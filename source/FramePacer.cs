using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace SmoothFlow {
// One-shot, high-resolution deadlines. No busy waiting and no accumulating
// "work duration + sleep duration" error; a wake signal never resets the phase.
internal sealed class FramePacer : IDisposable {
    sealed class TimerHandle : WaitHandle {internal TimerHandle(IntPtr value){SafeWaitHandle=new SafeWaitHandle(value,true);}}
    readonly TimerHandle timer;
    WaitHandle[] waits;
    internal bool HighResolution {get{return timer!=null;}}
    // Keep the phase of the active gesture when a wake-up or dispatch is late.
    internal static double FollowingDeadline(double previous,double now,double period) {
        double elapsed=Math.Max(0,now-previous);
        return previous+(Math.Floor(elapsed/period)+1)*period;
    }
    internal FramePacer() {
        IntPtr h=Native.CreateWaitableTimerEx(IntPtr.Zero,null,2,0x1F0003);
        if(h!=IntPtr.Zero)timer=new TimerHandle(h);
    }
    internal void WaitUntil(Stopwatch clock,double deadline,WaitHandle interrupt) {
        double remaining=deadline-clock.Elapsed.TotalMilliseconds;
        if(remaining<=0)return;
        if(timer==null){interrupt.WaitOne(Math.Max(1,(int)Math.Ceiling(remaining)));return;}
        long due=-(long)Math.Max(1,Math.Ceiling(remaining*10000));
        if(!Native.SetWaitableTimer(timer.SafeWaitHandle.DangerousGetHandle(),ref due,0,IntPtr.Zero,IntPtr.Zero,false))throw new Win32Exception(Marshal.GetLastWin32Error());
        if(waits==null)waits=new WaitHandle[]{timer,interrupt};
        WaitHandle.WaitAny(waits);
    }
    public void Dispose(){if(timer!=null)timer.Dispose();}
}
}
