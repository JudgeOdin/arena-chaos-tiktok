using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Bounded avatar cache. The LIVE adapter supplies stable viewer IDs and HTTPS photo URLs.</summary>
public sealed class ArenaViewerProfiles : MonoBehaviour
{
    sealed class Entry { public string url;public Texture2D photo;public float retryAt;public bool loading;public float used; }
    readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
    readonly HashSet<UnityWebRequest> requests=new HashSet<UnityWebRequest>();
    [SerializeField,Min(1)] int maximumCachedProfiles=64;
    [SerializeField,Range(1,4)] int maximumDownloads=2;
    int downloads;
    public Texture2D Resolve(string viewerId,string url)
    {
        if(string.IsNullOrEmpty(viewerId)||string.IsNullOrEmpty(url))return null;
        if(!Uri.TryCreate(url,UriKind.Absolute,out var address)||address.Scheme!=Uri.UriSchemeHttps)return null;
        if(!entries.TryGetValue(viewerId,out var entry))
        {
            if(entries.Count>=maximumCachedProfiles)
            {
                string oldest=null;float time=float.MaxValue;
                foreach(var pair in entries)if(!pair.Value.loading&&pair.Value.used<time){oldest=pair.Key;time=pair.Value.used;}
                if(oldest==null)return null;
                if(entries[oldest].photo!=null)Destroy(entries[oldest].photo);entries.Remove(oldest);
            }
            entry=new Entry{url=url};entries.Add(viewerId,entry);
        }
        entry.used=Time.unscaledTime;
        if(entry.url!=url&&!entry.loading)
        {if(entry.photo!=null)Destroy(entry.photo);entry.photo=null;entry.url=url;entry.retryAt=0;}
        if(entry.photo==null&&!entry.loading&&downloads<maximumDownloads&&Time.unscaledTime>=entry.retryAt)
            StartCoroutine(Download(entry));
        return entry.photo;
    }
    IEnumerator Download(Entry entry)
    {
        entry.loading=true;downloads++;
        using(var request=UnityWebRequestTexture.GetTexture(entry.url,true))
        {
            requests.Add(request);request.timeout=8;request.SetRequestHeader("Accept","image/png,image/jpeg");
            try
            {
                var operation=request.SendWebRequest();
                while(!operation.isDone)
                {
                    if(request.downloadedBytes>2*1024*1024){request.Abort();break;}
                    yield return null;
                }
                if(request.result==UnityWebRequest.Result.Success&&request.downloadedBytes<=2*1024*1024)
                {
                    var photo=DownloadHandlerTexture.GetContent(request);
                    if(photo.width<=1024&&photo.height<=1024)entry.photo=photo;else Destroy(photo);
                }
            }
            finally{requests.Remove(request);entry.loading=false;entry.retryAt=Time.unscaledTime+30;downloads--;}
        }
    }
    void OnDestroy()
    {
        foreach(var request in requests)request.Abort();
        foreach(var entry in entries.Values)if(entry.photo!=null)Destroy(entry.photo);
    }
}
