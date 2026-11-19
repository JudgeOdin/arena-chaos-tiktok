using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Imported dungeon dressing; combat footprint stays 16 by 16 metres.</summary>
public static class KayKitArenaEnvironment
{
    public static Transform Build(float radius, Renderer ground)
    {
        var content = ArenaContent.Load();
        var root = new GameObject("ArenaVisuals").transform;
        if(ground!=null)ground.sharedMaterial=content.sand;
        for(int side=0;side<4;side++)
        {
            Quaternion rotation=Quaternion.Euler(0,side*90,0);
            Vector3 outward=rotation*Vector3.forward;
            Vector3 right=rotation*Vector3.right;
            for(int i=-7;i<=7;i+=2)
            {
                if((side==1||side==3)&&Mathf.Abs(i)<2)continue;
                var wall=Place(content.Find("wall_half"),root,outward*radius+right*i+Vector3.up*.6f,new Vector3(2,1.2f,.48f),rotation);
                // Independent physical wall, sized in world coordinates.
                var boundary=new GameObject("Arena Boundary",typeof(BoxCollider));boundary.transform.SetParent(root,false);
                boundary.transform.SetPositionAndRotation(outward*radius+right*i+Vector3.up*.8f,rotation);
                boundary.GetComponent<BoxCollider>().size=new Vector3(2,1.6f,.35f);
            }
            for(int i=-1;i<=1;i+=2)
            {
                Place(content.Find("column"),root,outward*(radius+.2f)+right*i*(radius-.3f)+Vector3.up*1.5f,new Vector3(1,3,1),rotation);
                Place(content.Find(side%2==0?"banner_red":"banner_blue"),root,outward*(radius+.55f)+right*i*5.8f+Vector3.up*2.2f,new Vector3(.9f,2.6f,.25f),rotation);
            }
        }
        // Broken stone border around the warm sand floor.
        for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++)
        {
            if(Mathf.Abs(x)<3&&Mathf.Abs(z)<3)continue;
            Place(content.Find("floor_tile_large"),root,new Vector3(x*2.2f,-.02f,z*2.2f),new Vector3(2.15f,.08f,2.15f),Quaternion.identity);
        }
        // Complete the palace apron in every camera direction, beyond the stands.
        for(int x=-8;x<=8;x++)for(int z=-8;z<=8;z++)
        {
            if(Mathf.Abs(x)<4&&Mathf.Abs(z)<4)continue;
            Place(content.Find((Mathf.Abs(x)==5||Mathf.Abs(z)==5)?"floor_wood_large":"floor_tile_large"),root,
                new Vector3(x*2.7f,-.14f,z*2.7f),new Vector3(2.68f,.14f,2.68f),Quaternion.Euler(0,((x+z)&1)*90,0));
        }
        // Recessed arcades and raised galleries give the hall several layers of depth.
        for(int x=-3;x<=3;x++)
        {
            var arch=Place(content.Find("wall_doorway"),root,new Vector3(x*4.5f,3.4f,18.8f),new Vector3(4.2f,6.8f,1.1f),Quaternion.identity);
            foreach(Transform part in arch.GetComponentsInChildren<Transform>())if(part.name=="wall_doorway_door")part.gameObject.SetActive(false);
            Place(content.Find("floor_wood_large"),root,new Vector3(x*4.5f,7.1f,17.4f),new Vector3(4.4f,.35f,3.2f),Quaternion.identity);
            Place(content.Find("barrier"),root,new Vector3(x*4.5f,7.6f,15.9f),new Vector3(4.3f,1,.3f),Quaternion.identity);
            Place(content.Find("banner_red"),root,new Vector3(x*4.5f,8.5f,18.1f),new Vector3(1,2.6f,.25f),Quaternion.identity);
        }
        foreach(int side in new[]{-1,1})
        {
            Place(content.Find("stairs_wide"),root,new Vector3(side*12,-.1f,12),new Vector3(3,1.8f,3.5f),Quaternion.identity);
            for(int z=-2;z<=2;z++)
            {
                Place(content.Find("column"),root,new Vector3(side*14,2,-10+z*4.5f),new Vector3(.8f,4,.8f),Quaternion.identity);
                Place(content.Find("barrier"),root,new Vector3(side*14,1,-8+z*4.5f),new Vector3(4,1,.3f),Quaternion.Euler(0,90,0));
            }
        }
        // A surrounding hall behind the stands, built from the imported stone kit.
        for(int x=-4;x<=4;x++)
        {
            Place(content.Find("wall"),root,new Vector3(x*4,5,20),new Vector3(4,10,1),Quaternion.identity);
            Place(content.Find("floor_tile_large"),root,new Vector3(x*4,-.16f,15),new Vector3(4,.15f,12),Quaternion.identity);
            if(x%2==0)
                Place(content.Find("wall_gated"),root,new Vector3(x*4,11,20),new Vector3(3.8f,3,1),Quaternion.identity);
        }
        for(int side=-1;side<=1;side+=2)
        for(int row=0;row<5;row++)
        {
            Vector3 point=new Vector3(side*18,4.5f,-8+row*6);
            Place(content.Find("wall"),root,point,new Vector3(6,9,1),Quaternion.Euler(0,90,0));
            Place(content.Find("column"),root,point+Vector3.left*side*1.1f,new Vector3(1.4f,9,1.4f),Quaternion.identity);
        }
        for(int x=-1;x<=1;x++)
        {
            Place(content.Find("column"),root,new Vector3(x*12,5,16),new Vector3(1.5f,10,1.5f),Quaternion.identity);
            Place(content.Find("banner_red"),root,new Vector3(x*12+2.6f,6.2f,19.2f),new Vector3(1.5f,4,.3f),Quaternion.identity);
        }
        foreach(var point in new[]{new Vector3(-7,2.2f,8),new Vector3(7,2.2f,8),new Vector3(-13,3,13),new Vector3(13,3,13)})
        {
            var torch=Place(content.Find("torch_mounted"),root,point,new Vector3(.6f,1.8f,.6f),Quaternion.identity);
            var flame=new GameObject("Animated torch flame");flame.transform.SetParent(torch.transform,true);flame.transform.position=point+Vector3.up*.8f;
            var particles=flame.AddComponent<ParticleSystem>();var main=particles.main;
            main.startLifetime=.65f;main.startSpeed=.6f;main.startSize=new ParticleSystem.MinMaxCurve(.12f,.35f);
            main.startColor=new Color(1,.45f,.08f,.75f);main.maxParticles=24;
            var emission=particles.emission;emission.rateOverTime=20;
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=10;shape.radius=.08f;shape.rotation=new Vector3(-90,0,0);
            var fade=particles.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(1,.2f,.02f),1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(0,1)});fade.color=gradient;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=content.flameMaterial;
            var light=flame.AddComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=2;light.color=new Color(1,.5f,.16f);light.shadows=LightShadows.None;
        }
        if(Camera.main!=null){Camera.main.clearFlags=CameraClearFlags.SolidColor;Camera.main.backgroundColor=new Color(.12f,.085f,.065f);}
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.48f,.38f,.28f);
        RenderSettings.ambientEquatorColor=new Color(.38f,.34f,.31f);
        RenderSettings.ambientGroundColor=new Color(.22f,.17f,.14f);
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.16f,.12f,.085f);RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogStartDistance=32;RenderSettings.fogEndDistance=65;
        Light sun=null;foreach(var candidate in Object.FindObjectsByType<Light>())if(candidate.type==LightType.Directional){sun=candidate;break;}
        if(sun!=null){sun.intensity=1.5f;sun.color=new Color(1,.89f,.75f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-28,0);}
        StaticBatchingUtility.Combine(root.gameObject);
        return root;
    }
    public static GameObject Place(GameObject prefab,Transform parent,Vector3 center,Vector3 size,Quaternion rotation)
    {
        var wrapper=new GameObject(prefab!=null?prefab.name:"Missing decoration");wrapper.transform.SetParent(parent,false);
        wrapper.transform.SetPositionAndRotation(center,rotation);
        if(prefab==null)return wrapper;
        var obj=Object.Instantiate(prefab,wrapper.transform);obj.transform.localPosition=Vector3.zero;
        obj.transform.localRotation=Quaternion.identity;obj.transform.localScale=Vector3.one;
        var renderers=obj.GetComponentsInChildren<Renderer>();
        Bounds b=new Bounds();bool found=false;
        // Measure unrotated local geometry, not a yaw-expanded world AABB.
        wrapper.transform.rotation=Quaternion.identity;
        foreach(var renderer in renderers){if(!found){b=renderer.bounds;found=true;}else b.Encapsulate(renderer.bounds);}
        if(found)
        {
            obj.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,b.size.x),size.y/Mathf.Max(.001f,b.size.y),size.z/Mathf.Max(.001f,b.size.z));
            obj.transform.localPosition=Vector3.Scale(center-b.center,obj.transform.localScale);
        }
        wrapper.transform.rotation=rotation;
        foreach(var collider in obj.GetComponentsInChildren<Collider>())Object.Destroy(collider);
        return wrapper;
    }
}
