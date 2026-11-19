import json,subprocess,wave
from pathlib import Path
import numpy as np
import imageio_ffmpeg
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Videos'/'TikTok_Ilk_Seri';WORK=OUT/'Kaynaklar'
FF=imageio_ffmpeg.get_ffmpeg_exe();RATE=48000
def decode(path,normalize=False,seconds=None):
 args=[FF,'-hide_banner','-loglevel','error','-i',str(path)]
 if normalize:args+=['-af','loudnorm=I=-16:TP=-2:LRA=9']
 if seconds is not None:args+=['-t',str(seconds)]
 args+=['-ar',str(RATE),'-ac','2','-f','f32le','pipe:1']
 result=subprocess.run(args,capture_output=True,check=True)
 return np.frombuffer(result.stdout,dtype='<f4').reshape(-1,2).copy()
def map_time(key,t):
 if key=='meteor':return t if t<=2.7 else 2.7+(t-2.7)/.62 if t<=4.9 else 2.7+2.2/.62+t-4.9
 return t*(1.08 if key=='fire' else 1.1)
def attach_audio(key,file,duration):
 meta=json.loads((WORK/(key+'_capture.json')).read_text('utf-8-sig'))
 frames=round(duration*RATE);master=np.zeros((frames,2),dtype=np.float32)
 def add(data,start=0,gain=1):
  offset=round(start*RATE)
  if offset>=frames:return
  count=min(len(data),frames-offset);master[offset:offset+count]+=data[:count]*gain
 voice=decode(WORK/(key+'_voice.mp3'),True)
 words=json.loads((WORK/(key+'_words.json')).read_text('utf-8'))
 if len(voice)/RATE < words[-1]['start']+words[-1]['duration']-.1:raise RuntimeError('Narration incomplete')
 if len(voice)/RATE+.28>duration:raise RuntimeError('Narration longer than video')
 add(voice,.28)
 music=decode(ROOT/meta['music'],seconds=duration)
 if len(music)<frames:music=np.tile(music,(int(np.ceil(frames/len(music))),1))
 add(music,gain=.095)
 if key=='meteor':
  blast=decode(ROOT/'Assets/Resources/ArenaChaos/SpellAudio/Meteor.wav')
  add(blast,map_time(key,4),.48);add(blast,2.7+2.2/.62+2.3+1.2/.8,.42)
 if key=='fire':add(decode(ROOT/'Assets/Resources/ArenaChaos/SpellAudio/Fire.wav'),.54,.36)
 punch=decode(ROOT/meta['punch'],seconds=.28)
 fade=min(len(punch),round(RATE*.07));punch[-fade:]*=np.linspace(1,0,fade,dtype=np.float32)[:,None]
 last=-100;count=0
 for cue in meta['cues']:
  if cue['kind'] not in ('punch','arrow'):continue
  when=map_time(key,cue['time'])
  if key=='meteor' and when>map_time(key,4):continue
  if when-last<.27 or count>=18:continue
  add(punch,when,.25);last=when;count+=1
 peak=float(np.max(np.abs(master)))
 if peak>.9:master*=.9/peak
 fade=round(RATE*.45);master[-fade:]*=np.linspace(1,0,fade,dtype=np.float32)[:,None]
 wav=WORK/(key+'_mix.wav')
 with wave.open(str(wav),'wb') as output:
  output.setnchannels(2);output.setsampwidth(2);output.setframerate(RATE);output.writeframes((np.clip(master,-1,1)*32767).astype('<i2').tobytes())
 audio=WORK/(key+'_master.m4a')
 command=[FF,'-hide_banner','-loglevel','error','-y','-i',str(wav),'-af','loudnorm=I=-16:TP=-1.5:LRA=9','-ar',str(RATE),'-c:a','aac','-b:a','192k','-t',str(duration),str(audio)]
 subprocess.run(command,check=True)
 rendered=decode(audio)
 if len(rendered)<frames*.98:raise RuntimeError('Master audio truncated')
 temporary=WORK/(key+'_with_audio.mp4')
 subprocess.run([FF,'-hide_banner','-loglevel','error','-y','-i',str(file),'-i',str(audio),'-map','0:v:0','-map','1:a:0','-c','copy','-t',str(duration),'-movflags','+faststart',str(temporary)],check=True)
 temporary.replace(file)
 (WORK/(key+'_audio_mix.json')).write_text(json.dumps({'voiceSeconds':len(voice)/RATE,'masterSeconds':len(rendered)/RATE,'videoSeconds':duration,'status':'PASS'},indent=2),encoding='utf-8')
 print('FULL AUDIO',key,len(rendered)/RATE,flush=True)
if __name__=='__main__':
 for key,file,d in [('meteor','01_Meteor.mp4',17.4),('fire','02_Alevli_Yumruk.mp4',15.5),('classes','03_Okcu_Mu_Muhafiz_Mi.mp4',15.8)]:attach_audio(key,OUT/file,d)
