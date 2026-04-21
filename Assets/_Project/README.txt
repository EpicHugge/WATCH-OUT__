WATCH-OUT Project Folder Guide

Use these folders as the main navigation points inside Assets/_Project:

- Art: project art, materials, and imported/generated models.
- Audio: sound effects and future music if added later.
- Blockout: the blockout toolkit and its content.
- Prefabs: gameplay-ready prefabs grouped by feature.
- Rendering Assets: experimental shaders, materials, and spare URP assets.
- Resources: runtime-loaded shared assets, currently just the shared VCR font.
- Scenes: playable and test scenes.
- Scripts: gameplay and editor code grouped by system.
- Settings: render pipeline and project-side asset settings.
- ThirdParty: external tools and imported asset packs.

Current ThirdParty layout:

- Cans Pack: imported canned food prop pack.
- Meshy Bridge: the editor integration used to import Meshy models.
- Polygon-Lite Survival Collection: external survival prop pack.
- TextMesh Pro: TMP fonts, sprites, and support assets.

Current generated/imported art layout:

- Art/Imported/Meshy: Meshy-generated models and textures imported into the project.
- Art/Materials/Radio: radio-specific materials used by the interaction setup.

Current shared font layout:

- Resources/Fonts/VCR_OSD_MONO_1.001.ttf: the one canonical VCR font used by runtime UI and prefab-building tools.

Current radio/dialogue layout:

- Dialogue/Conversations/Radio Conversations: radio-triggered conversations and signal broadcasts.
- Dialogue/Conversations/Cassette Conversations: conversations played directly from cassette tapes.
- Dialogue/Conversations/Physical Conversations: world-space or interactable-triggered conversations.
- Dialogue/SpeakerPresets: shared speaker styling for conversations.
- Dialogue/VoiceProfiles: dialogue beep and voice profiles.
- Dialogue/Cassettes: cassette definitions used by the shelf and cassette player.
- Dialogue/RadioEvents: radio event assets referenced directly by the radio manager.
