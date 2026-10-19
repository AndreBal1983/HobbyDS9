"""Standard-library-only proof of the tracked, non-activated candidate payload."""
from pathlib import Path
import hashlib
import json

ROOT=Path(__file__).resolve().parents[2]
PACK=ROOT/'assets/art/galaxy-class-meshy-v2-runtime'
manifest=json.loads((PACK/'candidate-manifest.json').read_text(encoding='utf-8'))
for entry in manifest['files']:
    file=PACK/entry['file'];data=file.read_bytes()
    assert len(data)==entry['bytes'] and hashlib.sha256(data).hexdigest()==entry['sha256'],file
derivative=json.loads((PACK/'derivative-manifest.json').read_text(encoding='utf-8'))
assert derivative['sourceGlbSha256']==manifest['sourceGlbSha256']
assert [r['triangles'] for r in derivative['lods']]==[60000,20000,7000]
assert [r['materialSlots'] for r in derivative['lods']]==[1,1,1]
for entry in derivative['textureExtraction']:
    assert hashlib.sha256((PACK/entry['file']).read_bytes()).hexdigest()==entry['sha256']
print(f"Verified {len(manifest['files'])} CPU candidate files and exact source texture hashes. This check does not activate Unity.")
