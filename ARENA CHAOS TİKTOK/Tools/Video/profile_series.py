"""Produce the @arenachaostr vertical series from staged Unity gameplay.

Run with capture, voices, render, verify, or all. Sources and intermediate files
stay in the ignored Kaynaklar directory; finished MP4, SRT and covers are kept.
"""
import asyncio
import html
import json
import re
import subprocess
import sys
import time
import wave
import zipfile
from pathlib import Path

import edge_tts
import imageio_ffmpeg
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Videos/TikTok_Profil_Serisi'
WORK = OUT / 'Kaynaklar'
FF = imageio_ffmpeg.get_ffmpeg_exe()
RATE = 48000
SPECS = {
    'infection': dict(file='01_Dostun_Zombiye_Donustu', accent='A2F06C',
        hook='AZ ÖNCE\nTAKIMINDAYDI.', question='KAÇAR MISIN, SAVAŞIR MISIN?',
        voice='Az önce takımındaydı. Şimdi seni kovalıyor! Bu arenada elenince zombiye dönüşüyorsun. Eski dostların artık senden kaçıyor. Peki sen olsan, kaçmayı mı seçerdin, savaşmayı mı? Arena Chaos için takipte kal.',
        beats=[(0, 3.0, 'DOSTUNDU.'), (3.0, 7, 'ŞİMDİ ZOMBİ.'),
               (7, 11.5, 'AYNI ARENA. YENİ DÜŞMAN.'), (11.5, 16.8, 'SEN NE YAPARDIN?')],
        # A quick preview, then the actual transformation, then its aftermath.
        cuts=[(2.0, 3.2, 1.0), (0, 2.2, .65), (2.2, 5.7, .72), (5.7, 10, .65)]),
    'king': dict(file='02_Bir_Kral_Butun_Takim', accent='FFD177',
        hook='KRALIN DÜŞER.\nTAKIMIN KAYBEDER.', question='SAVUNMA MI, HÜCUM MU?',
        voice='Tek bir kral düşüyor. Bütün takım kaybediyor! Burada en çok vuran olmak yetmiyor. Kralın ayakta kalmalı. Sen savunmada mı dururdun, rakip krala mı dalardın? Seçimini yorumlara yaz. Arena Chaos için takipte kal.',
        beats=[(0, 3, 'TEK BİR DÜŞÜŞ.'), (3, 7, 'TAKIMIN KADERİ ONDA.'),
               (7, 11.5, 'SAVUNMA MI? HÜCUM MU?'), (11.5, 17.8, 'TARAFINI SEÇ.')],
        cuts=[(3.2, 4.1, .8), (0, 3.2, .6), (2.6, 4.4, .6), (0, 3.6, .65), (3.6, 7.5, 1.0)]),
    'giant': dict(file='03_Herkes_Tek_Bir_Deve_Karsi', accent='FF785B',
        hook='HEPİNİZ\nBİR DEVE KARŞI.', question='TAKIM MI KAZANIR, DEV Mİ?',
        voice='Bu kez birbirinize vurmayın. Hepiniz tek bir deve karşısınız! Etrafını sarın, saldırı alanından çıkın, birlikte düşürün. Ama takım dağılırsa dev işi bitirir. Sence kim kazanır? Takım mı, dev mi? Arena Chaos için takipte kal.',
        beats=[(0, 3, 'BU KEZ AYNI TAKIMDASINIZ.'), (3, 7, 'HEDEF: ARENA DEVİ.'),
               (7, 11.5, 'SALDIRI ALANINA DİKKAT!'), (11.5, 18.8, 'TAKIM MI? DEV Mİ?')],
        cuts=[(0, 3, 1), (3, 6.5, .8), (6.5, 10, .9), (2, 9, .85)]),
}


def run(args, log=None):
    p = subprocess.run([FF, '-hide_banner', '-y', *map(str, args)], capture_output=True, cwd=ROOT)
    if log:
        Path(log).write_bytes(p.stderr)
    if p.returncode:
        raise RuntimeError(p.stderr.decode('utf-8', 'replace')[-4000:])
    return p


