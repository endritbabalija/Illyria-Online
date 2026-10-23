# Bandit.glb from the Hooded Adventurer (Quaternius, CC0): JSON-only edit, mesh/rig/animations untouched.
# Crimson hood, dark leather, black hair, matte materials (FBX2glTF made everything metallic 0.4 /
# roughness 0.27).
import json, struct, sys

src, dst = sys.argv[1], sys.argv[2]
data = open(src, 'rb').read()
jlen = struct.unpack('<I', data[12:16])[0]
j = json.loads(data[20:20 + jlen])
rest = data[20 + jlen:]

colours = {
    'DarkBrown': [0.075, 0.011, 0.009],  # hood, sleeves, leggings: bandit maroon
    'LightBrown': [0.055, 0.032, 0.02],  # vest, boots: dark leather
    'White': [0.025, 0.022, 0.02],       # hair
    'Brown': [0.02, 0.012, 0.008],
}
for m in j['materials']:
    pbr = m['pbrMetallicRoughness']
    if m['name'] in colours:
        pbr['baseColorFactor'] = colours[m['name']] + [1.0]
    metal = m['name'] in ('Metal', 'Metal_Dark', 'Gold')
    pbr['metallicFactor'] = 0.6 if metal else 0.0
    pbr['roughnessFactor'] = 0.4 if metal else (0.6 if m['name'] == 'Skin' else 0.85)

js = json.dumps(j, separators=(',', ':')).encode()
js += b' ' * ((4 - len(js) % 4) % 4)
body = struct.pack('<I', len(js)) + b'JSON' + js + rest
open(dst, 'wb').write(b'glTF' + struct.pack('<II', 2, 12 + len(body)) + body)
print('bandit written', dst)
