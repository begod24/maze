---
name: blender-asset
description: Make a 3D asset for No Way Out and bring it into Unity — environment pieces and level modules from Blender Python build scripts, hand-made or purchased characters and entities, Mixamo animations on a Humanoid rig, HDRP materials (tiling PBR + triplanar for the environment, Substance Painter for characters and props), FBX export and import settings, and the prefab. Use for any model, animation or material work.
---

# Blender → Unity asset

Read `ArtSource/README.md`. Blender is `/Applications/Blender.app` (5.2.2 LTS). For live work use Blender MCP, and check `bpy.data.filepath` first.

## 0. Agree on it first

Before modeling, show the user a short plan:
- **what** the asset is (GDD reference),
- **which pipeline** (script, by hand, or purchased),
- **size in meters** and how it snaps to the 1 m grid,
- **material approach**,
- **animations needed**,
- **collider shape**.

New entities and characters also get 2–3 short concept options with a ★ first (silhouette, the one motion that sells it, how it reads in the dark).

## 1. Environment pieces and level modules: build scripts

- **Script:** `ArtSource/Blender/Scripts/Environment/build_<name>.py`. Shared helpers live in `ArtSource/Blender/Scripts/nwo_common.py`. Create them with the first script, and keep them minimal:
  - `reset_scene(seed)`
  - `box`, `stairs`, `cut`
  - `assign_material(obj, "M_Name")`
  - `socket(name, location, rotation)`
  - `join`
  - `save_and_export(blend_path, fbx_path)`
- **Script structure:**
  - `sys.path.insert` to `Scripts/`, then `import nwo_common as nc`,
  - every tunable number as an UPPER_SNAKE constant at the top,
  - `main()` builds → joins → adds sockets → saves and exports,
  - a fixed seed, so rerunning gives the same model.
- **Edit the script, never the `.blend`.** The output goes to `ArtSource/Blender/Environment/<Name>.blend` plus `Assets/_Project/Art/Environment/<Name>/<Name>.fbx`.
- **Grid:** 1 unit = 1 m, pieces snap to the 1 m grid, and the pivot is on the bottom face at the connection point.
- **Sockets:** level modules mark their connection points with empties named `Socket_<Type>` (`Socket_Door`, `Socket_Stair`). They import as child transforms that the generator reads.
- **Run it:**
  ```bash
  /Applications/Blender.app/Contents/MacOS/Blender -b --python "ArtSource/Blender/Scripts/Environment/build_<name>.py"
  ```
  Delete the `__pycache__` folder that gets created next to the scripts.
- **No UVs needed** for triplanar materials. If the piece will use baked lighting, Unity generates lightmap UVs (importer setting).

## 2. Characters and entities: by hand or purchased

- Work file: `ArtSource/Blender/Characters/<Name>.blend`. Third-party sources stay untouched in `ArtSource/ThirdParty/` with a `SOURCE.txt`, and you work on a copy.
- **Rig:** Humanoid-compatible bones (Hips, Spine, Chest, Neck, Head, arms, legs), with PascalCase or Mixamo names. A rig must map cleanly to a Unity Humanoid avatar.
- **The face** is a separate material slot `M_<Name>_Face`. The glitch-face effect is a shader on that slot, not geometry.
- **Readability:** one bright accent per character (GDD §9).
- **Entities** with non-human bodies use a Generic rig. Bone names are PascalCase, actions are `<Name>_<Action>`, and every non-idle action starts and ends at rest.

## 3. Animations: Mixamo + Blender

- **Mixamo** is for locomotion (idle, walk, run, jump, fall, climb, crouch). Downloading needs the user's Mixamo account, so **ask the user to download** and give them the exact list:
  - format **FBX for Unity**, **Without Skin**, 30 fps,
  - **In Place** for locomotion.

  The files go into `ArtSource/ThirdParty/Mixamo/`, then get copied into `Assets/_Project/Art/Characters/Shared/Animations/A_<Action>.fbx`.