def capture():
    WORK.mkdir(parents=True, exist_ok=True)
    request = ROOT / 'Tools/Video/CaptureRequest.json'
    for key in SPECS:
        report = WORK / (key + '_capture.json')
        if report.exists() and json.loads(report.read_text('utf-8-sig')).get('status') == 'PASS':
            print('CAPTURE EXISTS', key, flush=True)
            continue
        if request.exists():
            raise RuntimeError('Another capture request is pending')
        config = dict(scenario=key, folder=str(WORK), ffmpeg=FF, width=540,
                      height=960, fps=30, seconds=10)
        start = time.time()
        temp = request.with_suffix('.tmp')
        temp.write_text(json.dumps(config), encoding='utf-8')
        temp.replace(request)
        print('CAPTURING', key, flush=True)
        last = 0
        while True:
            if report.exists() and report.stat().st_mtime >= start:
                data = json.loads(report.read_text('utf-8-sig'))
                if data.get('status') != 'PASS' or data['frames'] != 300:
                    raise RuntimeError(json.dumps(data))
                print('CAPTURED', key, data['frames'], flush=True)
                break
            if time.time() - start > 300:
                raise RuntimeError('Capture timeout: ' + key)
            if time.time() - last > 20:
                progress = WORK / 'progress.txt'
                print(progress.read_text('utf-8-sig') if progress.exists() else 'Waiting for editor', flush=True)
                last = time.time()
            time.sleep(1)
        time.sleep(4)


async def voices():
    WORK.mkdir(parents=True, exist_ok=True)
    for key, spec in SPECS.items():
        path = WORK / (key + '_voice.mp3')
        if path.exists() and (WORK / (key + '_words.json')).exists():
            continue
        words = []
        tts = edge_tts.Communicate(spec['voice'], 'tr-TR-AhmetNeural', rate='+12%', boundary='WordBoundary')
        with path.open('wb') as output:
            async for chunk in tts.stream():
                if chunk['type'] == 'audio':
                    output.write(chunk['data'])
                elif chunk['type'] == 'WordBoundary':
                    words.append(dict(start=chunk['offset']/1e7, duration=chunk['duration']/1e7, text=chunk['text']))
        (WORK / (key + '_words.json')).write_text(json.dumps(words, ensure_ascii=False), 'utf-8')
        print('VOICE', key, words[-1]['start'] + words[-1]['duration'], flush=True)


def stamp(t, srt=False):
    n = round(t * (1000 if srt else 100))
    if srt:
        return f'{n//3600000:02}:{n//60000%60:02}:{n//1000%60:02},{n%1000:03}'
    return f'{n//360000}:{n//6000%60:02}:{n//100%60:02}.{n%100:02}'


def upper(t):
    return t.replace('i', 'İ').replace('ı', 'I').upper()


