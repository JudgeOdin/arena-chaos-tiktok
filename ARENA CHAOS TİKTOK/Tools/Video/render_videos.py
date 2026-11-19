import html,json,re,subprocess,sys,textwrap
from pathlib import Path
import imageio_ffmpeg
from mix_audio import attach_audio

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Videos'/'TikTok_Ilk_Seri';WORK=OUT/'Kaynaklar'
FF=imageio_ffmpeg.get_ffmpeg_exe()
SPECS={
 'meteor':{'file':'01_Meteor.mp4','title':'TEK METEOR.\nBÜTÜN ARENA.','kicker':'ARENA GÜÇLERİ / 01','accent':'F0BC62','duration':17.4,'cta':'TAKİP ET / YAYINI KAÇIRMA'},
 'fire':{'file':'02_Alevli_Yumruk.mp4','title':'BU YUMRUKLAR\nALEV ALIYOR!','kicker':'ARENA GÜÇLERİ / 02','accent':'FF714D','duration':15.5,'cta':'SENCE KİM DURDURABİLİR?'},
 'classes':{'file':'03_Okcu_Mu_Muhafiz_Mi.mp4','title':'OKÇU MU?\nMUHAFIZ MI?','kicker':'KARAKTERİNİ SEÇ / 03','accent':'73D7D3','duration':15.8,'cta':'TERCİHİNİ YORUMA YAZ'}
}
def run(args,log):
 p=subprocess.run([FF,'-hide_banner','-y',*map(str,args)],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 Path(log).write_bytes(p.stderr)
 if p.returncode:raise RuntimeError(p.stderr.decode('utf-8','replace')[-4500:])
def color(value):return '&H00'+value[4:6]+value[2:4]+value[:2]
def clock(t):
 n=round(t*100);return f'{n//360000}:{n//6000%60:02}:{n//100%60:02}.{n%100:02}'
def ass_escape(t):return t.replace('\\','\\\\').replace('{','').replace('}','').replace('\n',r'\N')
def make_ass(key,spec):
 d=spec['duration'];accent=color(spec['accent'])
 styles=[
  f'Style: Brand,Arial,34,&H00FFFFFF,&H00FFFFFF,&H00000000,&H00000000,-1,0,0,0,100,100,4,0,1,0,0,8,60,60,78,1',
  f'Style: Kicker,Arial,22,{accent},{accent},&H00000000,&H00000000,-1,0,0,0,100,100,2,0,1,0,0,8,70,70,141,1',
  'Style: Title,Arial,78,&H00FFFFFF,&H00FFFFFF,&H00000000,&H00000000,-1,0,0,0,100,100,0,0,1,0,0,8,65,65,194,1',
  'Style: Caption,Arial,52,&H00FFFFFF,&H00FFFFFF,&H00110D08,&H50000000,-1,0,0,0,100,100,0,0,1,2,0,2,90,150,285,1',
  f'Style: Cta,Arial,32,{accent},{accent},&H00000000,&H00000000,-1,0,0,0,100,100,1,0,1,0,0,8,100,150,1738,1',
  'Style: Fine,Arial,20,&H00C2B5A5,&H00FFFFFF,&H00000000,&H00000000,0,0,0,0,100,100,1,0,1,0,0,8,70,150,1805,1',
  'Style: Demo,Arial,18,&H00FFFFFF,&H00FFFFFF,&H00000000,&H50000000,0,0,0,0,100,100,1,0,1,1,0,9,40,45,1410,1',
  f'Style: Replay,Arial,24,{accent},{accent},&H00000000,&H50000000,-1,0,0,0,100,100,2,0,1,2,0,7,50,50,414,1',
 ]
 head='[Script Info]\nScriptType: v4.00+\nPlayResX: 1080\nPlayResY: 1920\nWrapStyle: 0\nScaledBorderAndShadow: yes\n\n[V4+ Styles]\nFormat: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding\n'
 lines=[]
 def event(style,start,end,text,tags=''):
  lines.append(f'Dialogue: 0,{clock(start)},{clock(end)},{style},,0,0,0,,{tags}{text}')
 event('Brand',0,d,'ARENA '+r'{\c'+accent+'}CHAOS')
 event('Kicker',0,d,ass_escape(spec['kicker']))
 event('Title',0,d,ass_escape(spec['title']),r'{\fad(120,220)}')
 event('Demo',0,d,'OYUN İÇİ DEMO')
 event('Cta',d-4.2,d,ass_escape(spec['cta']),r'{\fad(180,200)}')
 event('Fine',0,d,'YAKINDA CANLI YAYINDA')
 if key=='meteor':event('Replay',8.55,11.05,'TEKRAR',r'{\fad(70,70)}')
 words=json.loads((WORK/(key+'_words.json')).read_text('utf-8'))
 script_tokens=json.loads((WORK/'scripts.json').read_text('utf-8'))[key].split()
 if len(script_tokens)==len(words):
  for word,token in zip(words,script_tokens):word['text']=token
 groups=[];group=[]
 for word in words:
  if group and (len(' '.join(w['text'] for w in group))+len(word['text'])>30 or len(group)>=4):groups.append(group);group=[]
  group.append(word)
  if len(group)>=2 and re.search(r'[.!?,]$',word['text']):groups.append(group);group=[]
 if group:groups.append(group)
 srt=[]
 for i,group in enumerate(groups):
  start=group[0]['start']+.28;end=min(d-.3,group[-1]['start']+group[-1]['duration']+.45)
  if i+1<len(groups):end=min(end,groups[i+1][0]['start']+.27)
  value=html.unescape(' '.join(w['text'] for w in group)).replace('i','İ').replace('ı','I').upper()
  event('Caption',start,end,ass_escape(value),r'{\fad(50,50)}')
  def srt_time(t):
   n=round(t*1000);return f'{n//3600000:02}:{n//60000%60:02}:{n//1000%60:02},{n%1000:03}'
  srt.append(f'{i+1}\n{srt_time(start)} --> {srt_time(end)}\n{value}\n')
 file=WORK/(key+'.ass');file.write_text(head+'\n'.join(styles)+'\n\n[Events]\nFormat: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text\n'+'\n'.join(lines),encoding='utf-8-sig')
 (OUT/(spec['file'].replace('.mp4','.srt'))).write_text('\n'.join(srt),encoding='utf-8-sig')
 return file
def render(key):
 spec=SPECS[key];d=spec['duration'];ass=make_ass(key,spec)
 inputs=['-i',WORK/(key+'_upright.mp4')]
 graph=[]
 if key=='meteor':
  graph+=['[0:v]split=5[m0][m1][m2][m3][m4]',
   '[m0]trim=0:2.7,setpts=PTS-STARTPTS[ma]',
   '[m1]trim=2.7:4.9,setpts=(PTS-STARTPTS)/0.62[mb]',
   '[m2]trim=4.9:7.2,setpts=PTS-STARTPTS[mc]',
   '[m3]trim=2.8:4.8,setpts=(PTS-STARTPTS)/0.8[md]',
   '[m4]trim=4.8:8,setpts=PTS-STARTPTS[me]',
   '[ma][mb][mc][md][me]concat=n=5:v=1:a=0[edited]']
 else:graph.append(f'[0:v]setpts={1.08 if key=="fire" else 1.1}*(PTS-STARTPTS)[edited]')
 graph.append(f'[edited]fps=30,scale=1080:1080:flags=lanczos,tpad=stop_mode=clone:stop_duration=5,trim=duration={d}[game]')
 graph.append(f'color=c=0x09131C:s=1080x1920:r=30:d={d},drawbox=x=0:y=0:w=1080:h=365:color=0x101F29:t=fill,drawbox=x=74:y=126:w=932:h=2:color=0x{spec["accent"]}:t=fill[base]')
 graph.append('[base][game]overlay=0:365:shortest=1[placed]')
 relative=ass.relative_to(ROOT).as_posix()
 graph.append(f"[placed]drawbox=x=0:y=363:w=1080:h=3:color=0x{spec['accent']}:t=fill,drawbox=x=0:y=1445:w=1080:h=3:color=0x{spec['accent']}:t=fill,subtitles=filename='{relative}',fade=t=out:st={d-.35}:d=0.35,format=yuv420p[v]")
 (WORK/(key+'_filter.txt')).write_text(';\n'.join(graph),encoding='utf-8')
 print('RENDER',key,flush=True)
 run([*inputs,'-filter_complex',';'.join(graph),'-map','[v]','-an','-t',d,'-c:v','libx264','-preset','medium','-crf','18','-maxrate','12M','-bufsize','24M','-profile:v','high','-level:v','4.2','-r','30','-movflags','+faststart',OUT/spec['file']],WORK/(key+'_render.log'))
 attach_audio(key,OUT/spec['file'],d)
 run(['-ss','1.2','-i',OUT/spec['file'],'-frames:v','1',OUT/spec['file'].replace('.mp4','_Kapak.jpg')],WORK/(key+'_cover.log'))
 print('READY',spec['file'],(OUT/spec['file']).stat().st_size,flush=True)
if __name__=='__main__':
 for name in sys.argv[1:] or SPECS.keys():render(name)
