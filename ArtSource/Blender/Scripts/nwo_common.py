import math
import os
import random

import bpy

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
COLLIDER_PREFIX = "COL_"


def reset_scene(seed):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    random.seed(seed)


def new_collection(name):
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    return collection


def get_material(name):
    material = bpy.data.materials.get(name)
    if material is None:
        material = bpy.data.materials.new(name)
    return material


def box(collection, name, minimum, maximum, material_name):
    x0, y0, z0 = minimum
    x1, y1, z1 = maximum
    vertices = [
        (x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
        (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1),
    ]
    faces = [
        (0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
        (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7),
    ]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    if material_name is not None:
        mesh.materials.append(get_material(material_name))
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    return obj


def collider(collection, name, center, size, rotation_x_degrees=0.0):
    half = (size[0] * 0.5, size[1] * 0.5, size[2] * 0.5)
    obj = box(collection, COLLIDER_PREFIX + name, (-half[0], -half[1], -half[2]), half, None)
    obj.location = center
    obj.rotation_euler = (math.radians(rotation_x_degrees), 0.0, 0.0)
    return obj


def solid(collection, name, minimum, maximum, material_name):
    box(collection, name, minimum, maximum, material_name)
    center = tuple((minimum[i] + maximum[i]) * 0.5 for i in range(3))
    size = tuple(maximum[i] - minimum[i] for i in range(3))
    collider(collection, name, center, size)


def empty(collection, name, location, rotation_z_degrees=0.0):
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = 'ARROWS'
    obj.location = location
    obj.rotation_euler = (0.0, 0.0, math.radians(rotation_z_degrees))
    collection.objects.link(obj)
    return obj


def join_visuals(collection, name):
    visuals = [obj for obj in collection.objects if obj.type == 'MESH' and not obj.name.startswith(COLLIDER_PREFIX)]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in visuals:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = visuals[0]
    bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = name
    joined.data.name = name
    return joined


def export_collection(collection, fbx_path):
    os.makedirs(os.path.dirname(fbx_path), exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        object_types={'MESH', 'EMPTY'},
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='Y',
        axis_up='Z',
        bake_space_transform=False,
        add_leaf_bones=False,
        mesh_smooth_type='FACE',
        use_mesh_modifiers=True,
        bake_anim=False,
    )


def save_blend(blend_path):
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
