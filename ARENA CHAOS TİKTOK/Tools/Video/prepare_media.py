import asyncio,json,subprocess
from pathlib import Path
import edge_tts,imageio_ffmpeg

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Videos'/'TikTok_Ilk_Seri'
WORK=OUT/'Kaynaklar'
SCRIPTS={
 'meteor': 'Arenaya bir meteor düşerse ne olur? Kalkan da kurtaramaz, kaçmak da. Gönderen hasar almaz, diğer herkes elenir. Arena Chaos yakında canlı yayında. Takipte kal!',
 'fire': 'Bu yumruklar gerçekten alev alıyor! Her isabet yakıyor, rakibin canı eriyor. Sence alevli dövüşçüyü kim durdurabilir? Arena Chaos için takipte kal!',
 'classes': 'Okçu mu, kalkanlı muhafız mı? Biri uzaktan vuruyor, diğeri ön cephede dayanıyor. Sen hangisini seçerdin? Yorumlara yaz. Arena Chaos yakında!'
}
async def main():
 WORK.mkdir(parents=True,exist_ok=True)
 (WORK/'scripts.json').write_text(json.dumps(SCRIPTS,ensure_ascii=False,indent=2),encoding='utf-8')
 for key,text in SCRIPTS.items():
  media=WORK/(key+'_voice.mp3')
  if media.exists():continue
  communicate=edge_tts.Communicate(text,'tr-TR-AhmetNeural',rate='+8%',pitch='+0Hz',boundary='WordBoundary')
  words=[]
  with media.open('wb') as file:
   async for chunk in communicate.stream():
    if chunk['type']=='audio':file.write(chunk['data'])
    elif chunk['type']=='WordBoundary':words.append({'start':chunk['offset']/1e7,'duration':chunk['duration']/1e7,'text':chunk['text']})
  (WORK/(key+'_words.json')).write_text(json.dumps(words,ensure_ascii=False,indent=2),encoding='utf-8')
  print('VOICE',key,media.stat().st_size,flush=True)
 (WORK/'ffmpeg.txt').write_text(imageio_ffmpeg.get_ffmpeg_exe(),encoding='utf-8')
if __name__=='__main__':asyncio.run(main())
