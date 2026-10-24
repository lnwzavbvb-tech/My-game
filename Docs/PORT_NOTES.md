# Unity port decisions

- Combat uses transparent pixel sprites on camera-facing planes in a 3D environment, presented by a fixed orthographic camera. Exploration movement/collision is evaluated on an X/Z ground plane. The battle camera looks from behind the hunter toward a centered, elevated enemy.
- Character pixel art is generated reproducibly by `PixelArtAssets.cs` as transparent PNGs. Primitive character meshes remain as disabled mockup parts for the original rig references; visible characters are SpriteRenderers.
- The world is reduced to 225 pixels in height and point-upscaled, with discrete color bands. UI is composited afterward at native display resolution. Antialiasing/HDR are disabled for the pixel pass, and sprite textures have no mipmaps or compression.
- The battle backdrop uses a gold crescent, purple stone steps, blue/violet spires and suspended fragments to establish the composition of the supplied reference without requiring final art.
- All action cooldowns, enemy windups, delayed skills, invulnerability and transitions use one explicit simulation clock. Pausing and hit stop therefore preserve the timing windows.
- Delayed skill hits are canceled on victory, defeat and restart. Ultimate cannot replace a skill hit that is still pending. The enemy windup pauses during the ultimate cinematic.
- The original combo damage and enemy chains were retained. A 1.1 second command window resets unfinished combos.
- Map movement normalizes diagonals and uses collision substeps so sprinting cannot tunnel through thin walls. Navigation validation proves paths from the spawn to all targets and pickups.
- The original browser `gain(100)` behavior is preserved: collecting a health pickup also fills the ultimate gauge through its score reward.
- No account credentials, user data or license files are included in the project.
