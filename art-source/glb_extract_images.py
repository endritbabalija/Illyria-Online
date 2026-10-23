"""Move a .glb's embedded images out to files next to it (image.uri), compacting the BIN chunk.

Everything else in the JSON (nodes, meshes, materials, names, order) is left as is, so glTFast's
sub-asset identifiers (and thus fileIDs referenced by prefabs) don't change.
usage: python glb_extract_images.py in.glb out.glb name1 [name2 ...]   (one file stem per image)
"""
import json, struct, sys, os

src, dst, names = sys.argv[1], sys.argv[2], sys.argv[3:]
data = open(src, 'rb').read()
magic, version, length = struct.unpack_from('<III', data, 0)
assert magic == 0x46546C67 and version == 2
off = 12
chunks = []
while off < length:
    clen, ctype = struct.unpack_from('<II', data, off)
    chunks.append((ctype, data[off + 8: off + 8 + clen]))
    off += 8 + clen
gltf = json.loads(chunks[0][1].decode('utf-8'))
bin_chunk = chunks[1][1]
assert len(gltf['buffers']) == 1

images = gltf.get('images', [])
assert len(images) == len(names), f"{len(images)} images, {len(names)} names"
removed = set()
out_dir = os.path.dirname(os.path.abspath(dst))
for img, name in zip(images, names):
    bv = gltf['bufferViews'][img['bufferView']]
    raw = bin_chunk[bv.get('byteOffset', 0): bv.get('byteOffset', 0) + bv['byteLength']]
    ext = {'image/jpeg': '.jpg', 'image/png': '.png'}[img['mimeType']]
    fname = name + ext
    open(os.path.join(out_dir, fname), 'wb').write(raw)
    removed.add(img['bufferView'])
    del img['bufferView']
    del img['mimeType']
    img['uri'] = fname
    print('wrote', fname, len(raw), 'bytes')

# Compact: keep the other views in order, each 16-byte aligned (keeps every accessor's alignment).
remap, new_views, new_bin = {}, [], bytearray()
for i, bv in enumerate(gltf['bufferViews']):
    if i in removed:
        continue
    raw = bin_chunk[bv.get('byteOffset', 0): bv.get('byteOffset', 0) + bv['byteLength']]
    while len(new_bin) % 16:
        new_bin.append(0)
    bv = dict(bv)
    bv['byteOffset'] = len(new_bin)
    new_bin += raw
    remap[i] = len(new_views)
    new_views.append(bv)
while len(new_bin) % 4:
    new_bin.append(0)
gltf['bufferViews'] = new_views
gltf['buffers'][0]['byteLength'] = len(new_bin)

def fix(obj):
    if isinstance(obj, dict):
        for k, v in obj.items():
            if k == 'bufferView' and isinstance(v, int):
                assert v in remap, f"reference to removed bufferView {v}"
                obj[k] = remap[v]
            else:
                fix(v)
    elif isinstance(obj, list):
        for v in obj:
            fix(v)
for key in ('accessors',):
    fix(gltf.get(key, []))
fix(gltf.get('meshes', []))  # e.g. draco extensions
fix(gltf.get('extensions', {}))

js = json.dumps(gltf, separators=(',', ':')).encode('utf-8')
while len(js) % 4:
    js += b' '
out = bytearray(struct.pack('<III', 0x46546C67, 2, 0))
out += struct.pack('<II', len(js), 0x4E4F534A) + js
out += struct.pack('<II', len(new_bin), 0x004E4942) + new_bin
struct.pack_into('<I', out, 8, len(out))
open(dst, 'wb').write(out)
print(src, '->', dst, len(data), '->', len(out), 'bytes')
