﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu.Constants
open System
open System.Numerics
open System.Configuration
open Nu
open Prime

[<RequireQualifiedAccess>]
module Engine =

    let [<Literal>] ExitCodeSuccess = 0
    let [<Literal>] ExitCodeFailure = 1
    let [<Literal>] LohSize = 85000 // the approximate size of the .NET Large-Object Heap in bytes, although this seems misleading for some uses...
    let [<Literal>] Meter2d = 48.0f
    let [<Literal>] Meter3d = 1.0f
    let [<Uniform>] GravityDefault = Vector3 (0.0f, -9.80665f, 0.0f)
    let [<Literal>] GameSortPriority = Single.MaxValue
    let [<Uniform>] ScreenSortPriority = GameSortPriority - 1.0f
    let [<Uniform>] GroupSortPriority = ScreenSortPriority - 1.0f
    let [<Uniform>] EntitySortPriority = GroupSortPriority - 1.0f
    let [<Literal>] NamePropertyName = "Name"
    let [<Literal>] DispatcherPropertyName = "Dispatcher"
    let [<Literal>] PropertiesPropertyName = "Properties"
    let [<Literal>] FacetNamesPropertyName = "FacetNames"
    let [<Literal>] RequiredDispatcherPropertyName = "RequiredDispatcher"
    let [<Literal>] FacetsPropertyName = "Facets"
    let [<Literal>] XtensionPropertyName = "Xtension"
    let [<Literal>] TransformPropertyName = "Transform"
    let [<Literal>] ModelPropertyName = "Model"
    let [<Literal>] SurnamesPropertyName = "Surnames"
    let [<Literal>] OverlayNameOptPropertyName = "OverlayNameOpt"
    let [<Literal>] EffectNameDefault = "Effect"
    let [<Literal>] RefinementDir = "refinement"
    let [<Uniform>] EntitySize2dDefault = Vector3 (Meter2d, Meter2d, 0.0f)
    let [<Uniform>] EntitySizeGuiDefault = Vector3 (Meter2d * 4.0f, Meter2d, 0.0f)
    let [<Uniform>] EntitySize3dDefault = Vector3 1.0f
    let [<Uniform>] mutable EntityCentered2dDefault = match ConfigurationManager.AppSettings.["EntityCentered2dDefault"] with null -> true | centered -> scvalue<bool> centered
    let [<Uniform>] mutable EntityCenteredGuiDefault = match ConfigurationManager.AppSettings.["EntityCenteredGuiDefault"] with null -> true | centered -> scvalue<bool> centered
    let [<Uniform>] mutable EntityCentered3dDefault = match ConfigurationManager.AppSettings.["EntityCentered3dDefault"] with null -> true | centered -> scvalue<bool> centered
    let [<Uniform>] ParticleSize2dDefault = Vector3 (12.0f, 12.0f, 0.0f)
    let [<Uniform>] ParticleSize3dDefault = Vector3 (0.01f, 0.01f, 0.01f)
    let [<Uniform>] EyeCenter3dDefault = Vector3 (0.0f, 1.0f, 4.0f)
    let [<Uniform>] EyeCenter3dOffset = Vector3 (0.0f, 0.0f, 4.0f)
    let [<Uniform>] QuadtreeGranularity = 3
    let [<Uniform>] QuadtreeDepth = 4
    let [<Uniform>] QuadtreeSize = Vector2 (single (Math.Pow (2.0, 18.0)))
    let [<Uniform>] QuadtreeBounds = Box2 (-QuadtreeSize * 0.5f, QuadtreeSize)
    let [<Uniform>] OctreeGranularity = 3
    let [<Uniform>] OctreeDepth = 4
    let [<Uniform>] OctreeSize = Vector3 (single (Math.Pow (2.0, 10.0)))
    let [<Uniform>] OctreeBounds = Box3 (-OctreeSize * 0.5f, OctreeSize)
    let [<Uniform>] mutable EventTracing = match ConfigurationManager.AppSettings.["EventTracing"] with null -> false | tracing -> scvalue<bool> tracing
    let [<Uniform>] mutable EventFilter = match ConfigurationManager.AppSettings.["EventFilter"] with null -> EventFilter.Empty | filter -> scvalue<EventFilter.Filter> filter

