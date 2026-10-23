# Turns the Quaternius CC0 farm Pig into a wild boar: dark bristly colours, high shoulders, a mane of
# bristles along the spine, tusks, and a headbutt "Attack" action. Saves Boar.blend and exports Boar.glb.
import bpy, bmesh, math, sys
from mathutils import Vector, Quaternion
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

argv = sys.argv[sys.argv.index("--") + 1:]
out_blend, out_glb = argv[0], argv[1]

arm = bpy.data.objects['Armature']
pig = bpy.data.objects['Pig']
me = pig.data
pig.name = 'Boar'
me.name = 'Boar'


def mat(name, rgb, rough=0.9):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = 0
    return m


bpy.data.materials['Material.003'].name = 'BoarHide'
bpy.data.materials['Material'].name = 'BoarHoof'
mat('BoarHide', (0.075, 0.046, 0.03))
mat('BoarHoof', (0.03, 0.024, 0.02), 0.6)
for m in (mat('BoarSnout', (0.24, 0.15, 0.13), 0.8), mat('BoarBristle', (0.035, 0.028, 0.024)), mat('BoarTusk', (0.78, 0.72, 0.58), 0.5)):
    me.materials.append(m)
HIDE, SNOUT, BRISTLE, TUSK = [me.materials.find(n) for n in ('BoarHide', 'BoarSnout', 'BoarBristle', 'BoarTusk')]

# Snout faces (front of the head) get their own colour.
for p in me.polygons:
    if p.center.y < -4.55 and p.material_index == HIDE:
        p.material_index = SNOUT

orig = [v.co.copy() for v in me.vertices]
src_weights = [[(g.group, g.weight) for g in v.groups] for v in me.vertices]
bvh = BVHTree.FromObject(pig, bpy.context.evaluated_depsgraph_get(), deform=False)

# High shoulders, lower rump, pricked ears: reshape the rest pose (weights stay as they are).
for v in me.vertices:
    c = v.co
    if c.z > 3.0:
        t = max(0.0, 1 - abs(c.y + 1.4) / 1.6)
        c.z += 0.5 * (0.5 - 0.5 * math.cos(math.pi * t)) * (c.z - 3.0) / 1.5
    if c.z > 3.4 and c.y > 1.0:
        c.z -= 0.25 * min(1.0, (c.y - 1.0) / 1.5) * (c.z - 3.4) / 1.2
    if c.z > 3.55 and -3.8 < c.y < -2.8 and abs(c.x) > 0.4:
        c.z += 0.35
        c.y += 0.15
me.update()
reshaped = [v.co.copy() for v in me.vertices]
kd = KDTree(len(reshaped))
for i, co in enumerate(reshaped):
    kd.insert(co, i)
kd.balance()
bvh = BVHTree.FromPolygons(reshaped, [list(p.vertices) for p in me.polygons])

bm = bmesh.new()
bm.from_mesh(me)
new_faces = []


def tri(a, b, c, mi):
    f = bm.faces.new([bm.verts.new(a), bm.verts.new(b), bm.verts.new(c)])
    f.material_index = mi
    new_faces.append(f)


def spike(base, height, width, lean):
    tip = base + Vector((0, lean, height))
    a = base + Vector((-width * 0.5, -width * 0.6, -0.15))
    b = base + Vector((width * 0.5, -width * 0.6, -0.15))
    c = base + Vector((0, width * 0.9, -0.15))
    tri(a, b, tip, BRISTLE)
    tri(b, c, tip, BRISTLE)
    tri(c, a, tip, BRISTLE)


# Mane: from behind the ears to mid back, tallest over the shoulders.
import random
rng = random.Random(7)
y = -2.7
while y < 1.2:
    t = (y + 2.7) / 3.9
    h = (0.35 + 0.5 * math.sin(math.pi * min(1.0, t * 1.5))) if t < 0.6 else 0.35 + 0.5 * (1 - t) / 0.4
    for side, k, dy in ((0.0, 1.0, 0.0), (-0.24, 0.6, 0.18), (0.24, 0.6, -0.12)):
        hit = bvh.ray_cast(Vector((side, y + dy, 9)), Vector((0, 0, -1)))
        if hit[0] is not None:
            hh = h * k * rng.uniform(0.75, 1.15)
            spike(hit[0], hh, rng.uniform(0.26, 0.38), hh * rng.uniform(0.6, 1.0))
    y += rng.uniform(0.38, 0.5)

