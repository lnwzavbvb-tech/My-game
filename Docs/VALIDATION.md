# Validation — 24 October 2026

## Restart regression

Fixed a red damage overlay persisting after restart: the simulation clock reset to zero while the old `flashUntil` deadline survived. Restart now clears all presentation timers, hit stop, camera shake, audio, transient impact rings and sprite poses/colors. Exploration remains the starting mode.

The built-player smoke test now forces a fatal hit, confirms its damage flash is active, immediately restarts, checks presentation timers/sprite tint/world mode, waits for transient effects to be destroyed and captures `restart.png`. The JSON result includes `restartAfterFatalHit`.

Unity version: 6000.6.2f1. Target: Windows x64, development build, built-in rendering pipeline.

## Browser distribution

`ArenaWebBuild.Build` creates a self-contained WebGL build for the existing GitHub Pages site. The loader uses content-hashed game assets, version metadata, a reload button and a bug-report link. `Tools/Verify-WebBuild.py` verifies template expansion, local asset references, WebAssembly magic, version agreement and the GitHub file-size limit before publication. Browser testing is separate from the earlier Windows-player smoke tests.

The first browser build exposed a Unity 6.6 native-loop callback issue: `Unity.Loading.ContentLoadingSystem.ProcessResults` was absent after linking, leading to `RuntimeError: null function`. The WebGL build now keeps the ContentLoadModule through `link.xml`, uses minimal managed stripping and disables native engine stripping. The rebuilt game was checked in Chrome: the title rendered, clicking ENTER SECTOR entered exploration and keyboard movement worked. The browser wrapper was also adjusted to keep the canvas and version/reload footer inside the viewport.

## Pixel art update

The pixel art build also passes the same 42 gameplay checks and the built-player smoke check. Visible characters now use transparent, point-filtered PNG sprites: rear-facing hunter, flying Sentinel, armored Enforcer and robed Warden. Hunter attack/guard/hurt poses and enemy attack poses are separate images. The world renders through a 225-pixel-high point-upscaled pass with discrete color bands; HUD and world labels are composited at display resolution. Antialiasing and HDR are disabled on the scene camera.

Current captures are `Docs/Preview/pixel-battle.png`, `pixel-exploration.png` and `pixel-ultimate.png`. The earlier `battle.png` captures document the previous primitive-model version.

## Compilation and build

- Runtime and editor C# compiled against the installed Unity assemblies with .NET 8.
- Unity imported the project, generated the mockup scene/materials/prefabs, compiled scripts and built the Windows player successfully.
- No C# compiler errors or warnings in the final Unity build log.

## Deterministic checks

42 checks pass through `ArenaValidation.Run` in both the standalone validator and Unity build:

- Encounter transition, boss health and normal attack cooldowns.
- LLL, LH, LLH, HL and HH damage, plus command window expiry.
- Cyan parry, counter damage and score; orange attacks bypass parry.
- Dash invulnerability, defense overlap prevention and delayed stamina regeneration.
- Ultimate meter requirement, consumption, damage and safe cinematic timing.
- Pending skill hits cannot be overwritten by ultimate.
- Weapon and character skill damage, healing and cooldowns.
- Enemy chains, death, full restart and cancellation of delayed hits.
- Defeating each enemy with ordinary attacks, health reward limits and whole-sector completion.
- Valid spawn/pickups, normalized diagonal movement and collision substeps.
- Flood-fill navigation from spawn to every enemy and pickup.

## Built-player check

The actual Windows player was launched with `--arena-smoke`. It exercised the world pickup and proximity encounter code, entered the battle scene, activated ultimate and verified the boss lost 70 HP. All five JSON flags passed. Screenshots were captured from the rendered player, rather than generated concept art.

Reviewed captures: exploration, battle windup and ultimate. Battle composition uses a fixed rear view, centered enemy, foreground hunter, purple/blue depth layers and a gold crescent. HUD panels sit outside the central character composition.

## Limits

This is a playable mockup. Final art, production animation/audio, controller/mobile support and extended player balance testing are outside this port. The automated checks validate state transitions and timing, but do not judge game feel or replace a human playthrough.