[<RequireQualifiedAccess>]
module Associations =

    let [<Literal>] Symbol = "Symbol"
    let [<Literal>] Render2d = "Render2d"
    let [<Literal>] Render3d = "Render3d"
    let [<Literal>] Audio = "Audio"

[<RequireQualifiedAccess>]
module Render =

    let [<Literal>] VirtualResolutionX = 960
    let [<Literal>] VirtualResolutionY = 540
    let [<Uniform>] VirtualResolution = Vector2i (VirtualResolutionX, VirtualResolutionY)
    let [<Uniform>] VirtualResolutionF = Vector2 (single VirtualResolutionX, single VirtualResolutionY)
    let [<Uniform>] VirtualScalar = match ConfigurationManager.AppSettings.["VirtualScalar"] with null -> 2 | scalar -> scvalue<int> scalar
    let [<Uniform>] VirtualScalarF = single VirtualScalar
    let [<Uniform>] VirtualScalar2i = Vector2i VirtualScalar
    let [<Uniform>] VirtualScalar2 = Vector2 (single VirtualScalar2i.X, single VirtualScalar2i.Y)
    let [<Uniform>] ResolutionX = VirtualResolutionX * VirtualScalar
    let [<Uniform>] ResolutionY = VirtualResolutionY * VirtualScalar
    let [<Uniform>] ResolutionF = Vector2 (single ResolutionX, single ResolutionY)
    let [<Uniform>] Resolution = Vector2i (ResolutionX, ResolutionY)
    let [<Uniform>] FieldOfView = single (Math.PI / 3.0) // 60 degrees
    let [<Literal>] NearPlaneDistanceEnclosed = 0.1f
    let [<Literal>] FarPlaneDistanceEnclosed = 200.0f
    let [<Literal>] NearPlaneDistanceExposed = FarPlaneDistanceEnclosed
    let [<Literal>] FarPlaneDistanceExposed = 400.0f
    let [<Literal>] NearPlaneDistanceImposter = FarPlaneDistanceExposed
    let [<Literal>] FarPlaneDistanceImposter = 1600.0f
    let [<Literal>] NearPlaneDistanceOmnipresent = NearPlaneDistanceEnclosed
    let [<Literal>] FarPlaneDistanceOmnipresent = FarPlaneDistanceImposter
    let [<Uniform>] ViewportMargin (windowSize : Vector2i) = let size = Vector2i (ResolutionX, ResolutionY) in Vector2i ((windowSize.X - size.X) / 2, (windowSize.Y - size.Y) / 2)
    let [<Uniform>] ViewportOffset (windowSize : Vector2i) = Viewport (NearPlaneDistanceOmnipresent, FarPlaneDistanceOmnipresent, Box2i(ViewportMargin windowSize, Resolution))
    let [<Uniform>] Viewport = Viewport (NearPlaneDistanceOmnipresent, FarPlaneDistanceOmnipresent, Box2i(v2iZero, Resolution))
    let [<Uniform>] PlayBoxSize3d = Vector3 200.0f
    let [<Uniform>] LightBoxSize3d = Vector3 400.0f
    let [<Uniform>] WindowClearColor = Color.White // NOTE: do not change this color as the deferred2 renderer checks if normal color is equal to [1;1;1] to discard fragment.
    let [<Literal>] OpenGlVersionMajor = 4
    let [<Literal>] OpenGlVersionMinor = 1
    let [<Literal>] OpenGlCore = true
    let [<Uniform>] GlslVersionPragma = "#version " + string OpenGlVersionMajor + string OpenGlVersionMinor + "0 " + if OpenGlCore then "core" else ""
    let [<Literal>] TextureAnisotropyMax = 8.0f
    let [<Literal>] SpriteBatchSize = 192
    let [<Literal>] SpriteBorderTexelScalar = 0.01f
    let [<Literal>] SpriteMessagesPrealloc = 256
    let [<Literal>] StaticModelMessagesPrealloc = 256
    let [<Literal>] GeometryBatchPrealloc = 1024
    let [<Literal>] ShaderLightsMax = 8
    let [<Literal>] IrradianceMapResolution = 32
    let [<Literal>] EnvironmentFilterResolution = 128
    let [<Literal>] EnvironmentFilterMips = 5 // NOTE: changing this requires changing the REFLECTION_LOD_MAX constants in shader code.

