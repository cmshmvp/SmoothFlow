using Microsoft.Win32;
using System.Windows.Forms;
namespace SmoothFlow {
internal static class StartupManager {
 internal static void Apply(bool enabled){
  using(RegistryKey run=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){
   if(enabled)run.SetValue("SmoothFlow","\""+Application.ExecutablePath+"\" --resume --tray");else run.DeleteValue("SmoothFlow",false);
  }
 }
}
}