def ass(key, spec, duration):
    rgb = spec['accent']
    accent = '&H00' + rgb[4:6] + rgb[2:4] + rgb[:2]
    header = '''[Script Info]
ScriptType: v4.00+
PlayResX: 1080
PlayResY: 1920
WrapStyle: 0
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
'''
    styles = [
        f'Style: Brand,Arial,31,{accent},&H00FFFFFF,&H00000000,&H00000000,-1,0,0,0,100,100,2,0,1,1,0,7,64,160,110,1',
        'Style: Hook,Arial,82,&H00FFFFFF,&H00FFFFFF,&H00110E0B,&H00000000,-1,0,0,0,100,100,0,0,1,3,1,7,64,145,174,1',
        f'Style: Beat,Arial,48,{accent},&H00FFFFFF,&H00110E0B,&H95000000,-1,0,0,0,100,100,0,0,1,3,1,2,64,155,525,1',
        'Style: Caption,Arial,57,&H00FFFFFF,&H00FFFFFF,&H00110E0B,&H95000000,-1,0,0,0,100,100,0,0,3,2,0,2,80,165,370,1',
        f'Style: CTA,Arial,45,{accent},&H00FFFFFF,&H00110E0B,&H00000000,-1,0,0,0,100,100,0,0,1,2,0,2,64,155,244,1',
        'Style: Handle,Arial,34,&H00FFFFFF,&H00FFFFFF,&H00110E0B,&H00000000,-1,0,0,0,100,100,1,0,1,1,0,2,64,155,180,1',
        'Style: Demo,Arial,22,&H00D7D7D7,&H00FFFFFF,&H00110E0B,&H00000000,0,0,0,0,100,100,1,0,1,1,0,7,64,155,420,1',
        f'Style: Replay,Arial,22,{accent},&H00FFFFFF,&H00110E0B,&H00000000,-1,0,0,0,100,100,0,0,1,1,0,9,64,155,420,1',
    ]
    events = []
    def event(style, start, end, text, tags=''):
        text = text.replace('{', '').replace('}', '').replace('\n', r'\N')
        events.append(f'Dialogue: 0,{stamp(start)},{stamp(end)},{style},,0,0,0,,{tags}{text}')
    event('Brand', 0, duration, 'ARENA CHAOS  /  @arenachaostr')
    event('Hook', 0, 3.1, spec['hook'], r'{\fad(70,120)}')
    event('Hook', 3.1, duration, {'infection':'ZOMBİ İSTİLASI','king':'KRALI KORU','giant':'DEV AVI'}[key])
    event('Demo', 0, duration, 'OYUN İÇİ DEMO')
    if key == 'king':
        event('Replay', 1.13, 15, 'YAVAŞ ÇEKİM / TEKRAR')
    for start, end, text in spec['beats']:
        event('Beat', start, min(end, duration - 3.4), text, r'{\fad(60,80)}') if start < duration - 3.4 else None
    event('CTA', duration-3.4, duration, spec['question'], r'{\fad(120,0)}')
    event('Handle', 0, duration, '@arenachaostr  •  Devamı için takip et')
    words = json.loads((WORK / (key + '_words.json')).read_text('utf-8'))
    tokens = spec['voice'].split()
    if len(tokens) == len(words):
        for w, token in zip(words, tokens):
            w['text'] = token
    groups = []
    group = []
    for word in words:
        if group and (len(' '.join(w['text'] for w in group)) + len(word['text']) > 24 or len(group) >= 3):
            groups.append(group)
            group = []
        group.append(word)
        if re.search(r'[.!?,]$', word['text']):
            groups.append(group)
            group = []
    if group:
        groups.append(group)
    srt = []
    for i, group in enumerate(groups):
        start = group[0]['start'] + .12
        end = min(duration-.15, group[-1]['start']+group[-1]['duration']+.3)
        if i+1 < len(groups):
            end = min(end, groups[i+1][0]['start']+.11)
        text = upper(html.unescape(' '.join(w['text'] for w in group)))
        event('Caption', start, end, text, r'{\fad(35,35)}')
        srt.append(f'{i+1}\n{stamp(start,True)} --> {stamp(end,True)}\n{text}\n')
    (OUT / (spec['file'] + '.srt')).write_text('\n'.join(srt), 'utf-8-sig')
    path = WORK / (key + '.ass')
    path.write_text(header+'\n'.join(styles)+'\n\n[Events]\nFormat: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text\n'+'\n'.join(events), 'utf-8-sig')
    return path


def decode(path, normalize=False, seconds=None):
    args = ['-v','error','-i',str(path)]
    if normalize:
        args += ['-af','loudnorm=I=-17:TP=-2:LRA=9']
    if seconds:
        args += ['-t', str(seconds)]
    args += ['-ar',str(RATE),'-ac','2','-f','f32le','pipe:1']
    return np.frombuffer(run(args).stdout, dtype='<f4').reshape(-1,2).copy()