- **Blender** is for unique actions (entities, "help up", holding hands, the Observer turning). Actions are named `<Asset>_<Action>` and exported with the character, or as an animation-only FBX.
- **Unity import for animations:**
  - Rig **Humanoid**, Avatar **Copy From Other** (the character's avatar),
  - Loop Time only on loops,
  - Root Transform Rotation and Position (Y) Bake Into Pose,
  - **no root motion**: movement is driven by code and the network.

## 4. FBX export (all assets)

```python
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    use_selection=True,
    object_types={'MESH', 'ARMATURE', 'EMPTY'},
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    bake_space_transform=False,
    add_leaf_bones=False,
    mesh_smooth_type='FACE',
    use_mesh_modifiers=True,
    bake_anim=has_animation,
)
```
- No Apply Transform (`bake_space_transform=False`). Unity's **Bake Axis Conversion** handles the axes.
- Material slot names are Unity material names (`M_ConcreteWet`).

## 5. Unity import

- `NoWayOut.Editor/Import/ArtImportPostprocessor.cs` (`AssetPostprocessor`) enforces the settings for everything under `Assets/_Project/Art/`. Create it on the first import, then only extend it. It sets:
  - `bakeAxisConversion = true`, `globalScale = 1`,
  - remaps materials **by name** to existing `M_` materials (`SearchAndRemapMaterials`),
  - Environment: `generateSecondaryUV = true`, no animation import,
  - Characters: `animationType = Human` (avatar from the model), and Mixamo animation FBXs copy the avatar,
  - Entities: `animationType = Generic`.
- After import, the model root must have rotation (0, 0, 0) and scale 1. If it doesn't, fix the importer or the export settings, never the prefab.

## 6. HDRP materials

- **Environment:**
  - Use tiling PBR sets (Poly Haven / ambientCG, CC0). Downloading files needs the user's OK: ask first, or let the user download.
  - Textures go in `Art/Textures/<Set>/T_<Set>_BaseColor`, `_Normal`, `_Mask`.
  - **HDRP Mask map** channels: R metallic, G AO, B detail mask, A smoothness = 1 − roughness. Pack it with `Tools → No Way Out → Pack HDRP Mask Map` (an editor tool in `NoWayOut.Editor`, created on first use).
  - Materials use the triplanar Shader Graph `Art/Shaders/SG_TriplanarLit` (HDRP Lit target, world-space, tiling and blend sharpness, created on first use). They live in `Art/Materials/Environment/M_<Set>.mat`.
- **Characters and props:**
  - Substance Painter project in `ArtSource/Substance/<Name>.spp`.
  - Export preset **Unity HD Render Pipeline (Metallic Standard)**, renamed to `T_<Name>_BaseColor/_Mask/_Normal` in `Art/<Domain>/<Name>/Textures/`.
  - Material `M_<Name>` uses HDRP/Lit.
- **Texture import:**
  - BaseColor in sRGB.
  - Mask and Normal not sRGB. Normal uses texture type Normal map.
  - Max size 2048 (4096 only for hero assets).

## 7. Prefab

- **Location:** `Assets/_Project/Prefabs/<Domain>/<Name>.prefab`. The root is an empty GameObject named after the asset, and the model is a child.
- **Colliders** are simple primitives, never the render mesh. Stairs get an invisible **ramp** `BoxCollider` so walking is smooth.
- **Static flags:** pieces that never move are Static. Modules the void can reshape are **not** static.
- **Networking:** interactive parts (doors, levers) are separate prefabs with a `NetworkObject` (see `add-network-behaviour`), spawned at sockets. Never nest them inside the static module.
- **Illusions:** a fake version of an asset is the same prefab with colliders removed, spawned locally by the illusion presenter. Never make a separate model "for illusions".

## 8. Verify and report

- **Blender:** render a preview (front, side, 3/4) to the scratchpad and show it to the user.
- **Unity:** without Play Mode, instantiate the prefab in `EditorSceneManager.NewPreviewScene()`, render a camera to a RenderTexture, save a PNG and show it.
- **Report:** script or `.blend`, FBX + `.meta`, textures, materials, Shader Graph, prefab, and any postprocessor change.
