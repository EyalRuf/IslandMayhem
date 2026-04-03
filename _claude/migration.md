# Island Mayhem — Unity 6.4 Migration

## Status
✅ Project compiles clean in Unity 6000.4.1f1

## Environment
- Target Unity version: 6000.4.1f1
- Previous Unity version: Unknown (check git history)
- Platform: Windows

## Fixes Applied

| Date | Change | File(s) | Reason |
|------|--------|---------|--------|
| 2026-04-03 | Added `message = default` to `ReceiveNextReliable` failure path | `Mirror/Runtime/Transport/KCP/kcp2k/highlevel/KcpConnection.cs` | CS0177 — `out` param must be assigned before return, stricter in C# 10+ |
| 2026-04-03 | Removed `[SerializeField]` from 6 `static` property declarations | `HBAO/Runtime/HBAO.cs` | CS0592 — `[SerializeField]` not valid on static properties |
| 2026-04-03 | Wrapped `OnCameraSetup`, `Configure`, `Execute` + Blit helper methods in `#if !UNITY_6000_0_OR_NEWER` | `HBAO/SRP/URP/Runtime/HBAORendererFeature.cs` | CS0115 — `RenderingData`-based overrides removed from URP 17 `ScriptableRenderPass` |
| 2026-04-03 | Removed `hasAdvancedMode` override | `HBAO/SRP/URP/Editor/HBAOEditor.cs` | CS0115 — `VolumeComponentEditor.hasAdvancedMode` removed in URP 17 |
| 2026-04-03 | Stubbed `FlatKitFog` class for Unity 6 via `#if !UNITY_6000_0_OR_NEWER` | `FlatKit/RenderFeatures/Fog/FlatKitFog.cs` | CS0619 — `RenderTargetHandle` removed in URP 2023.1+ |
| 2026-04-03 | Stubbed `FlatKitDepthNormals` class for Unity 6 | `FlatKit/RenderFeatures/Outline/FlatKitDepthNormals.cs` | CS0619 — same |
| 2026-04-03 | Stubbed `FlatKitOutline` class for Unity 6 | `FlatKit/RenderFeatures/Outline/FlatKitOutline.cs` | CS0619 + CS0115 — same + old Execute/Configure overrides |

## Pending / Future Migration Work

| Item | File(s) | Priority | Notes |
|------|---------|----------|-------|
| Port HBAO to URP 17 Render Graph API | `HBAORendererFeature.cs` | Medium | Replace `Execute`/`Configure` with `RecordRenderGraph`. HBAO visually disabled on Unity 6 until done |
| Port FlatKit to URP 17 RTHandle API | `FlatKitFog.cs`, `FlatKitOutline.cs`, `FlatKitDepthNormals.cs` | Medium | Replace `RenderTargetHandle` with `RTHandle`. All FlatKit features visually disabled |
| Replace `OnLevelWasLoaded` | `CustomNetworkManager.cs`, `TTTMatchManager.cs` | High | `OnLevelWasLoaded` is Unity 4-era API, unreliable in Unity 6. Replace with `SceneManager.sceneLoaded` |

## Package Versions

| Package | Version | Notes |
|---------|---------|-------|
| Mirror | Unknown — vendored in `Assets/ExternalAssets/Mirror/` | Not a UPM package. No version tag visible. |
| Steamworks.NET / Steam transport | Unknown — vendored in ExternalAssets | Same issue |
| URP | 17.4.0 | `com.unity.render-pipelines.universal` |
| Unity AI Navigation | 2.0.11 | NavMesh |
| Unity UGUI | 2.0.0 | Legacy UI |
| Unity Timeline | 1.8.11 | |
