"""Original synthesized spell cues; deterministic and free of third-party recordings."""
import math, random, struct, wave
from pathlib import Path
out=Path('Assets/Resources/ArenaChaos/SpellAudio');out.mkdir(parents=True,exist_ok=True)
rate=22050
for name,duration in [('Fire',.7),('Ice',.65),('Lightning',.45),('Magic',.8),('Meteor',2.4)]:
    rng=random.Random(name);samples=[];low=0
    for i in range(int(rate*duration)):
        t=i/rate;p=t/duration;n=rng.uniform(-1,1);low=.91*low+.09*n
        fade=min(1,t/.008)*math.exp(-p*5)
        if name=='Fire': value=(low*2.2+n*.12+math.sin(2*math.pi*(100*t-25*t*t))*.15)*fade
        elif name=='Ice':value=sum(math.sin(2*math.pi*f*t)*math.exp(-p*k) for f,k in [(1480,3),(2137,5),(3211,7)])*.16*fade+n*.06*fade
        elif name=='Lightning':value=(n*.65*(.4+.6*math.sin(t*320)**8)+math.sin(2*math.pi*70*t)*.25)*fade
        elif name=='Meteor':value=(low*2.5+math.sin(2*math.pi*(58*t-7*t*t))*.4+n*.16)*fade
        else:value=(math.sin(2*math.pi*(440*t+140*t*t))*.25+math.sin(2*math.pi*660*t)*.13+low*.35)*fade
        samples.append(max(-.95,min(.95,value)))
    with wave.open(str(out/(name+'.wav')),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(b''.join(struct.pack('<h',int(v*32767)) for v in samples))
print('Generated five original spell cues')
