using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SmoothFlow {
internal sealed class ScrollService : IDisposable {
    sealed class Policy { internal IntPtr Root; internal bool Allowed; internal string Name; internal Settings Configuration; internal ScrollOptions Options; }
    readonly object gate = new object();
    readonly Motion motion;
    readonly AutoResetEvent wake = new AutoResetEvent(false);
    readonly ManualResetEvent ready = new ManualResetEvent(false);
    readonly AutoResetEvent policyWake = new AutoResetEvent(false);
    readonly Thread hookThread, worker, policyWorker;
    readonly Stopwatch clock = Stopwatch.StartNew();
    readonly Native.HookProc callback;
    volatile Settings settings;
    volatile Policy policy = new Policy {Name=UiText.Get("Waiting")};
    volatile bool enabled, closing;
    volatile string fault = "";
    volatile string notice = "";
    IntPtr hook;
    uint hookThreadId;
    int outputCount, inputCount;
    int generation;
    IntPtr leafTarget, foregroundTarget;
    readonly int ownPid=Process.GetCurrentProcess().Id;
    internal IntPtr DemoWindow=IntPtr.Zero;
    internal bool Enabled {get {return enabled;} }
    internal string Fault {get {return fault;} }
    internal string Notice {get {return notice;} }
    internal string TargetName {get {return policy.Name;} }
    internal int OutputCount {get {return Interlocked.CompareExchange(ref outputCount,0,0);} }
    internal int InputCount {get {return Interlocked.CompareExchange(ref inputCount,0,0);} }
    internal ScrollService(Settings initial) {
        settings=initial.Snapshot(); motion=new Motion(initial.FineSteps); callback=OnMouse;
        hookThread=new Thread(HookLoop); hookThread.IsBackground=true; hookThread.Name="SmoothFlow input"; hookThread.SetApartmentState(ApartmentState.STA);
        worker=new Thread(WorkerLoop); worker.IsBackground=true; worker.Name="SmoothFlow animation";
        policyWorker=new Thread(PolicyLoop);policyWorker.IsBackground=true;policyWorker.Name="SmoothFlow application rules";
        hookThread.Start(); worker.Start();policyWorker.Start();
        if(!ready.WaitOne(4000)) fault=UiText.Get("HookTimeout");
    }
    internal void Configure(Settings s) {
        lock(gate) {
            settings=s.Snapshot();
            policy=new Policy {Name=UiText.Get("Updating")};
            motion.Clear();
            motion.FineSteps=s.FineSteps;
            Interlocked.Increment(ref generation);
        }
        wake.Set();policyWake.Set();
    }
    internal bool SetEnabled(bool value) {
        if(value)notice="";
        enabled=value && hook!=IntPtr.Zero && fault.Length==0;
        Cancel(); wake.Set(); return enabled;
    }
    internal void Cancel() { lock(gate) {motion.Clear();Interlocked.Increment(ref generation);} }
    void HookLoop() {
        try {
            hookThreadId=Native.GetCurrentThreadId();
            // Force a native message queue before publishing readiness.
            using(Control queueOwner=new Control()) {
                IntPtr ignored=queueOwner.Handle;
                hook=Native.SetWindowsHookEx(14,callback,Native.GetModuleHandle(null),0);
                if(hook==IntPtr.Zero) { fault=UiText.Get("HookError")+" · "+Marshal.GetLastWin32Error(); return; }
                ready.Set(); Application.Run();
            }
        } catch(Exception ex) { fault=UiText.Get("InputError")+" · "+ex.Message; enabled=false; }
        finally { ready.Set(); if(hook!=IntPtr.Zero) {Native.UnhookWindowsHookEx(hook); hook=IntPtr.Zero;} }
    }
    IntPtr OnMouse(int code, IntPtr message, IntPtr data) {
        try {
            if(code>=0 && enabled) {
                int msg=message.ToInt32(); Native.MouseHook m=(Native.MouseHook)Marshal.PtrToStructure(data,typeof(Native.MouseHook));
                // Own packets never re-enter smoothing. Other injected events pass through.
                if(m.Extra==Native.Marker) return Native.CallNextHookEx(hook,code,message,data);
                // Automated input is accepted only in our own test window.
                if((m.Flags&1)!=0 && Native.GetAncestor(Native.WindowFromPoint(m.Point),2)!=DemoWindow) return Native.CallNextHookEx(hook,code,message,data);
                if(msg==0x201 || msg==0x204 || msg==0x207 || msg==0x20B) Cancel();
                if(msg==Native.WM_WHEEL || msg==Native.WM_HWHEEL) {
                    int delta=(short)(m.Data>>16); Settings configuration=settings;
                    IntPtr leaf=Native.WindowFromPoint(m.Point), root=Native.GetAncestor(leaf,2); Policy p=policy;
                    ScrollOptions s=p.Options;
                    if(s==null || Native.Modifiers() || Native.Down(0x10) || delta==0 || delta%120!=0 || root!=p.Root || !Object.ReferenceEquals(configuration,p.Configuration) || !p.Allowed || (msg==Native.WM_HWHEEL && !s.Horizontal)) {Cancel(); return Native.CallNextHookEx(hook,code,message,data);}
                    bool accepted;
                    lock(gate) {
                        accepted=Object.ReferenceEquals(configuration,settings) && enabled;
                        if(accepted) {
                            if(leaf!=leafTarget || root!=motion.Target) {motion.Clear();Interlocked.Increment(ref generation);}
                            leafTarget=leaf;foregroundTarget=Native.GetForegroundWindow();
                            motion.Add(delta,msg==Native.WM_HWHEEL,root,clock.Elapsed.TotalMilliseconds,s);
                        }
                    }
                    if(!accepted)return Native.CallNextHookEx(hook,code,message,data);
                    Interlocked.Increment(ref inputCount); wake.Set(); return new IntPtr(1);
                }
            }
        } catch(Exception ex) { fault=UiText.Get("InputError")+" · "+ex.Message; enabled=false; Cancel(); }
        return Native.CallNextHookEx(hook,code,message,data);
    }
    void RefreshPolicy() {
        Settings configuration=settings;
        IntPtr root=Native.RootAtCursor(); uint pid; Native.GetWindowThreadProcessId(root,out pid);
        string name=UiText.Get("Unknown"); bool allowed=false;
        try {
            using(Process p=Process.GetProcessById((int)pid)) {
                name=p.ProcessName; allowed=Native.CanInject(pid) && (pid==(uint)ownPid ? root==DemoWindow : configuration.Allows(name,false));
            }
        } catch { }
        policy=new Policy {Root=root,Allowed=allowed,Name=name+(allowed ? " · "+UiText.Get("Enabled") : " · "+UiText.Get("Original")),Configuration=configuration,Options=configuration.OptionsFor(name)};
    }
    void PolicyLoop() {
        int checks=0;
        while(!closing) {
            try {
                RefreshPolicy();
                if(enabled && ++checks>=5) {
                    checks=0;
                    foreach(string name in new string[]{"SmoothScroll","smoothscroll-app"}) {
                        Process[] other=Process.GetProcessesByName(name);bool present=other.Length>0;
                        foreach(Process p in other)p.Dispose();
                        if(present){enabled=false;Cancel();notice=UiText.Get("AutoPaused");break;}
                    }
                }
            }catch { }
            policyWake.WaitOne(150);
        }
    }
    void WorkerLoop() {
        bool timerResolution=false;
        try {
            using(FramePacer pacer=new FramePacer()) {
            double nextFrame=0,period=1000.0/240;
            while(!closing) {
                double now=clock.Elapsed.TotalMilliseconds;
                bool active; lock(gate) active=enabled && motion.Active;
                if(!active) {
                    if(timerResolution) {Native.timeEndPeriod(1);timerResolution=false;}
                    nextFrame=0;
                    wake.WaitOne(100); continue;
                }
                if(!pacer.HighResolution && !timerResolution) timerResolution=Native.timeBeginPeriod(1)==0;
                if(nextFrame==0)nextFrame=now;
                if(now<nextFrame){pacer.WaitUntil(clock,nextFrame,wake);continue;}
                IntPtr target,leaf,foreground; int ticket;
                lock(gate) {target=motion.Target;leaf=leafTarget;foreground=foregroundTarget;ticket=generation;}
                IntPtr currentLeaf=Native.LeafAtCursor();
                if(currentLeaf!=leaf || Native.GetAncestor(currentLeaf,2)!=target || Native.GetForegroundWindow()!=foreground || Native.Modifiers() || Native.Down(0x10)) {Cancel();continue;}
                int vertical,horizontal;
                lock(gate) {if(ticket!=generation)continue;vertical=motion.Step(false,now);horizontal=motion.Step(true,now);}
                // Never hold gate across SendInput: the input hook can synchronously run.
                if(enabled && ticket==Interlocked.CompareExchange(ref generation,0,0) && vertical!=0) Send(vertical,false);
                if(enabled && ticket==Interlocked.CompareExchange(ref generation,0,0) && horizontal!=0) Send(horizontal,true);
                nextFrame=FramePacer.FollowingDeadline(nextFrame,clock.Elapsed.TotalMilliseconds,period);
            }
            }
        } catch(Exception ex) {fault=UiText.Get("ScrollError")+" · "+ex.Message;enabled=false;}
        finally {if(timerResolution) Native.timeEndPeriod(1);}
    }
    void Send(int delta,bool horizontal) {
        if(!Native.Inject(delta,horizontal)) {fault=UiText.Get("InputDenied");enabled=false;Cancel();}
        else Interlocked.Increment(ref outputCount);
    }
    public void Dispose() {
        if(closing)return; enabled=false; Cancel(); closing=true; wake.Set();policyWake.Set();
        if(hookThreadId!=0) Native.PostThreadMessage(hookThreadId,0x12,UIntPtr.Zero,IntPtr.Zero);
        bool stopped=worker.Join(2000),hookStopped=hookThread.Join(2000),policyStopped=policyWorker.Join(2000);
        if(stopped && hookStopped && policyStopped) {wake.Dispose();ready.Dispose();policyWake.Dispose();}
    }
}
}
