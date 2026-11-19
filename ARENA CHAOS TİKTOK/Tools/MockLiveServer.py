"""Loopback-only WebSocket transport fixture. No TikTok account or external service."""
import base64, hashlib, json, socket, struct, time, sys
from pathlib import Path

port=int(sys.argv[1]) if len(sys.argv)>1 else 21214
report=Path(f'Tools/TestResults/LiveTransportServer_{port}.json')
state={'connections':0,'fragmentedMessages':0,'finished':False}
def save():report.write_text(json.dumps(state,indent=2),encoding='utf-8')
def frame(payload,opcode=1,final=True):
    data=payload.encode() if isinstance(payload,str) else payload
    length=len(data)
    return bytes([(128 if final else 0)|opcode])+ (bytes([length]) if length<126 else bytes([126])+struct.pack('!H',length))+data
def payload(message,count):
    return json.dumps({'event':'gift','data':{'giftId':'fixture-rose','giftType':1,'userId':'ws-transport-fixture','nickname':'Transport Fixture','groupId':'group-fixed','msgId':message,'repeatCount':count}})

save()
with socket.socket(socket.AF_INET,socket.SOCK_STREAM) as listener:
    listener.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEADDR,1)
    listener.bind(('127.0.0.1',port));listener.listen(1);listener.settimeout(180)
    print('READY 127.0.0.1:'+str(port),flush=True)
    for index in range(1,5):
        client,_=listener.accept()
        with client:
            client.settimeout(20);request=b''
            while b'\r\n\r\n' not in request:
                chunk=client.recv(4096)
                if not chunk:raise RuntimeError('Handshake interrupted')
                request+=chunk
            headers={line.split(':',1)[0].lower():line.split(':',1)[1].strip() for line in request.decode().split('\r\n')[1:] if ':' in line}
            accept=base64.b64encode(hashlib.sha1((headers['sec-websocket-key']+'258EAFA5-E914-47DA-95CA-C5AB0DC85B11').encode()).digest()).decode()
            client.sendall(('HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: '+accept+'\r\n\r\n').encode())
            client.sendall(frame(payload('message-1',1)))
            message=payload('message-3',3);middle=len(message)//2
            client.sendall(frame(message[:middle],final=False));time.sleep(.05);client.sendall(frame(message[middle:],opcode=0))
            client.sendall(frame(message))
            state['connections']=index;state['fragmentedMessages']+=1;save();print('CONNECTION',index,flush=True)
            if index==3:
                try:client.recv(1024)
                except (TimeoutError,ConnectionError):pass
            else:
                time.sleep(.3)
                try:client.sendall(frame(struct.pack('!H',1000),opcode=8))
                except ConnectionError:pass
    state['finished']=True;save();print('SERVER COMPLETE',flush=True)
