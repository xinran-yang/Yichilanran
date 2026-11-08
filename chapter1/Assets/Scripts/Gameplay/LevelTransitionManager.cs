using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YiChiLanRan
{
    public sealed class LevelTransitionManager : MonoBehaviour
    {
        [Serializable] private sealed class Settings { public float fadeOutSeconds,fadeInSeconds,spawnReadyTimeoutSeconds; }
        public static LevelTransitionManager Instance { get; private set; }
        public bool IsTransitioning { get; private set; }
        public float Alpha => group?group.alpha:0;
        public int CompletedTransitions { get; private set; }
        public bool LoadedUnderBlack { get; private set; }
        private Settings settings;
        private CanvasGroup group;
        private void Awake()
        {
            if(Instance && Instance!=this){Destroy(gameObject);return;}
            Instance=this; DontDestroyOnLoad(gameObject);
            var asset=Resources.Load<TextAsset>("Configuration/LevelFlow");
            if(!asset)throw new InvalidOperationException("Missing LevelFlow settings");
            settings=JsonUtility.FromJson<Settings>(asset.text);
            var overlay=new GameObject("Transition black",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));
            overlay.transform.SetParent(transform,false);
            var canvas=overlay.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=32767;
            group=overlay.GetComponent<CanvasGroup>(); group.alpha=0;
            var panel=new GameObject("Black",typeof(RectTransform),typeof(Image)); panel.transform.SetParent(overlay.transform,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            panel.GetComponent<Image>().color=Color.black;
        }
        private void OnDestroy(){if(Instance==this)Instance=null;}
        public bool Begin(string nextScene,ChapterWorld current)
        {
            if(IsTransitioning || string.IsNullOrEmpty(nextScene))return false;
            if(!Application.CanStreamedLevelBeLoaded(nextScene)){Debug.LogError("Next scene unavailable: "+nextScene);return false;}
            IsTransitioning=true; current.player.SetControlLock(true);
            StartCoroutine(Transition(nextScene,current)); return true;
        }
        private IEnumerator Fade(float target,float duration)
        {
            float start=group.alpha,elapsed=0;
            while(elapsed<duration){elapsed+=Time.unscaledDeltaTime;group.alpha=Mathf.Lerp(start,target,duration>0?elapsed/duration:1);yield return null;}
            group.alpha=target;
        }
        private IEnumerator Transition(string scene,ChapterWorld previous)
        {
            yield return Fade(1,settings.fadeOutSeconds);
            AsyncOperation operation=null;
            try{operation=SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);}
            catch(Exception exception){Debug.LogException(exception);}
            if(operation==null){group.alpha=0;IsTransitioning=false;if(previous)previous.player.SetControlLock(false);yield break;}
            while(!operation.isDone)yield return null;
            LoadedUnderBlack=group.alpha==1;
            ChapterWorld next=null;float elapsed=0;
            while(elapsed<settings.spawnReadyTimeoutSeconds)
            {
                next=FindFirstObjectByType<ChapterWorld>();
                if(next && next.SpawnInitialized)break;
                elapsed+=Time.unscaledDeltaTime;yield return null;
            }
            if(!next || !next.SpawnInitialized){Debug.LogError("Loaded scene did not initialise its world");group.alpha=0;IsTransitioning=false;yield break;}
            next.player.SetControlLock(true);next.SnapCamera();
            // Let the loaded scene initialise rendering while the overlay is still fully opaque.
            yield return null;
            yield return Fade(0,settings.fadeInSeconds);
            next.player.SetControlLock(false);CompletedTransitions++;IsTransitioning=false;
        }
    }
}