def audio(key, spec, duration):
    n = round(duration*RATE)
    mix = np.zeros((n,2), dtype=np.float32)
    def add(sound, when=0, gain=1):
        offset = round(when*RATE)
        count = min(len(sound), n-offset)
        if count > 0:
            mix[offset:offset+count] += sound[:count]*gain
    voice = decode(WORK/(key+'_voice.mp3'), True)
    if len(voice)/RATE+.12 > duration-.2:
        raise RuntimeError('Narration is longer than video')
    add(voice, .12)
    music = decode(ROOT/'Assets/Audio/the_final_battle.ogg', seconds=duration)
    if len(music) < n:
        music = np.tile(music,(int(np.ceil(n/len(music))),1))
    add(music, gain=.07)
    meta = json.loads((WORK/(key+'_capture.json')).read_text('utf-8-sig'))
    punch = decode(ROOT/meta['punch'], seconds=.23)
    punch[-2400:] *= np.linspace(1,0,2400,dtype=np.float32)[:,None]
    slam=decode(ROOT/'Assets/Resources/ArenaChaos/SpellAudio/Meteor.wav') if key=='giant' else None
    previous = -1
    offset = 0
    for a,b,speed in spec['cuts']:
        for cue in meta['cues']:
            if cue['kind']=='boss-slam' and slam is not None and a<=cue['time']<b:
                add(slam,offset+(cue['time']-a)/speed,.28)
            if cue['kind'] not in ('punch','arrow') or not a <= cue['time'] < b:
                continue
            t = offset+(cue['time']-a)/speed
            if t-previous >= .4:
                add(punch,t,.17)
                previous=t
        offset += (b-a)/speed
    peak = np.max(np.abs(mix))
    if peak > .9:
        mix *= .9/peak
    fade = round(RATE*.22)
    mix[-fade:] *= np.linspace(1,0,fade,dtype=np.float32)[:,None]
    wav = WORK/(key+'_mix.wav')
    with wave.open(str(wav),'wb') as out:
        out.setnchannels(2)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes((np.clip(mix,-1,1)*32767).astype('<i2').tobytes())
    return wav, len(voice)/RATE


def render(keys=None):
    for key,spec in SPECS.items():
        if keys and key not in keys:
            continue
        voice_seconds=len(decode(WORK/(key+'_voice.mp3')))/RATE
        duration=max(16.8, round(voice_seconds+.8,1))
        subtitle=ass(key,spec,duration)
        wav,narration=audio(key,spec,duration)
        segments=spec['cuts']
        graph=[f'[0:v]split={len(segments)}'+''.join(f'[s{i}]' for i in range(len(segments)))]
        for i,(a,b,speed) in enumerate(segments):
            graph.append(f'[s{i}]trim=start={a}:end={b},setpts=(PTS-STARTPTS)/{speed},fps=30,setsar=1[c{i}]')
        graph.append(''.join(f'[c{i}]' for i in range(len(segments)))+f'concat=n={len(segments)}:v=1:a=0,tpad=stop_mode=clone:stop_duration=6,trim=duration={duration},scale=1080:1920:flags=lanczos,eq=contrast=1.035:saturation=1.09:brightness=.008,unsharp=5:5:.25:5:5:0[base]')
        relative=subtitle.relative_to(ROOT).as_posix()
        # Dark gradients give readable type without shrinking the action window.
        graph.append(f'color=c=black@0:s=1080x1920:r=30:d={duration},format=rgba,geq=r=0:g=0:b=0:a=\'if(lt(Y,480),180*(1-Y/480),if(gt(Y,1300),185*(Y-1300)/620,0))\'[shade]')
        graph.append(f"[base][shade]overlay=0:0:shortest=1,drawbox=x=64:y=153:w=76:h=5:color=0x{spec['accent']}:t=fill,subtitles=filename='{relative}',format=yuv420p[v]")
        (WORK/(key+'_filter.txt')).write_text(';\n'.join(graph),'utf-8')
        output=OUT/(spec['file']+'.mp4')
        print('RENDER',key,duration,flush=True)
        run(['-i',WORK/(key+'_raw.mp4'),'-i',wav,'-filter_complex',';'.join(graph),'-map','[v]','-map','1:a:0','-af','loudnorm=I=-16:TP=-1.5:LRA=9','-c:v','libx264','-preset','fast','-crf','19','-pix_fmt','yuv420p','-profile:v','high','-r','30','-c:a','aac','-ar','48000','-b:a','192k','-t',duration,'-movflags','+faststart',output], WORK/(key+'_render.log'))
        run(['-ss','0.6','-i',output,'-frames:v','1',OUT/(spec['file']+'_Kapak.jpg')])
        (WORK/(key+'_timing.json')).write_text(json.dumps(dict(duration=duration,narrationSeconds=narration)),'utf-8')
        print('READY',output.name,flush=True)


