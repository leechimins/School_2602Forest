# Virtual Forest Restoration Study

Unity 3D user study: do nature sounds and ambient motion in a virtual forest improve stress recovery and presence? (within-subjects, static vs. dynamic forest)

- Engine: Unity 6000.3.15f1 (URP, desktop)
- Status: in progress

## How to run
1. Open this folder in Unity Hub (Add project from disk) with Unity 6000.3.15f1.
2. Import the free Asset Store packages (not included in this repo): Window > Package Management > Package Manager > My Assets
   - Terrain Sample Asset Pack
   - Starter Assets - FirstPerson
   - Free Pack - Tree (PolyOne Studio) — imports to `Assets/PolyOne/`; used as terrain tree prototypes

   These folders are git-ignored because Asset Store content is not redistributable (Standard Unity Asset Store EULA). After importing, the scene reconnects to them by GUID.
3. Open `Assets/Forest/Forest.unity` and press Play (WASD + mouse).
