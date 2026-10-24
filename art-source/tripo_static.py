# Cleans a raw Tripo export of a static model (weapon, item, prop) to docs/art/assets.md: stands it on
# the game's axes with the grip at the origin, scales it to its real length, reduces it to the
# triangle budget, and swaps Tripo's material (three 4096 maps, volume extension, double-sided) for
# one matte, single-sided material with only the base colour, resized. Exports a .glb with the
# texture embedded; split it with glb_extract_images.py.
#
#   blender -b --factory-startup --python art-source/tripo_static.py -- in.glb out.glb Name
#       --along +x --side +z --grip 0.125 0.275 --length 0.6 --tris 1000 --texture 512
#
# --along  Tripo axis from the grip end to the tip (becomes Blender +Z, the glb's up axis)
# --side   Tripo axis the blade curves or the edge faces: becomes Unity +X, which is Blender −X
#          (glTFast mirrors X on import), the way the greybox sicas' tips bend
# --grip   where the hand holds, as fractions of the length from the grip end; its centre is the origin
import bpy, math, sys
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:]
src, out, name = argv[:3]
opts = {}
i = 3
while i < len(argv):
    key, n = argv[i][2:], {"grip": 2}.get(argv[i][2:], 1)
    opts[key] = argv[i + 1:i + 1 + n]
    i += 1 + n

AXES = {"x": Vector((1, 0, 0)), "y": Vector((0, 1, 0)), "z": Vector((0, 0, 1))}


def axis(s):
    return AXES[s[1]] * (-1 if s[0] == "-" else 1)


along, side = axis(opts["along"][0]), axis(opts["side"][0])
grip = [float(v) for v in opts.get("grip", ["0", "0"])]
length = float(opts["length"][0])
tris = int(opts["tris"][0])
size = int(opts.get("texture", ["512"])[0])
roughness = float(opts.get("roughness", ["0.7"])[0])

bpy.ops.wm.read_homefile(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
assert len(meshes) == 1, "expected one mesh, got %d" % len(meshes)
obj = meshes[0]
obj.name = obj.data.name = name
me = obj.data
me.transform(obj.matrix_world)
obj.matrix_world = Matrix.Identity(4)

# Origin at the middle of the grip slab, measured in Tripo's frame.
t = [v.co.dot(along) for v in me.vertices]
lo, hi = min(t), max(t)
slab = [v.co for v, tv in zip(me.vertices, t) if lo + grip[0] * (hi - lo) <= tv <= lo + grip[1] * (hi - lo)] or [v.co for v in me.vertices]
centre = Vector([(min(c[k] for c in slab) + max(c[k] for c in slab)) / 2 for k in range(3)])
centre += along * (lo + (grip[0] + grip[1]) / 2 * (hi - lo) - centre.dot(along))

# Rows of the rotation: Tripo's side → −X, side × along → +Y, along → +Z.
rot = Matrix((-side, side.cross(along), along)).to_4x4()
me.transform(rot @ Matrix.Scale(length / (hi - lo), 4) @ Matrix.Translation(-centre))
me.update()

dec = obj.modifiers.new("Decimate", "DECIMATE")
dec.ratio = min(1.0, tris / len(me.polygons))
dec.use_collapse_triangulate = True
bpy.context.view_layer.objects.active = obj
bpy.ops.object.modifier_apply(modifier=dec.name)
for p in me.polygons:
    p.use_smooth = True

# One matte material with the base colour only.
old = obj.active_material
tex = next(n.image for n in old.node_tree.nodes if n.type == "TEX_IMAGE"
           and any(l.to_socket.name == "Base Color" for l in n.outputs["Color"].links))
img = tex.copy()
img.name = name + "_BaseColor"
img.scale(size, size)
mat = bpy.data.materials.new(name)
mat.use_backface_culling = True
nodes, links = mat.node_tree.nodes, mat.node_tree.links
bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
bsdf.inputs["Metallic"].default_value = 0
bsdf.inputs["Roughness"].default_value = roughness
node = nodes.new("ShaderNodeTexImage")
node.image = img
links.new(node.outputs["Color"], bsdf.inputs["Base Color"])
me.materials.clear()
me.materials.append(mat)

bpy.ops.object.select_all(action="DESELECT")
obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=out, export_format="GLB", use_selection=True, export_image_format="JPEG",
                          export_jpeg_quality=90, export_yup=True, export_apply=True, export_animations=False)
xs = [v.co for v in me.vertices]
print("%s: %d triangles, %.3f m long, %dx%d texture, bounds x %.3f..%.3f y %.3f..%.3f z %.3f..%.3f" % (
    name, sum(len(p.vertices) - 2 for p in me.polygons), length, size, size,
    min(c.x for c in xs), max(c.x for c in xs), min(c.y for c in xs), max(c.y for c in xs), min(c.z for c in xs), max(c.z for c in xs)))
