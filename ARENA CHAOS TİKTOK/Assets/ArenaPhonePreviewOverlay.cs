#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

public static class ArenaPhonePreviewOverlay
{
    public static GameObject Create()
    {
        var root=new GameObject("Illustrative phone live overlay",typeof(Canvas),typeof(CanvasScaler));
        var canvas=root.GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder=1000;
        var scaler=root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(540,960);

        var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var dark=new Color(.02f,.03f,.06f,.86f);
        var soft=new Color(.04f,.06f,.09f,.72f);
        var gold=new Color(1f,.78f,.40f);
        Panel(root.transform,new Vector2(.02f,.991f),new Vector2(.98f,1),dark);
        TextAt(root.transform,font,"ÖRNEK CANLI YAYIN  •  ARENA CHAOS",
            new Vector2(.045f,.004f),new Vector2(.51f,.027f),11,Color.white,dark);

        TextAt(root.transform,font,"    İzleyiciden Gül geldi   +1",
            new Vector2(.045f,.163f),new Vector2(.49f,.197f),14,gold,dark);
        TextAt(root.transform,font,"    Sena  •  !mavi",
            new Vector2(.045f,.124f),new Vector2(.65f,.158f),14,Color.white,soft);
        TextAt(root.transform,font,"    Deniz  •  Kırmızı kralı koruyun!",
            new Vector2(.045f,.088f),new Vector2(.65f,.122f),14,Color.white,soft);
        TextAt(root.transform,font,"    Sohbete yaz…",
            new Vector2(.045f,.039f),new Vector2(.74f,.079f),14,new Color(.78f,.85f,.91f),dark);

        TextAt(root.transform,font,"TAKİP",
            new Vector2(.83f,.33f),new Vector2(.966f,.371f),11,Color.white,soft,TextAnchor.MiddleCenter);
        TextAt(root.transform,font,"BEĞEN",
            new Vector2(.83f,.283f),new Vector2(.966f,.324f),11,Color.white,soft,TextAnchor.MiddleCenter);
        TextAt(root.transform,font,"HEDİYE",
            new Vector2(.83f,.236f),new Vector2(.966f,.277f),11,gold,soft,TextAnchor.MiddleCenter);
        return root;
    }

    static RectTransform Panel(Transform parent,Vector2 min,Vector2 max,Color shade)
    {
        var item=new GameObject("Viewer overlay panel",typeof(RectTransform),typeof(Image));
        item.transform.SetParent(parent,false);
        var rect=(RectTransform)item.transform;
        rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var image=item.GetComponent<Image>();image.color=shade;image.raycastTarget=false;
        return rect;
    }

    static void TextAt(Transform parent,Font font,string message,Vector2 min,Vector2 max,
        int size,Color ink,Color shade,TextAnchor align=TextAnchor.MiddleLeft)
    {
        var rect=Panel(parent,min,max,shade);
        var label=new GameObject("Caption",typeof(RectTransform),typeof(Text));
        label.transform.SetParent(rect,false);
        var t=(RectTransform)label.transform;
        t.anchorMin=Vector2.zero;t.anchorMax=Vector2.one;t.offsetMin=new Vector2(5,0);t.offsetMax=new Vector2(-5,0);
        var text=label.GetComponent<Text>();
        text.font=font;text.fontSize=size;text.text=message;text.color=ink;text.alignment=align;text.raycastTarget=false;
        text.fontStyle=FontStyle.Bold;
    }
}
#endif

