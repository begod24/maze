import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import nwo_common as nc

SEED = 1729
CELL = 4.0
HALF_CELL = CELL * 0.5
SLAB = 0.5
BRIDGE_HALF_WIDTH = 1.0
NARROW_HALF_WIDTH = 0.5
GAP_HALF = 0.75
STEPS = 16
STEP_RISE = CELL / STEPS
STEP_RUN = CELL * 2.0 / STEPS
RAMP_THICKNESS = 0.3
CORRIDOR_HALF_WIDTH = 1.5
CORRIDOR_HEIGHT = 3.0
WALL = 0.25
DOOR_HALF_WIDTH = 0.8
DOOR_HEIGHT = 2.4
FRAME = 0.25
FRAME_DEPTH = 0.15
ROOM_WALL_HEIGHT = 5.0
EXIT_DOOR_HALF_WIDTH = 1.0
EXIT_DOOR_HEIGHT = 3.0
PILLAR = 0.6
PILLAR_HEIGHT = 3.0
SPAWN_HEIGHT = 0.05
HANGING_MIN = 0.6
HANGING_MAX = 1.6

FLOOR = "M_ConcreteFloor"
WALL_MATERIAL = "M_ConcreteWall"
ACCENT = "M_ConcreteLayers"

def blend_path(name):
    return os.path.join(nc.REPO_ROOT, "ArtSource", "Blender", "Environment", name + ".blend")


def fbx_path(name):
    return os.path.join(nc.REPO_ROOT, "Assets", "_Project", "Art", "Environment", "Modules", name + ".fbx")


def slab(collection, name, x0, x1, y0, y1, top=0.0):
    nc.solid(collection, name, (x0, y0, top - SLAB), (x1, y1, top), FLOOR)


def hanging_blocks(collection, x0, x1, y0, y1, count):
    for index in range(count):
        size = random.uniform(HANGING_MIN, HANGING_MAX)
        x = random.uniform(x0 + size * 0.5, x1 - size * 0.5)
        y = random.uniform(y0 + size * 0.5, y1 - size * 0.5)
        top = -SLAB
        bottom = top - random.uniform(size, size * 3.0)
        nc.box(collection, "Hanging_%d" % index, (x - size * 0.5, y - size * 0.5, bottom), (x + size * 0.5, y + size * 0.5, top), WALL_MATERIAL)


def build_platform(collection):
    slab(collection, "Floor", -HALF_CELL, HALF_CELL, -HALF_CELL, HALF_CELL)
    hanging_blocks(collection, -HALF_CELL, HALF_CELL, -HALF_CELL, HALF_CELL, 3)


def build_bridge(collection):
    slab(collection, "Floor", -BRIDGE_HALF_WIDTH, BRIDGE_HALF_WIDTH, -HALF_CELL, HALF_CELL)
    hanging_blocks(collection, -BRIDGE_HALF_WIDTH, BRIDGE_HALF_WIDTH, -HALF_CELL, HALF_CELL, 1)


def build_bridge_gap(collection):
    slab(collection, "FloorSouth", -BRIDGE_HALF_WIDTH, BRIDGE_HALF_WIDTH, -HALF_CELL, -GAP_HALF)
    slab(collection, "FloorNorth", -BRIDGE_HALF_WIDTH, BRIDGE_HALF_WIDTH, GAP_HALF, HALF_CELL)
    nc.empty(collection, "Illusion_1", (0.0, 0.0, 0.0))


def build_bridge_broken(collection):
    slab(collection, "FloorSouth", -BRIDGE_HALF_WIDTH, BRIDGE_HALF_WIDTH, -HALF_CELL, -1.0)
    slab(collection, "FloorNarrow", -NARROW_HALF_WIDTH, NARROW_HALF_WIDTH, -1.0, 1.0)
    slab(collection, "FloorNorth", -BRIDGE_HALF_WIDTH, BRIDGE_HALF_WIDTH, 1.0, HALF_CELL)
    hanging_blocks(collection, -BRIDGE_HALF_WIDTH, BRIDGE_HALF_WIDTH, -HALF_CELL, -1.0, 1)
    nc.empty(collection, "Illusion_1", (0.0, 0.0, 0.0))


def build_stair(collection):
    for index in range(STEPS):
        y0 = -HALF_CELL + STEP_RUN * index
        top = STEP_RISE * (index + 1)
        nc.box(collection, "Step_%d" % index, (-BRIDGE_HALF_WIDTH, y0, top - SLAB), (BRIDGE_HALF_WIDTH, y0 + STEP_RUN, top), FLOOR)

    run = CELL * 2.0
    rise = CELL
    length = math.hypot(run, rise)
    angle = math.degrees(math.atan2(rise, run))
    normal_y = -rise / length
    normal_z = run / length
    center_y = -HALF_CELL + run * 0.5 - normal_y * RAMP_THICKNESS * 0.5
    center_z = rise * 0.5 - normal_z * RAMP_THICKNESS * 0.5
    nc.collider(collection, "Ramp", (0.0, center_y, center_z), (BRIDGE_HALF_WIDTH * 2.0, length, RAMP_THICKNESS), angle)


def build_doorway(collection):
    build_bridge(collection)
    post_x0 = DOOR_HALF_WIDTH
    post_x1 = DOOR_HALF_WIDTH + FRAME
    top = DOOR_HEIGHT + FRAME
    nc.solid(collection, "PostWest", (-post_x1, -FRAME_DEPTH, 0.0), (-post_x0, FRAME_DEPTH, top), ACCENT)
    nc.solid(collection, "PostEast", (post_x0, -FRAME_DEPTH, 0.0), (post_x1, FRAME_DEPTH, top), ACCENT)
    nc.solid(collection, "Lintel", (-post_x1, -FRAME_DEPTH, DOOR_HEIGHT), (post_x1, FRAME_DEPTH, top), ACCENT)
    nc.empty(collection, "Illusion_1", (0.0, 0.0, DOOR_HEIGHT * 0.5))


