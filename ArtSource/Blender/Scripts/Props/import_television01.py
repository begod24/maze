import os

import bpy
from mathutils import Matrix

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
SOURCE_FBX = os.path.join(REPO_ROOT, "ArtSource", "ThirdParty", "PolyHaven", "Television_01", "Television_01_2k.fbx")
BLEND_PATH = os.path.join(REPO_ROOT, "ArtSource", "Blender", "Props", "Television01.blend")
FBX_PATH = os.path.join(REPO_ROOT, "Assets", "_Project", "Art", "Props", "Television01", "Television01.fbx")
OBJECT_NAME = "Television01"
MATERIAL_NAME = "M_Television01"


def import_source():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE_FBX)

    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH':
            bpy.data.objects.remove(obj, do_unlink=True)

    return meshes[0]


def apply_transform(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def move_pivot_to_bottom_center(obj):
    xs = [vertex.co.x for vertex in obj.data.vertices]
    ys = [vertex.co.y for vertex in obj.data.vertices]
    zs = [vertex.co.z for vertex in obj.data.vertices]
    center_x = (min(xs) + max(xs)) * 0.5
    center_y = (min(ys) + max(ys)) * 0.5
    obj.data.transform(Matrix.Translation((-center_x, -center_y, -min(zs))))
    obj.location = (0.0, 0.0, 0.0)


def rename(obj):
    obj.name = OBJECT_NAME
    obj.data.name = OBJECT_NAME
    obj.material_slots[0].material.name = MATERIAL_NAME


def save_and_export(obj):
    os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='Y',
        axis_up='Z',
        bake_space_transform=False,
        add_leaf_bones=False,
        mesh_smooth_type='FACE',
        use_mesh_modifiers=True,
        bake_anim=False,
    )


def main():
    obj = import_source()
    apply_transform(obj)
    move_pivot_to_bottom_center(obj)
    rename(obj)
    save_and_export(obj)
    print("exported", FBX_PATH, tuple(round(value, 3) for value in obj.dimensions))


main()