# Tusks: curved cones from the sides of the mouth, sweeping out and up.
for sx in (-1, 1):
    base = Vector((sx * 0.6, -4.7, 2.3))
    path = [base + Vector((sx * 0.08 * i, -0.07 * i, 0.06 * i * i)) for i in range(4)]
    rings = []
    for i, p in enumerate(path):
        r = 0.15 * (1 - i / 3.6)
        rings.append([bm.verts.new(p + Vector((math.cos(a) * r, math.sin(a) * r * 0.6, 0))) for a in (0, 2.1, 4.2)])
    for i in range(len(rings) - 1):
        for k in range(3):
            f = bm.faces.new([rings[i][k], rings[i][(k + 1) % 3], rings[i + 1][(k + 1) % 3], rings[i + 1][k]])
            f.material_index = TUSK
            new_faces.append(f)
    tip = bm.verts.new(path[-1] + Vector((sx * 0.02, 0.02, 0.18)))
    for k in range(3):
        f = bm.faces.new([rings[-1][k], rings[-1][(k + 1) % 3], tip])
        f.material_index = TUSK
        new_faces.append(f)

bm.normal_update()
for f in new_faces:
    c = f.calc_center_median()
    if f.normal.dot(c - Vector((c.x * 0.3, c.y, 3.0))) < 0:
        f.normal_flip()
first_new = len(orig)
bm.to_mesh(me)
bm.free()
for p in me.polygons:
    p.use_smooth = False
groups = {g.index: g for g in pig.vertex_groups}
for v in me.vertices[first_new:]:
    _, idx, _ = kd.find(v.co)
    for g, w in src_weights[idx]:
        groups[g].add([v.index], w, 'REPLACE')
me.update()

# Headbutt: wind up with the snout down, lunge and toss the tusks up, recover.
act = bpy.data.actions.new("Attack")
arm.animation_data_create()
arm.animation_data.action = act
if hasattr(arm.animation_data, 'action_slot') and arm.animation_data.action_slot is None and len(act.slots):
    arm.animation_data.action_slot = act.slots[0]
pbs = arm.pose.bones


def local_dir(pb, v):
    return pb.bone.matrix_local.to_3x3().inverted() @ v


def pose(frame, body_dy=0.0, body_dz=0.0, head=0.0, neck=0.0, front=0.0):
    for pb in pbs:
        pb.rotation_mode = 'QUATERNION'
        pb.location = (0, 0, 0)
        pb.rotation_quaternion = (1, 0, 0, 0)
    pbs['Body'].location = local_dir(pbs['Body'], Vector((0, body_dy, body_dz)))
    for name, ang in (('Head', head), ('Neck', neck)):
        pbs[name].rotation_quaternion = Quaternion(local_dir(pbs[name], Vector((1, 0, 0))).normalized(), math.radians(ang))
    for n in ('FrontFoot.L', 'FrontFoot.R'):
        pbs[n].location = local_dir(pbs[n], Vector((0, front, 0)))
    for pb in pbs:
        pb.keyframe_insert('location', frame=frame)
        pb.keyframe_insert('rotation_quaternion', frame=frame)


pose(0)
pose(6, body_dy=0.45, body_dz=-0.15, head=28, neck=12)
pose(10, body_dy=-0.7, body_dz=-0.05, head=-30, neck=-12, front=-0.35)
pose(13, body_dy=-0.75, head=-34, neck=-14, front=-0.35)
pose(22)

for a in ('Jump', 'WalkSlow'):
    bpy.data.actions.remove(bpy.data.actions[a])
for a in bpy.data.actions:
    a.use_fake_user = True
arm.animation_data.action = bpy.data.actions['Idle']
if hasattr(arm.animation_data, 'action_slot') and len(bpy.data.actions['Idle'].slots):
    arm.animation_data.action_slot = bpy.data.actions['Idle'].slots[0]
bpy.ops.wm.save_as_mainfile(filepath=out_blend)

for o in bpy.data.objects:
    o.select_set(False)
arm.select_set(True)
pig.select_set(True)
bpy.ops.export_scene.gltf(filepath=out_glb, export_format='GLB', use_selection=True, export_animations=True,
                          export_animation_mode='ACTIONS', export_force_sampling=True, export_def_bones=False,
                          export_image_format='NONE', export_apply=False)
print("BOAR DONE", len(me.vertices), len(me.polygons))
