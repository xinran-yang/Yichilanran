using UnityEngine;
namespace YiChiLanRan
{
    public sealed class DebugOverlay : MonoBehaviour
    {
        public LevelRuntime world;
        void OnGUI()
        {
            if(!world || !world.Player) return;
            GUI.color=Color.white;
            float scale=Mathf.Max(.5f,Screen.height/900f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            var style=new GUIStyle(GUI.skin.label) {fontSize=18}; style.normal.textColor=Color.white;
            GUI.Box(new Rect(12,12,760,110),GUIContent.none);
            GUI.Label(new Rect(24,18,740,26),"A YARD OF INDIGO   /   "+world.definition.levelId,style);
            GUI.Label(new Rect(24,46,740,26),"Phase: "+world.Player.Entity.phase+"    Mirrored: "+world.Mirrored+"    Transfers: "+world.Transfers,style);
            GUI.Label(new Rect(24,74,740,26),"A/D or arrows: move   Space: jump   S/Down: dive   R: reset",style);
            GUI.Label(new Rect(24,128,1000,28),"Last: "+world.LastEvent+(world.Failure.Length>0 ? "   "+world.Failure : ""),style);
            if(world.Complete && string.IsNullOrEmpty(world.definition.nextScene))
            { GUI.Box(new Rect(320,280,600,100),"2.2 COMPLETE\nBoth greybox levels complete. Press R to restart."); }
            GUI.matrix=Matrix4x4.identity;
            if(world.ViewCamera)
            {
                var r=world.ViewCamera.rect; GUI.color=Color.black;
                if(r.x>0) { GUI.DrawTexture(new Rect(0,0,r.x*Screen.width,Screen.height),Texture2D.whiteTexture); GUI.DrawTexture(new Rect(r.xMax*Screen.width,0,r.x*Screen.width,Screen.height),Texture2D.whiteTexture); }
                if(r.y>0) { GUI.DrawTexture(new Rect(0,0,Screen.width,r.y*Screen.height),Texture2D.whiteTexture); GUI.DrawTexture(new Rect(0,r.yMax*Screen.height,Screen.width,r.y*Screen.height),Texture2D.whiteTexture); }
            }
            GUI.color=new Color(0,0,0,world.Fade); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); GUI.color=Color.white;
        }
    }
}