[<RequireQualifiedAccess>]
module Assimp =

    let [<Literal>] PostProcessSteps = Assimp.PostProcessSteps.Triangulate ||| Assimp.PostProcessSteps.GlobalScale

[<RequireQualifiedAccess>]
module Audio =

    let [<Literal>] SongVolumeDefault = 0.5f
    let [<Literal>] SoundVolumeDefault = 1.0f
    let [<Uniform>] FadeOutTimeDefault = GameTime.ofSeconds 0.5f
    let [<Uniform>] SongResumptionMaximum = GameTime.ofSeconds 90.0f // HACK: prevents songs from starting over too often due to hack in SdlAudioPlayer.playSong.
    let [<Literal>] Frequency = 44100
    let [<Literal>] BufferSizeDefault = 1024
    let [<Literal>] FadeInSecondsMinimum = 0.1f // NOTE: Mix_PlayMusic seems to sometimes cause audio 'popping' when starting a song, so a minimum fade is used instead.

[<RequireQualifiedAccess>]
module Physics =

    let [<Literal>] PhysicsToPixelRatio = Engine.Meter2d // 48 pixels = 1 meter
    let [<Uniform>] PixelToPhysicsRatio = 1.0f / Engine.Meter2d
    let [<Literal>] DensityDefault = 1.0f

[<RequireQualifiedAccess>]
module TileMap =

    let [<Literal>] AnimationPropertyName = "A"
    let [<Literal>] CollisionPropertyName = "C"
    let [<Literal>] ElevationPropertyName = "E"
    let [<Literal>] InfoPropertyName = "I"

[<RequireQualifiedAccess>]
module Particles =

    let [<Literal>] RestitutionDefault = 0.9f

[<RequireQualifiedAccess>]
module Effects =

    let [<Literal>] EffectHistoryMaxDefault = 60 // 1 second of effect history @ 60 fps

[<RequireQualifiedAccess>]
module Ecs =

    let [<Literal>] ArrayReserve = 256 // just large enough to amortize cache misses
    let [<Literal>] ParallelTaskSizeMinimum = 1024
    let [<Literal>] PreallocateAmount = ArrayReserve
    let [<Literal>] IntraComponentPrefix = "@"
    let [<Literal>] UnscheduledEventSuffix = "!U"
    let [<Literal>] ScheduledEventSuffix = "!S"

[<RequireQualifiedAccess>]
module Paths =

    let [<Literal>] SkyBoxShaderFilePath = "Assets/Default/SkyBox.glsl"
    let [<Literal>] IrradianceShaderFilePath = "Assets/Default/Irradiance.glsl"
    let [<Literal>] EnvironmentFilterShaderFilePath = "Assets/Default/EnvironmentFilter.glsl"
    let [<Literal>] PhysicallyBasedDeferredShaderFilePath = "Assets/Default/PhysicallyBasedDeferred.glsl"
    let [<Literal>] PhysicallyBasedDeferred2ShaderFilePath = "Assets/Default/PhysicallyBasedDeferred2.glsl"
    let [<Literal>] PhysicallyBasedForwardShaderFilePath = "Assets/Default/PhysicallyBasedForward.glsl"
    let [<Literal>] BrdfTextureFilePath = "Assets/Default/Brdf.png"