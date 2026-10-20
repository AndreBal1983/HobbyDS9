"""Preserve verified sample-based M1 art evidence in a fresh stable directory."""
from pathlib import Path
import hashlib
import json
import shutil
import sys
import zipfile
from PIL import Image, ImageDraw, ImageFont

source=Path(sys.argv[1]);target=Path(sys.argv[2]);root=Path(__file__).resolve().parents[3]
assert not target.exists(), "Use a new delivery directory; previous evidence is preserved"
report=json.loads((source/'review-checks.json').read_text(encoding='utf-8'))
parity=json.loads((source/'parity-checks.json').read_text(encoding='utf-8'))
assert not report['errors'] and len(report['captures'])==39 and parity['exportDocumentUnchanged']
for capture in report['captures']:assert hashlib.sha256((source/capture['file']).read_bytes()).hexdigest()==capture['sha256']
shutil.copytree(source,target)
shutil.copytree(root/'art/generated-galaxy-m1',target/'source-samples')
states=['overview','mid','close','empty-region','candidate','route','overview-30','overview-50','alternate-seed']
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
sheet=Image.new('RGB',(1800,1180),(5,10,16));draw=ImageDraw.Draw(sheet)
draw.text((20,15),'Generated galaxy M1 / actual Primary samples / browser design evidence',font=font,fill=(239,177,107))
for i,state in enumerate(states):
    image=Image.open(target/f'standard-{state}.png').convert('RGB');image.thumbnail((580,327))
    x=10+(i%3)*600;y=65+(i//3)*365;sheet.paste(image,(x,y));draw.text((x,y+330),state,font=font,fill=(185,172,230))
sheet.save(target/'contact-sheet.png')
(target/'START-HERE.txt').write_text('Open review.html, then index.html for the sample viewer.\nThis uses actual Primary771a3de exports; it is not a Unity build or a replacement generator.\nNative Generate/Open/FindRoute and owner acceptance are separate.\nDefault seed42/40: 6e306b02c09344db583fd0871f806e42549e81a06697f53dd124fa1305660a8f\nNo campaign, ownership, bodies/resources, sensors, exploration progression or AI is created.\n',encoding='utf-8')
archive=target/'HobbyDS9-Generated-Galaxy-M1-Art.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED)as z:
    for p in sorted(target.rglob('*')):
        if p.is_file()and p!=archive:z.write(p,p.relative_to(target))
receipt={'status':'M1 art/browser evidence; no native/package promotion','entry':str(target/'review.html'),'backendSource':report['sourceCommit'],'defaultContentHash':parity['parity']['contentHash'],'capturedImages':39,'resolutions':['1280x720','1920x1080','3440x1440'],'sampleSeeds':['42','43','12345','20261020'],'zipBytes':archive.stat().st_size,'zipSha256':hashlib.sha256(archive.read_bytes()).hexdigest()}
(target/'delivery.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt))
