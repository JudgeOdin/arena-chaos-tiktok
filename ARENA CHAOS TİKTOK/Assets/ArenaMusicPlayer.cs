using UnityEngine;

/// <summary>The looping soundtrack survives scene and round changes.</summary>
public sealed class ArenaMusicPlayer : MonoBehaviour
{
    static ArenaMusicPlayer instance;
    public static AudioSource CurrentSource=>instance!=null?instance.source:null;
    AudioSource source;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession(){instance=null;}
    public static AudioSource GetOrCreate(AudioClip clip)
    {
        if(instance==null)
        {
            var obj=new GameObject("Persistent arena soundtrack");instance=obj.AddComponent<ArenaMusicPlayer>();
            DontDestroyOnLoad(obj);instance.source=obj.AddComponent<AudioSource>();instance.source.playOnAwake=false;instance.source.loop=true;instance.source.volume=0;
        }
        if(clip!=null&&instance.source.clip!=clip){instance.source.clip=clip;instance.source.Play();}
        return instance.source;
    }
    void OnDestroy(){if(instance==this)instance=null;}
}
