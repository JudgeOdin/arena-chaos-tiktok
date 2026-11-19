using UnityEngine;

public static class ArenaPortraitCameraFit
{
    public static void Fit(Camera camera,float radius)
    {
        if(Screen.width>=Screen.height)return;
        camera.rect=new Rect(0,.19f,1,.54f);
        float vertical=Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
        float horizontal=vertical*((float)Screen.width/Screen.height/.54f);
        float shift=0;
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)for(int y=0;y<=3;y+=3)
        {
            Vector3 point=camera.transform.InverseTransformPoint(new Vector3(x*(radius+1),y,z*(radius+1)));
            shift=Mathf.Max(shift,Mathf.Abs(point.x)/horizontal-point.z,Mathf.Abs(point.y)/vertical-point.z);
        }
        camera.transform.position-=camera.transform.forward*(shift+1);
    }
}
