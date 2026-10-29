# ArtSource

Source art that Unity never imports. Only exported results go into `Assets/_Project/Art/`. The step-by-step workflow is in the `blender-asset` skill.

```
ArtSource/
  Blender/
    Scripts/          nwo_common.py (shared helpers), Environment/build_floor_modules.py (the floor module kit), Props/ conversion scripts for third-party models
    Environment/      one .blend per module, produced by the build scripts. Don't edit them by hand
    Characters/       hand-made characters and entities (.blend)
    Props/            hand-made props (.blend)
    Assets_NWO.blend  the original asset file
  Substance/          Substance Painter projects (.spp) for characters and props
  ThirdParty/         purchased or downloaded sources, kept with their original names, plus SOURCE.txt
    Mixamo/           Mixamo downloads (FBX for Unity)
```

## The pipeline is mixed

| What | How | Source of truth |
|---|---|---|
| Environment (blocks, stairs, doors, bridges, level modules) | Python build script run in Blender in the background, with a fixed seed | the script. The `.blend` and FBX are its output |
| Characters, entities, hero props | Modeled by hand, or purchased and reworked | the `.blend` in `Characters/` or `Props/` |
| Locomotion animation (walk, run, jump, climb) | Mixamo on a Humanoid rig | `ThirdParty/Mixamo/` |
| Unique animation (entities, "help up", holding hands) | Blender actions | the character's `.blend` |
| Environment materials | Tiling PBR sets (Poly Haven / ambientCG, CC0) + triplanar Shader Graph in Unity | the texture set |
| Character and prop textures | Substance Painter, HDRP export preset | the `.spp` |

## Rules

- **Blender:** `C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe` (5.2.3 LTS, Steam). Background runs from Git Bash use `"/c/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" -b --python <script>`.
- **Export target:** `Assets/_Project/Art/<Domain>/<AssetName>/<AssetName>.fbx` (Domain: Characters, Entities, Environment, Props). Textures go in a `Textures/` folder next to it. **Modular kits** (level modules) share one folder: `Assets/_Project/Art/Environment/Modules/<Module>.fbx`.
- **Colliders and markers:** objects named `COL_*` become BoxColliders in Unity. Empties `Spawn_N`, `Illusion_N`, `Door_*` mark points.
- **Units:** 1 Blender unit = 1 m. Environment pieces snap to a **1 m grid**. The pivot sits on the bottom face at the connection point. Level module sockets are empties named `Socket_<Type>`.
- **Axes:** apply rotation and scale in Blender, then export in Blender's axes (**Y forward, Z up**) with **no** Apply Transform. Unity's `bakeAxisConversion` (set by the import postprocessor) brings models in with rotation 0 and scale 1.
- **Third-party models** are converted by a script in `Blender/Scripts/Props/` (e.g. `import_television01.py`), which writes `Blender/Props/<Name>.blend` and the FBX in `Assets`.
- **Names:** PascalCase objects, meshes and bones (`Root`, `Spine`, `Head`). Actions are `<Asset>_<Action>`. Material slots use the Unity material name (`M_ConcreteWet`), so imports remap cleanly.
- **Never edit an exported FBX in Unity.** Fix the source and re-export.
- **Third-party:** keep the originals untouched in `ThirdParty/<name>/`, with a `SOURCE.txt` (link, license, date). Work on a copy in `Blender/`.
- **Blender MCP:** check `bpy.data.filepath` first, because the open GUI may hold another file.
- **`.blend1` backups** can be deleted at any time.
