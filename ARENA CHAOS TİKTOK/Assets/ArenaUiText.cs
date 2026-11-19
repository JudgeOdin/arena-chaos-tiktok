public static class ArenaUiText
{
    public static string Safe(string text)=>string.IsNullOrEmpty(text)?"":text.Replace("<","‹").Replace(">","›");
    public static string Short(string text,int length=22)
    {text=Safe(text);return text.Length<=length?text:text.Substring(0,length-1)+"…";}
}
