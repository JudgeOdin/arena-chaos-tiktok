import json,time,sys,shutil
from pathlib import Path
from datetime import datetime


def recording_candidates(arguments):
    """Convert live UTC markers into offsets for the existing promo workflow."""
    import argparse

    parser = argparse.ArgumentParser()
    parser.add_argument("--candidates", type=Path, required=True)
    parser.add_argument("--recording-start", required=True, help="ISO 8601 time with UTC offset")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args(arguments)
    start = datetime.fromisoformat(args.recording_start.replace("Z", "+00:00"))
    if start.tzinfo is None:
        parser.error("--recording-start requires a UTC offset")

    clips = []
    for line in args.candidates.read_text(encoding="utf-8-sig").splitlines():
        if not line.strip():
            continue
        event = json.loads(line)
        when = datetime.fromisoformat(event["utc"].replace("Z", "+00:00"))
        offset = (when - start).total_seconds()
        if offset >= 0:
            clips.append({
                **event,
                "recordingOffset": round(offset, 3),
                "in": round(max(0, offset - 7), 3),
                "out": round(offset + 5, 3),
            })

    args.output.write_text(json.dumps(clips, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"{len(clips)} clip candidates written to {args.output}")


if "--candidates" in sys.argv:
    recording_candidates(sys.argv[1:])
    sys.exit(0)

import imageio_ffmpeg

ROOT=Path(__file__).resolve().parents[2]
WORK=ROOT/'Videos'/'TikTok_Ilk_Seri'/'Kaynaklar'
REQUEST=ROOT/'Tools'/'Video'/'CaptureRequest.json'
WORK.mkdir(parents=True,exist_ok=True)
for scenario,seconds in [('meteor',8),('fire',13),('classes',13)]:
 if any(a!='--force' for a in sys.argv[1:]) and scenario not in sys.argv[1:]:continue
 report=WORK/(scenario+'_capture.json')
 if '--force' not in sys.argv and report.exists() and json.loads(report.read_text('utf-8-sig')).get('status')=='PASS':
  print('ALREADY CAPTURED',scenario,flush=True);continue
 config={'scenario':scenario,'folder':str(WORK),'ffmpeg':imageio_ffmpeg.get_ffmpeg_exe(),'size':960,'fps':60,'seconds':seconds}
 began=time.time();temp=REQUEST.with_suffix('.tmp');temp.write_text(json.dumps(config),encoding='utf-8');temp.replace(REQUEST)
 print('START CAPTURE',scenario,seconds,flush=True);last=0
 while True:
  if time.time()-began>300:raise RuntimeError('Capture timed out: '+scenario)
  if report.exists() and report.stat().st_mtime>=began:
   data=json.loads(report.read_text('utf-8-sig'))
   if data.get('status')!='PASS':raise RuntimeError(json.dumps(data,ensure_ascii=False))
   if data['frames']!=round(seconds*60):raise RuntimeError('Incomplete frame count')
   shutil.copyfile(WORK/(scenario+'_raw.mp4'),WORK/(scenario+'_upright.mp4'))
   print('CAPTURE PASS',scenario,'frames',data['frames'],'contacts',data['contacts'],'arrows',data['arrowHits'],flush=True)
   break
  if time.time()-last>15:
   progress=WORK/'progress.txt'
   if progress.exists():print(progress.read_text('utf-8-sig'),flush=True)
   last=time.time()
  time.sleep(1)
 time.sleep(4)
print('ALL GAMEPLAY CAPTURES COMPLETE',flush=True)
