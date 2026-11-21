using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
namespace SmoothFlow {
internal static class Program {
 [STAThread] static int Main(string[] args){
  if(Array.IndexOf(args,"--self-test")>=0)return Tests.Run();if(Array.IndexOf(args,"--timing-benchmark")>=0)return TimingBenchmark.Run();
  try{if(!Native.SetProcessDpiAwarenessContext(new IntPtr(-4)))Native.SetProcessDPIAware();}catch(EntryPointNotFoundException){Native.SetProcessDPIAware();}
  bool first;using(Mutex single=new Mutex(true,"Local\\SmoothFlow.Personal.0_1",out first)){
   if(!first){UiText.SelectLanguage(Settings.Load().InterfaceLanguage);MessageBox.Show(UiText.Get("Running"),"SmoothFlow");return 0;}
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   try{Application.Run(new MainForm(Array.IndexOf(args,"--tray")>=0));return 0;}catch(Exception ex){MessageBox.Show(ex.Message,"SmoothFlow");return 1;}
  }
 }
}
internal sealed partial class MainForm:Form {
 Settings settings=Settings.Load();readonly ScrollService service;readonly Icon appIcon=LoadIcon();readonly NotifyIcon tray=new NotifyIcon();
 readonly CheckBox autoStart=new CheckBox(),globalEnabled=new CheckBox(),defaultEnabled=new CheckBox(),easing=new CheckBox(),horizontal=new CheckBox(),reverse=new CheckBox();
 readonly ComboBox profiles=new ComboBox();readonly NumericUpDown step=new NumericUpDown(),duration=new NumericUpDown(),accelerationMax=new NumericUpDown();
 readonly Label status=new Label();readonly Button remove=new Button();readonly ToolTip tips=new ToolTip();readonly System.Windows.Forms.Timer refresh=new System.Windows.Forms.Timer(),saveTimer=new System.Windows.Forms.Timer();
 bool updating,exitRequested,hotkey;string saveError="",startupError="",enableNotice="";
 internal MainForm(bool startInTray){
  settings.InterfaceLanguage=UiText.SelectLanguage(settings.InterfaceLanguage);
  SuspendLayout();Text="SmoothFlow "+ReleaseInfo.Version;Icon=appIcon;BackColor=Color.FromArgb(241,241,241);ForeColor=Color.FromArgb(40,40,40);Font=new Font("Segoe UI",11F);AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(874,544);MinimumSize=new Size(850,554);StartPosition=FormStartPosition.CenterScreen;
  BuildLayout();
  help.Click+=delegate{MessageBox.Show(this,UiText.Get("FineHelp")+"\n\n"+UiText.Get("DistanceTip")+"\n"+UiText.Get("ShiftTip")+"\n\n"+UiText.Get("CloseHint")+"\nCtrl+Alt+S · "+UiText.Get("Toggle"),UiText.Get("Help"));};
  reset.Click+=delegate{bool login=settings.AutoStart,on=settings.Enabled;settings=new Settings{AutoStart=login,Enabled=on,InterfaceLanguage=settings.InterfaceLanguage};ReloadProfiles(0);Changed();};add.Click+=delegate{AddProfile();};remove.Click+=delegate{if(profiles.SelectedIndex>0){settings.Profiles.RemoveAt(profiles.SelectedIndex-1);ReloadProfiles(0);Changed();}};
  service=new ScrollService(settings);ReloadProfiles(0);
  foreach(NumericUpDown n in new NumericUpDown[]{step,duration,accelerationMax})n.ValueChanged+=delegate{Changed();};foreach(CheckBox c in new CheckBox[]{defaultEnabled,easing,horizontal,reverse})c.CheckedChanged+=delegate{Changed();};profiles.SelectedIndexChanged+=delegate{if(!updating)LoadProfile();};
  globalEnabled.CheckedChanged+=delegate{if(!updating)SetEnabled(globalEnabled.Checked);};autoStart.CheckedChanged+=delegate{if(updating)return;try{StartupManager.Apply(autoStart.Checked);startupError="";settings.AutoStart=autoStart.Checked;QueueSave();}catch(Exception ex){startupError=ex.Message;}};
  saveTimer.Interval=500;saveTimer.Tick+=delegate{saveTimer.Stop();Save();};refresh.Interval=250;refresh.Tick+=delegate{RefreshStatus();};refresh.Start();
  ContextMenuStrip menu=new ContextMenuStrip();

  fineSteps.CheckOnClick=true;fineSteps.Checked=settings.FineSteps;
  menu.Opening+=delegate{fineSteps.Checked=settings.FineSteps;};
  fineSteps.Click+=delegate{settings.FineSteps=fineSteps.Checked;service.Configure(settings);Save();};
  menu.Items.Add(fineSteps);
  menu.Items.Add(UiText.Get("Open"),null,delegate{Show();WindowState=FormWindowState.Normal;Activate();});menu.Items.Add(UiText.Get("Toggle"),null,delegate{SetEnabled(!service.Enabled);});menu.Items.Add(UiText.Get("Quit"),null,delegate{exitRequested=true;Close();});tray.Icon=appIcon;tray.ContextMenuStrip=menu;tray.Visible=true;tray.DoubleClick+=delegate{Show();WindowState=FormWindowState.Normal;Activate();};
  Translate();
  language.SelectedIndexChanged+=delegate{
   if(updating || language.SelectedIndex<0)return;
   settings.InterfaceLanguage=UiText.SelectLanguage(languageIds[language.SelectedIndex]);
   Translate();Save();
  };
  Resize+=delegate{if(WindowState==FormWindowState.Minimized)Hide();};FormClosing+=delegate(object sender,FormClosingEventArgs e){if(!exitRequested&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Save();Hide();}};
  Shown+=delegate{hotkey=Native.RegisterHotKey(Handle,1,0x4003,0x53);try{StartupManager.Apply(settings.AutoStart);}catch(Exception ex){startupError=ex.Message;}SetEnabled(settings.Enabled);Save();if(startInTray)BeginInvoke((Action)delegate{Hide();});};
  FormClosed+=delegate{refresh.Stop();saveTimer.Stop();Save();tray.Visible=false;tray.Dispose();if(hotkey)Native.UnregisterHotKey(Handle,1);service.Dispose();appIcon.Dispose();tips.Dispose();};
  float scale;using(Graphics dpi=Graphics.FromHwnd(IntPtr.Zero))scale=dpi.DpiX/96F;if(scale!=1){foreach(Control child in Controls)ScaleUi(child,scale);ClientSize=new Size(Scale(874,scale),Scale(544,scale));MinimumSize=new Size(Scale(850,scale),Scale(554,scale));}ResumeLayout(false);PerformLayout();RefreshStatus();
 }
 static Icon LoadIcon(){using(Stream s=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("SmoothFlow.AppIcon"))using(Icon icon=new Icon(s,new Size(32,32)))return (Icon)icon.Clone();}
 ScrollOptions Current(){return profiles.SelectedIndex>0?settings.Profiles[profiles.SelectedIndex-1]:(ScrollOptions)settings;}
 void ReloadProfiles(int index){updating=true;profiles.Items.Clear();profiles.Items.Add(UiText.Get("DefaultProfile"));foreach(AppProfile p in settings.Profiles)profiles.Items.Add(p.ProcessName+".exe");profiles.SelectedIndex=Math.Max(0,Math.Min(index,profiles.Items.Count-1));autoStart.Checked=settings.AutoStart;updating=false;LoadProfile();}
 void LoadProfile(){updating=true;ScrollOptions o=Current();step.Value=(decimal)o.Distance;duration.Value=o.Duration;accelerationMax.Value=(decimal)o.AccelerationMax;defaultEnabled.Text=profiles.SelectedIndex==0?UiText.Get("AllApps"):UiText.Get("ThisApp");defaultEnabled.AccessibleName=defaultEnabled.Text;defaultEnabled.Checked=profiles.SelectedIndex==0?settings.DefaultEnabled:o.Enabled;easing.Checked=o.Easing;horizontal.Checked=o.Horizontal;reverse.Checked=o.Reverse;remove.Visible=profiles.SelectedIndex>0;updating=false;}
 void Changed(){if(updating)return;ScrollOptions o=Current();o.Distance=(double)step.Value;o.Duration=(int)duration.Value;o.AccelerationMax=(double)accelerationMax.Value;o.Easing=easing.Checked;o.Horizontal=horizontal.Checked;o.Reverse=reverse.Checked;if(profiles.SelectedIndex==0)settings.DefaultEnabled=defaultEnabled.Checked;else o.Enabled=defaultEnabled.Checked;service.Configure(settings);QueueSave();}
 void QueueSave(){saveTimer.Stop();saveTimer.Start();}void Save(){try{settings.Save();saveError="";}catch(Exception ex){saveError=ex.Message;}}
 void SetEnabled(bool value){if(value&&OtherEngine()){service.SetEnabled(false);settings.Enabled=false;enableNotice="OtherEngine";}else{service.SetEnabled(value);settings.Enabled=service.Enabled;enableNotice="";}Save();RefreshStatus();}
 static bool OtherEngine(){foreach(string name in new string[]{"SmoothScroll","smoothscroll-app"}){Process[] ps=Process.GetProcessesByName(name);bool found=ps.Length>0;foreach(Process p in ps)p.Dispose();if(found)return true;}return false;}
 void RefreshStatus(){updating=true;globalEnabled.Checked=service.Enabled;updating=false;status.Text=service.Fault.Length>0?service.Fault:service.Notice.Length>0?service.Notice:enableNotice.Length>0?UiText.Get(enableNotice):startupError.Length>0?UiText.Get("StartupError")+" · "+startupError:saveError.Length>0?saveError:(service.Enabled?UiText.Get("Enabled"):UiText.Get("Paused"))+"  ·  "+UiText.Get("CloseHint")+"  ·  Ctrl+Alt+S";tray.Text="SmoothFlow · "+(service.Enabled?UiText.Get("Enabled"):UiText.Get("Paused"));}
 void AddProfile(){using(Form dialog=new Form{Text=UiText.Get("AddApp"),Font=Font,StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(380,140),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){Label hint=new Label{Text=UiText.Get("ProcessHint"),Location=new Point(16,12),Size=new Size(345,25)};TextBox name=new TextBox{AccessibleName=UiText.Get("ProcessHint"),Location=new Point(16,44),Width=345};Button ok=new Button{Text=UiText.Get("Add"),Location=new Point(265,87),Size=new Size(95,30),DialogResult=DialogResult.OK};dialog.Controls.AddRange(new Control[]{hint,name,ok});dialog.AcceptButton=ok;ScaleDialog(dialog);if(dialog.ShowDialog(this)!=DialogResult.OK)return;string key=Settings.Normalize(name.Text);if(key.Length==0||key.IndexOfAny(new char[]{'\\','/',':','*','?'})>=0){MessageBox.Show(this,UiText.Get("BadName"));return;}AppProfile existing=settings.Find(key);if(existing!=null){profiles.SelectedIndex=settings.Profiles.IndexOf(existing)+1;return;}ScrollOptions o=Current();settings.Profiles.Add(new AppProfile{ProcessName=key,Distance=o.Distance,Duration=o.Duration,AccelerationDelta=o.AccelerationDelta,AccelerationMax=o.AccelerationMax,TailRatio=o.TailRatio,Easing=o.Easing,ShiftHorizontal=o.ShiftHorizontal,Horizontal=o.Horizontal,Reverse=o.Reverse,Enabled=true});ReloadProfiles(settings.Profiles.Count);Changed();}}
 static void ScaleDialog(Form dialog){dialog.AutoScaleMode=AutoScaleMode.None;using(Graphics g=Graphics.FromHwnd(IntPtr.Zero)){float f=g.DpiX/96F;if(f==1)return;foreach(Control child in dialog.Controls)ScaleUi(child,f);dialog.ClientSize=new Size(Scale(380,f),Scale(140,f));}}
 protected override void WndProc(ref Message m){if(m.Msg==0x312&&m.WParam.ToInt32()==1){SetEnabled(!service.Enabled);return;}base.WndProc(ref m);}
 static int Scale(int v,float f){return (int)Math.Round(v*f);}static Padding Pad(Padding p,float f){return new Padding(Scale(p.Left,f),Scale(p.Top,f),Scale(p.Right,f),Scale(p.Bottom,f));}
 static void ScaleUi(Control c,float f){c.SuspendLayout();c.Margin=Pad(c.Margin,f);c.Padding=Pad(c.Padding,f);c.MinimumSize=new Size(Scale(c.MinimumSize.Width,f),Scale(c.MinimumSize.Height,f));TableLayoutPanel t=c as TableLayoutPanel;if(t!=null){foreach(RowStyle r in t.RowStyles)if(r.SizeType==SizeType.Absolute)r.Height*=f;foreach(ColumnStyle col in t.ColumnStyles)if(col.SizeType==SizeType.Absolute)col.Width*=f;}c.SetBounds(Scale(c.Left,f),Scale(c.Top,f),Scale(c.Width,f),Scale(c.Height,f));foreach(Control child in c.Controls)ScaleUi(child,f);c.ResumeLayout(false);}
}
}
