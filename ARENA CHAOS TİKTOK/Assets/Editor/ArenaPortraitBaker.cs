#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class ArenaPortraitBaker
{
    [MenuItem("Arena Chaos/Bake Fighter Portraits")]
    public static void Bake()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before baking portraits");
        var content=ArenaContent.Load();content.fighterPortraits=new Texture2D[content.fighters.Length];
        const string settingsPath="Assets/Resources/ArenaChaos/PowerUps.asset";
        if(AssetDatabase.LoadAssetAtPath<ArenaPowerSettings>(settingsPath)==null)
        {
            var settings=ScriptableObject.CreateInstance<ArenaPowerSettings>();settings.powers=new ArenaPowerUps.Settings[12];
            for(int i=0;i<12;i++)settings.powers[i]=new ArenaPowerUps.Settings{power=(ArenaPower)i};
            AssetDatabase.CreateAsset(settings,settingsPath);
        }
        string folder="Assets/ArenaChaos/Portraits";Directory.CreateDirectory(folder);
        var cameraObject=new GameObject("Portrait camera");var camera=cameraObject.AddComponent<Camera>();
        var target=new RenderTexture(160,160,24);var previous=RenderTexture.active;
        camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.10f,.06f);
        camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=30;
        GameObject actor=null;
        try
        {
            for(int i=0;i<content.fighters.Length;i++)
            {
                actor=Object.Instantiate(content.fighters[i]);actor.transform.position=new Vector3(1000,1000,1000);
                foreach(var animator in actor.GetComponentsInChildren<Animator>())animator.enabled=false;
                Bounds bounds=new Bounds();bool found=false;
                foreach(var renderer in actor.GetComponentsInChildren<Renderer>()){if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}
                Vector3 focus=bounds.center+Vector3.up*bounds.size.y*.2f;
                camera.orthographicSize=bounds.size.y*.36f;
                camera.transform.position=focus+new Vector3(0,bounds.size.y*.04f,bounds.size.y*2);
                camera.transform.LookAt(focus);camera.Render();RenderTexture.active=target;
                var image=new Texture2D(160,160,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,160,160),0,0);image.Apply();
                string path=folder+"/Fighter_"+i+".png";File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path);content.fighterPortraits[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Object.DestroyImmediate(actor);actor=null;
            }
            EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();
        }
        finally
        {
            if(actor!=null)Object.DestroyImmediate(actor);
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(cameraObject);
        }
    }
}
#endif
