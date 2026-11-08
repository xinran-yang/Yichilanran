using System;
using System.IO;
using UnityEditor;
namespace YiChiLanRan.Editor
{
    [InitializeOnLoad]
    public static class GreyboxBridge
    {
        static GreyboxBridge() { EditorApplication.update+=Poll; }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            const string request="DevReports/editor-command.txt";
            if(!File.Exists(request)) return;
            string text=File.ReadAllText(request).Trim();
            if(text.Length==0) return;
            if(text==SessionState.GetString("YiChiLanRan.GreyboxCommand","")) return;
            SessionState.SetString("YiChiLanRan.GreyboxCommand",text);
            try
            {
                var command=text.Split('|');
                if(command[1]=="sandbox") { GreyboxBuilder.Sandbox(); GreyboxChecks.Run(); }
                else if(command[1]=="checks") GreyboxChecks.Run();
                else Dispatch(command[1]);
                File.WriteAllText("DevReports/editor-response.txt",text+"\nSUCCESS\n"+DateTime.Now.ToString("O"));
            }
            catch(Exception e) { File.WriteAllText("DevReports/editor-response.txt",text+"\nFAILED\n"+e); UnityEngine.Debug.LogException(e); }
        }
        public static Action<string> AdditionalCommand;
        static void Dispatch(string command)
        {
            if(AdditionalCommand!=null) AdditionalCommand(command);
            else throw new InvalidOperationException("Unknown command: "+command);
        }
    }
}

