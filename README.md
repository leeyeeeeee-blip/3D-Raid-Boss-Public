# 3D Raid Boss

A Unity portfolio prototype focused on a short, mechanics-heavy raid encounter. The public repository contains the gameplay code, scenes, tests, project settings, and a design document while intentionally excluding third-party visual-effect packs and local AI/editor integrations.

## Highlights

- Three-minute boss timeline with multiple encounter phases
- Telegraph-before-damage arena mechanics and knockback interactions
- Player skill system with cast time, GCD, stacks, charges, buffs, and cooldowns
- HUD for HP, cast progress, GCD, skill state, incoming damage, and battle time
- Battle records with damage breakdown, skill usage, incoming-damage source, and timestamps
- Main menu, settings, pause, retry, victory, and defeat flows
- EditMode tests for core combat and encounter behavior

## Open the project

- Unity Editor: `6000.4.11f1`
- Open this repository root from Unity Hub.
- Entry menu scene: `Assets/Scenes/MainMenu.unity`
- Gameplay scene: `Assets/SampleScene.unity`

Unity will resolve the official packages listed in `Packages/manifest.json` on first open.

## Public-edition differences

The original private project uses an external particle pack for presentation. Those files, their generated examples, Coplay, Unity MCP, Unity AI Assistant, local caches, cloud project identifiers, and raw development history are intentionally omitted here. Core gameplay and telegraph visuals rely on project-owned code and Unity primitives.

## Project ownership and AI assistance

The project owner defined the gameplay requirements, encounter timeline, skill rules, UI behavior, battle-record requirements, testing criteria, and iteration feedback. AI-assisted tools supported implementation and debugging. See `Docs/GameDesignDocument.txt` for the original gameplay specification.

## License

This repository is source-available for portfolio review. No permission is granted to copy, modify, or redistribute the project code except where a third-party notice states otherwise. See `LICENSE.md` and `THIRD_PARTY_NOTICES.md`.