def verify(keys=None):
    checks=[]
    for key,spec in SPECS.items():
        if keys and key not in keys:
            continue
        file=OUT/(spec['file']+'.mp4')
        reader=imageio_ffmpeg.read_frames(str(file))
        meta=next(reader)
        reader.close()
        assert tuple(meta['size'])==(1080,1920) and abs(meta['fps']-30)<.01
        assert 14 <= meta['duration'] <= 24
        assert meta.get('audio_codec')=='aac'
        p=run(['-v','error','-progress','pipe:1','-i',file,'-f','null','-'])
        assert not p.stderr.strip(),p.stderr.decode('utf-8','replace')
        frames=max(map(int,re.findall(rb'frame=(\d+)',p.stdout)))
        assert abs(frames/30-meta['duration'])<.08,(frames,meta['duration'])
        audio=decode(file)
        seconds=len(audio)/RATE
        peak=20*np.log10(max(float(np.max(np.abs(audio))),1e-9))
        mean=20*np.log10(max(float(np.sqrt(np.mean(audio**2))),1e-9))
        assert seconds >= meta['duration']*.98
        assert -10 < peak < -.1 and -28 < mean < -10,(peak,mean)
        checks.append(dict(file=file.name,status='PASS',seconds=meta['duration'],width=1080,height=1920,fps=30,decodedFrames=frames,audioSeconds=round(seconds,3),peakDb=round(peak,2),rmsDb=round(mean,2),bytes=file.stat().st_size))
        run(['-ss','0.6','-i',file,'-frames:v','1',OUT/(spec['file']+'_Kapak.jpg')])
        for t in [.6,2.5,5,8,12,meta['duration']-1]:
            run(['-ss',str(t),'-i',file,'-frames:v','1',WORK/(key+f'_check_{t:.1f}.jpg')])
        print('VERIFIED',file.name,checks[-1],flush=True)
    report=OUT/'Video_Kontrol_Raporu.json'
    if keys and report.exists():
        old=json.loads(report.read_text('utf-8'))['videos']
        updated={v['file']:v for v in old}
        updated.update({v['file']:v for v in checks})
        checks=list(updated.values())
    report.write_text(json.dumps(dict(status='PASS',source='Staged Unity gameplay; 540x960 captures upscaled to 1080x1920',profileAccess='TikTok profile could not be inspected from this environment',voice='Synthetic Turkish narration: tr-TR-AhmetNeural',videos=checks),ensure_ascii=False,indent=2),'utf-8')


def package():
    report=json.loads((OUT/'Video_Kontrol_Raporu.json').read_text('utf-8'))
    assert report['status']=='PASS' and len(report['videos'])==3
    files=sorted(p for p in OUT.iterdir() if p.is_file() and p.suffix in ('.mp4','.jpg','.srt','.txt','.json'))
    assert len(files)==12,len(files)
    target=OUT/'ArenaChaos_Profil_Serisi.zip'
    with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
        for file in files:
            archive.write(file,file.name)
    with zipfile.ZipFile(target) as archive:
        assert archive.testzip() is None
    print('PACKAGE',str(target),target.stat().st_size,'bytes; 12 files; CRC PASS',flush=True)


if __name__=='__main__':
    action=sys.argv[1] if len(sys.argv)>1 else 'all'
    if action in ('capture','all'):capture()
    if action in ('voices','all'):asyncio.run(voices())
    if action in ('render','all'):render(sys.argv[2:])
    if action in ('verify','all'):verify(sys.argv[2:])
    if action in ('package','all'):package()