def build_corridor(collection):
    slab(collection, "Floor", -CORRIDOR_HALF_WIDTH, CORRIDOR_HALF_WIDTH, -HALF_CELL, HALF_CELL)
    nc.solid(collection, "WallWest", (-CORRIDOR_HALF_WIDTH, -HALF_CELL, 0.0), (-CORRIDOR_HALF_WIDTH + WALL, HALF_CELL, CORRIDOR_HEIGHT), WALL_MATERIAL)
    nc.solid(collection, "WallEast", (CORRIDOR_HALF_WIDTH - WALL, -HALF_CELL, 0.0), (CORRIDOR_HALF_WIDTH, HALF_CELL, CORRIDOR_HEIGHT), WALL_MATERIAL)
    nc.solid(collection, "Ceiling", (-CORRIDOR_HALF_WIDTH, -HALF_CELL, CORRIDOR_HEIGHT), (CORRIDOR_HALF_WIDTH, HALF_CELL, CORRIDOR_HEIGHT + WALL), WALL_MATERIAL)


def build_junction(collection):
    slab(collection, "Floor", -CELL - HALF_CELL, CELL + HALF_CELL, -HALF_CELL, HALF_CELL)
    hanging_blocks(collection, -CELL - HALF_CELL, CELL + HALF_CELL, -HALF_CELL, HALF_CELL, 4)


def build_start(collection):
    x0 = -CELL - HALF_CELL
    x1 = CELL + HALF_CELL
    y0 = -HALF_CELL
    y1 = CELL + HALF_CELL
    slab(collection, "Floor", x0, x1, y0, y1)
    hanging_blocks(collection, x0, x1, y0, y1, 6)
    nc.solid(collection, "PillarSouthWest", (x0, y0, 0.0), (x0 + PILLAR, y0 + PILLAR, PILLAR_HEIGHT), WALL_MATERIAL)
    nc.solid(collection, "PillarSouthEast", (x1 - PILLAR, y0, 0.0), (x1, y0 + PILLAR, PILLAR_HEIGHT), WALL_MATERIAL)
    spawns = [(-1.5, 2.5), (1.5, 2.5), (-1.5, 0.5), (1.5, 0.5)]
    for index, (x, y) in enumerate(spawns):
        nc.empty(collection, "Spawn_%d" % (index + 1), (x, y, SPAWN_HEIGHT))


def build_exit_room(collection):
    x0 = -CELL - HALF_CELL
    x1 = CELL + HALF_CELL
    y0 = -HALF_CELL
    y1 = CELL + HALF_CELL
    slab(collection, "Floor", x0, x1, y0, y1)
    nc.solid(collection, "WallWest", (x0, y0, 0.0), (x0 + WALL, y1, ROOM_WALL_HEIGHT), WALL_MATERIAL)
    nc.solid(collection, "WallEast", (x1 - WALL, y0, 0.0), (x1, y1, ROOM_WALL_HEIGHT), WALL_MATERIAL)
    nc.solid(collection, "WallNorthWest", (x0, y1 - WALL, 0.0), (-EXIT_DOOR_HALF_WIDTH, y1, ROOM_WALL_HEIGHT), WALL_MATERIAL)
    nc.solid(collection, "WallNorthEast", (EXIT_DOOR_HALF_WIDTH, y1 - WALL, 0.0), (x1, y1, ROOM_WALL_HEIGHT), WALL_MATERIAL)
    nc.solid(collection, "WallAboveDoor", (-EXIT_DOOR_HALF_WIDTH, y1 - WALL, EXIT_DOOR_HEIGHT), (EXIT_DOOR_HALF_WIDTH, y1, ROOM_WALL_HEIGHT), WALL_MATERIAL)
    nc.solid(collection, "ExitDoor", (-EXIT_DOOR_HALF_WIDTH, y1 - WALL * 2.0, 0.0), (EXIT_DOOR_HALF_WIDTH, y1 - WALL, EXIT_DOOR_HEIGHT), ACCENT)
    nc.empty(collection, "Door_Exit", (0.0, y1 - WALL * 2.0, 0.0))


def build_uturn(collection):
    slab(collection, "Floor", -CELL - HALF_CELL, HALF_CELL, -HALF_CELL, HALF_CELL)
    hanging_blocks(collection, -CELL - HALF_CELL, HALF_CELL, -HALF_CELL, HALF_CELL, 2)


MODULES = [
    ("ModulePlatform", build_platform),
    ("ModuleBridge", build_bridge),
    ("ModuleBridgeGap", build_bridge_gap),
    ("ModuleBridgeBroken", build_bridge_broken),
    ("ModuleStair", build_stair),
    ("ModuleDoorway", build_doorway),
    ("ModuleCorridor", build_corridor),
    ("ModuleJunction", build_junction),
    ("ModuleStart", build_start),
    ("ModuleExitRoom", build_exit_room),
    ("ModuleUTurn", build_uturn),
]


def main():
    for name, builder in MODULES:
        nc.reset_scene("%d-%s" % (SEED, name))
        collection = nc.new_collection(name)
        builder(collection)
        nc.join_visuals(collection, name)
        nc.save_blend(blend_path(name))
        nc.export_collection(collection, fbx_path(name))
        print("exported", name)


main()
