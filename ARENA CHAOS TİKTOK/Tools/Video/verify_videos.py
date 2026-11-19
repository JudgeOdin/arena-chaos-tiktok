import json,re,subprocess
from pathlib import Path
import imageio_ffmpeg
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Videos'/'TikTok_Ilk_Seri';WORK=OUT/'Kaynaklar'
ff=imageio_ffmpeg.get_ffmpeg_exe();results=[]
for file in sorted(OUT.glob('0[123]_*.mp4')):
 reader=imageio_ffmpeg.read_frames(str(file));meta=next(reader);reader.close()
 if tuple(meta['size'])!=(1080,1920) or abs(meta['fps']-30)>.01:raise RuntimeError('Wrong video format: '+file.name)
 if not 14<=meta['duration']<=20:raise RuntimeError('Unexpected length')
 decode=subprocess.run([ff,'-v','error','-i',str(file),'-f','null','-'],capture_output=True)
 if decode.returncode or decode.stderr.strip():raise RuntimeError(decode.stderr.decode('utf-8','replace'))
 volume=subprocess.run([ff,'-hide_banner','-i',str(file),'-vn','-af','volumedetect','-f','null','-'],capture_output=True)
 log=volume.stderr.decode('utf-8','replace');(WORK/(file.stem+'_audio_check.log')).write_text(log,encoding='utf-8')
 peak=float(re.search(r'max_volume:\s*(-?[\d.]+)',log)[1]);mean=float(re.search(r'mean_volume:\s*(-?[\d.]+)',log)[1])
 samples=max(map(int,re.findall(r'n_samples:\s*(\d+)',log)));audio_seconds=samples/(48000*2)
 if peak>-.1 or peak<-10 or not -28<mean<-10:raise RuntimeError('Audio level outside delivery range: '+file.name+' '+str(peak))
 if audio_seconds<meta['duration']*.98:raise RuntimeError('Audio stream is shorter than video: '+file.name)
 if meta.get('audio_codec')!='aac':raise RuntimeError('AAC audio missing')
 results.append({'file':file.name,'status':'PASS','width':1080,'height':1920,'fps':meta['fps'],'seconds':meta['duration'],'codec':meta['codec'],'audioPeakDb':peak,'audioMeanDb':mean,'audioSeconds':audio_seconds,'bytes':file.stat().st_size})
 print(json.dumps(results[-1],ensure_ascii=False),flush=True)
 for t in [4.9,10.1,meta['duration']-2]:
  destination=WORK/(file.stem+f'_check_{t:.1f}.jpg')
  subprocess.run([ff,'-hide_banner','-loglevel','error','-y','-ss',str(t),'-i',str(file),'-frames:v','1',str(destination)],check=True)
(OUT/'Video_Kontrol_Raporu.json').write_text(json.dumps({'status':'PASS','videos':results},ensure_ascii=False,indent=2),encoding='utf-8')
