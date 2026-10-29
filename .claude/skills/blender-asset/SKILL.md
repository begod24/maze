---
name: blender-asset
description: Make a 3D asset for No Way Out and bring it into Unity — environment pieces and level modules from Blender Python build scripts, hand-made or purchased characters and entities, Mixamo animations on a Humanoid rig, URP materials (tiling PBR + triplanar for the environment, Substance Painter for characters and props), FBX export and import settings, and the prefab. Use for any model, animation or material work.
---

# Blender → Unity asset

Read `ArtSource/README.md`. Blender is `C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe` (5.2.3 LTS, Steam). For live work use Blender MCP, and check `bpy.data.filepath` first.

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

- **Script:** one script per kit, e.g. `ArtSource/Blender/Scripts/Environment/build_floor_modules.py` (the floor module kit: a `build_<module>()` function per module and a `MODULES` list). Shared helpers live in `ArtSource/Blender/Scripts/nwo_common.py` [exists]: `reset_scene(seed)`, `new_collection`, `box` (visual), `collider` (`COL_` box, optional X rotation), `solid` (visual + collider), `empty`, `join_visuals`, `export_collection`, `save_blend`. Extend it instead of copying code.
- **Script structure:**
  - `sys.path.insert` to `Scripts/`, then `import nwo_common as nc`,
  - every tunable number as an UPPER_SNAKE constant at the top,
  - `main()`: for each module, reset with its own seed → build → join visuals → save its `.blend` → export,
  - a fixed seed, so rerunning gives the same model.
- **Edit the script, never the `.blend`.** Outputs: `ArtSource/Blender/Environment/<Module>.blend` (one file per module, so object names stay clean) and **one shared folder** `Assets/_Project/Art/Environment/Modules/<Module>.fbx`. Modular kits never get a folder per piece. Prefabs go to `Prefabs/World/Modules/`, definitions to `Data/World/Modules/`.
- **Grid:** 1 unit = 1 m. Level modules follow the floor grid (TechnicalOverview §12): 4 m cells, the pivot at the bottom center of the anchor cell, passages 2 m wide in the middle of a cell face, floor top at z = 0. Blender +Y is north (Unity +Z), +X is east.
- **Colliders:** an object named `COL_<Name>` becomes a `BoxCollider` on import (its mesh bounds), and its renderer is removed (`ArtImportPostprocessor`). Walkable slabs and walls use `nc.solid`. Stairs use an invisible tilted `COL_Ramp` and visual-only steps.
- **Markers:** empties `Spawn_N` (start spawns, collected into `PlayerSpawnPoints` on the prefab), `Illusion_N` (illusion slots), `Door_<Name>` (interactable sockets). They import as child transforms.
- **A new module** needs: its builder in the kit script, a run, a prefab in `Prefabs/World/Modules`, a `WorldModuleDefinition` (role, weight, cells, passages) and an entry in `WorldModuleCatalog`. Check it with `Tools → No Way Out → Preview Generated Floor` and the `FloorGeneratorTests`.
- **Run it:**
  ```bash
  "/c/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" -b --factory-startup --python "ArtSource/Blender/Scripts/Environment/build_floor_modules.py"
  ```
  This is Git Bash syntax. Paths inside Python scripts use forward slashes or `os.path.join`.
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
    axis_forward='Y',
    axis_up='Z',
    bake_space_transform=False,
    add_leaf_bones=False,
    mesh_smooth_type='FACE',
    use_mesh_modifiers=True,
    bake_anim=has_animation,
)
```
- Export in Blender's own axes (**Y forward, Z up**) with no Apply Transform (`bake_space_transform=False`), and let Unity's **Bake Axis Conversion** convert them. Verified on Television01: `-Z forward / Y up` leaves a 90° X rotation on the root node in Unity.
- Apply rotation and scale in Blender before exporting (`transform_apply`), so the root comes in with rotation 0 and scale 1.
- Material slot names are Unity material names (`M_ConcreteWet`).
- **Third-party models** (Poly Haven, store packs) go through a small conversion script in `ArtSource/Blender/Scripts/Props/` (see `import_television01.py`): import the original from `ThirdParty/`, apply transforms, move the pivot to the bottom center, rename the object and its material slot to `M_<Name>`, save `Blender/Props/<Name>.blend` and export the FBX.

## 5. Unity import

- `NoWayOut.Editor/Import/ArtImportPostprocessor.cs` (`AssetPostprocessor`) enforces the settings for everything under `Assets/_Project/Art/` [exists]. Only extend it. It sets:
  - `bakeAxisConversion = true`, `globalScale = 1`,
  - remaps material slots named `M_<Name>` to the existing `M_<Name>.mat` under `Art/` (`OnAssignMaterialModel`). Create the material before the first import,
  - Environment: `generateSecondaryUV = true`, no animation import,
  - Characters: `animationType = Human` (avatar from the model). Copying the avatar to Mixamo animation FBXs is not written yet: add it with the first Mixamo import,
  - Entities: `animationType = Generic`,
  - Props and everything else: no animation,
  - textures: `_Normal` → normal map, `_Mask` / `_ARM` → not sRGB, max size 2048 on first import.
- Files copied in before the postprocessor compiled keep the old settings: reimport them (`ImportAssetOptions.ForceUpdate`).
- After import, the model root must have rotation (0, 0, 0) and scale 1. If it doesn't, fix the importer or the export settings, never the prefab.

## 6. URP materials

- **Environment:**
  - Use tiling PBR sets (Poly Haven / ambientCG, CC0). Downloading files needs the user's OK: ask first, or let the user download.
  - Textures go in `Art/Textures/<Set>/T_<Set>_BaseColor`, `_Normal`, `_Mask`.
  - **Mask map** channels: R metallic, G AO, B unused (255), A smoothness = 1 − roughness. Select a `T_<Name>_ARM` texture (glTF/Poly Haven packing: R AO, G roughness, B metallic) and run `Tools → No Way Out → Pack Mask Map (from ARM)` [exists]. It writes `T_<Name>_Mask.png` next to it. Then delete the ARM copy from `Assets` (the original stays in `ArtSource/ThirdParty`). Sets with separate maps need the tool extended first.
  - Materials use `NoWayOut/Triplanar Lit` (`Art/Shaders/TriplanarLit.shader` [exists]: world-space, `_TexWorldScale`, `_BlendSharpness`, mask map with metallic/smoothness/AO remap ranges, enable `_NORMALMAP` and `_MASKMAP`). They live in `Art/Materials/Environment/M_<Set>.mat`.
- **Characters and props:**
  - Substance Painter project in `ArtSource/Substance/<Name>.spp`.
  - Export preset **Unity Universal Render Pipeline (Metallic Standard)**, or the HDRP preset packed into the same mask map, renamed to `T_<Name>_BaseColor/_Mask/_Normal` in `Art/<Domain>/<Name>/Textures/`.
  - Material `M_<Name>` uses `Universal Render Pipeline/Lit`: `_BaseMap`, `_BumpMap`, the mask as both `_MetallicGlossMap` and `_OcclusionMap`, `_Smoothness` 1 (keywords `_NORMALMAP`, `_METALLICSPECGLOSSMAP`, `_OCCLUSIONMAP`).
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
