﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace OmniBlade
open System
open System.Numerics
open System.IO
open TiledSharp
open Prime
open Nu
open OmniBlade

type Direction =
    | Upward
    | Rightward
    | Downward
    | Leftward

    member this.Opposite =
        match this with
        | Upward -> Downward
        | Rightward -> Leftward
        | Downward -> Upward
        | Leftward -> Rightward

    static member ofVector3 (v3 : Vector3) =
        let angle = double (atan2 v3.Y v3.X)
        let angle = if angle < 0.0 then angle + Math.PI * 2.0 else angle
        let direction =
            if      angle > Math.PI * 1.75 || angle <= Math.PI * 0.25 then  Rightward
            elif    angle > Math.PI * 0.75 && angle <= Math.PI * 1.25 then  Leftward
            elif    angle > Math.PI * 0.25 && angle <= Math.PI * 0.75 then  Upward
            else                                                            Downward
        direction

    static member ofVector3Biased (v3 : Vector3) =
        let angle = double (atan2 v3.Y v3.X)
        let angle = if angle < 0.0 then angle + Math.PI * 2.0 else angle
        let direction =
            if      angle > Math.PI * 1.74997 || angle <= Math.PI * 0.25003 then    Rightward
            elif    angle > Math.PI * 0.74997 && angle <= Math.PI * 1.25003 then    Leftward
            elif    angle > Math.PI * 0.25 && angle <= Math.PI * 0.75 then          Upward
            else                                                                    Downward
        direction

    static member toVector3 direction =
        match direction with
        | Upward -> v3Up
        | Rightward -> v3Right
        | Downward -> v3Down
        | Leftward -> v3Left

type CharacterIndex =
    | AllyIndex of int
    | EnemyIndex of int

    member this.Ally =
        match this with
        | AllyIndex _ -> true
        | EnemyIndex _ -> false

    member this.Enemy =
        not this.Ally

    member this.Subindex =
        match this with
        | AllyIndex index
        | EnemyIndex index -> index

    static member friendly index index2 =
        match (index, index2) with
        | (AllyIndex _, AllyIndex _) -> true
        | (EnemyIndex _, EnemyIndex _) -> true
        | (_, _) -> false

    static member unfriendly index index2 =
        not (CharacterIndex.friendly index index2)

    static member toEntityName index =
        match index with
        | AllyIndex i -> "Ally+" + scstring i
        | EnemyIndex i -> "Enemy+" + scstring i

type EffectType =
    | Physical
    | Magical

type AffinityType =
    | Fire
    | Ice
    | Lightning
    | Water
    | Wind // weak to self
    | Metal // vulnerable to shadow
    | Earth // not resistant to self
    | Light // not resistant to self
    | Shadow // not resistant to self

    static member getScalar source target =
        match (source, target) with

        // self-resist
        | (Fire, Fire) -> Constants.Battle.AffinityResistanceScalar
        | (Ice, Ice) -> Constants.Battle.AffinityResistanceScalar
        | (Lightning, Lightning) -> Constants.Battle.AffinityResistanceScalar
        | (Water, Water) -> Constants.Battle.AffinityResistanceScalar
        | (Metal, Metal) -> Constants.Battle.AffinityResistanceScalar

        // self-vulnerable
        | (Wind, Wind) -> Constants.Battle.AffinityVulnerabilityScalar

        // vulnerable
        | (Fire, Ice) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Fire, Earth) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Ice, Fire) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Lightning, Water) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Lightning, Metal) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Water, Lightning) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Light, Shadow) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Shadow, Light) -> Constants.Battle.AffinityVulnerabilityScalar
        | (Shadow, Metal) -> Constants.Battle.AffinityVulnerabilityScalar

        // neutral
        | (_, _) -> 1.0f

type [<CustomEquality; CustomComparison>] StatusType =
    | Poison
    | Silence // TODO: implement effect in battle.
    | Sleep // TODO: implement effect in battle.
    | Confuse // TODO: implement effect in battle (disallows enemy use of techs (except charge) and same for player but randomizes attack targets, too).
    | Curse // TODO: implement effect of 'can't gain HP' in battle.
    //| Blind - maybe in the sequel
    | Time of bool // true = Haste, false = Slow
    | Power of bool * bool // true = Up, false = Down; true = 2, false = 1
    | Magic of bool * bool // true = Up, false = Down; true = 2, false = 1
    | Shield of bool * bool // true = Up, false = Down; true = 2, false = 1
    //| Counter of bool * bool // true = Up, false = Down; true = 2, false = 1 - maybe in the sequel
    //| Provoke of CharacterIndex - maybe in the sequel

    static member randomizeWeak this =
        match this with
        | Poison -> Gen.random1 2 = 0
        | Silence -> Gen.random1 3 = 0
        | Sleep -> Gen.random1 4 = 0
        | Confuse -> Gen.random1 3 = 0
        | Curse -> Gen.random1 2 = 0
        | Time false | Power (false, _) | Magic (false, _) | Shield (false, _) -> Gen.random1 2 = 0
        | Time true | Power (true, _) | Magic (true, _) | Shield (true, _) -> true

    static member randomizeStrong this =
        match this with
        | Poison -> Gen.random1 5 <> 0
        | Silence -> Gen.random1 2 = 0
        | Sleep -> Gen.random1 3 = 0
        | Confuse -> Gen.random1 2 = 0
        | Curse -> Gen.random1 5 <> 0
        | Time false | Power (false, _) | Magic (false, _) | Shield (false, _) -> Gen.random1 5 <> 0
        | Time true | Power (true, _) | Magic (true, _) | Shield (true, _) -> true

    static member enumerate this =
        match this with
        | Poison -> 0
        | Silence -> 1
        | Sleep -> 2
        | Confuse -> 3
        | Curse -> 4
        | Time _ -> 5
        | Power (_, _) -> 6
        | Magic (_, _) -> 7
        | Shield (_, _) -> 8

    static member compare this that =
        compare
            (StatusType.enumerate this)
            (StatusType.enumerate that)

    interface StatusType IComparable with
        member this.CompareTo that =
            StatusType.compare this that

    interface IComparable with
        member this.CompareTo that =
            match that with
            | :? StatusType as that -> (this :> StatusType IComparable).CompareTo that
            | _ -> failwithumf ()

    override this.Equals that =
        match that with
        | :? StatusType as that -> StatusType.enumerate this = StatusType.enumerate that
        | _ -> false

    override this.GetHashCode () =
        StatusType.enumerate this

type AimType =
    | EnemyAim of bool // healthy (N/A)
    | AllyAim of bool // healthy
    | AnyAim of bool // healthy
    | NoAim

type TargetType =
    | SingleTarget of AimType
    | ProximityTarget of single * AimType
    | RadialTarget of single * AimType
    | LineTarget of single * AimType
    | SegmentTarget of single * AimType
    | VerticalTarget of single * AimType
    | HorizontalTarget of single * AimType
    | AllTarget of AimType

    static member getAimType targetType =
        match targetType with
        | SingleTarget aimType -> aimType
        | ProximityTarget (_, aimType) -> aimType
        | RadialTarget (_, aimType) -> aimType
        | LineTarget (_, aimType) -> aimType
        | SegmentTarget (_, aimType) -> aimType
        | VerticalTarget (_, aimType) -> aimType
        | HorizontalTarget (_, aimType) -> aimType
        | AllTarget aimType -> aimType

type TechType =
    | Critical
    | Slash
    | HeavyCritical
    | Cyclone
    | PoisonCut
    | PowerCut
    | DispelCut
    | DoubleCut
    | Fire
    | Flame
    | Ice
    | Snowball
    | Stone
    | Quake
    | Cure
    | Empower
    | Aura
    | Enlighten
    | Protect
    | Weaken
    | Muddle
    | Slow
    | Bolt
    | ConjureIfrit
    | Purify
    member this.ConjureTech =
        match this with
        | ConjureIfrit -> true
        | _ -> false

type ActionType =
    | Attack
    | Defend
    | Consume of ConsumableType
    | Tech of TechType
    | Wound

type StatureType =
    | SmallStature
    | NormalStature
    | LargeStature
    | BossStature

type ArchetypeType =
    | Apprentice
    | Fighter
    | Ninja
    | Wizard
    | Conjuror
    | Cleric
    | Bat
    | Snake
    | Gorgon
    | Goblin
    | Soldier
    | Shaman
    | Trixter
    | Avian
    | Minotaur
    | Armoros
    | Arachnos

type ShopType =
    | Chemist
    | Armory

type ShopkeepAppearanceType =
    | Male
    | Female
    | Fancy

type FieldType =
    | EmptyField
    | DebugField
    | TombOuter
    | TombGround
    | TombBasement
    | Castle of int
    | CastleConnector

    member this.Connector =
        match this with
        | EmptyField
        | DebugField
        | TombOuter
        | TombGround
        | TombBasement
        | Castle _ -> false
        | CastleConnector -> true

    static member toFieldName (fieldType : FieldType) =
        match valueToSymbol fieldType with
        | Symbol.Atom (name, _) -> name
        | Symbols ([Symbol.Atom (name , _); _], _) -> name
        | _ -> failwithumf ()

type BattleType =
    | EmptyBattle
    | DebugBattle
    | CastleBattle
    | CastleBattle2
    | CastleBattle3
    | CastleBattle4
    | CastleBattle5
    | CastleBattle6
    | CastleBattle7
    | CastleBattle8
    | CastleBattle9
    | MadTrixterBattle
    | Castle2Battle
    | Castle2Battle2
    | Castle2Battle3
    | Castle2Battle4
    | Castle2Battle5
    | Castle2Battle6
    | Castle2Battle7
    | Castle2Battle8
    | Castle2Battle9
    | HeavyArmorosBattle
    | Castle3Battle
    | Castle3Battle2
    | Castle3Battle3
    | Castle3Battle4
    | Castle3Battle5
    | Castle3Battle6
    | Castle3Battle7
    | Castle3Battle8
    | Castle3Battle9
    | AraneaImplicitumBattle

type EncounterType =
    | DebugEncounter
    | CastleEncounter
    | Castle2Encounter
    | Castle3Encounter

type LockType =
    | BrassKey

type ChestType =
    | WoodenChest
    | SteelChest
    | BrassChest

type DoorType =
    | WoodenDoor
    | SteelDoor

type PortalIndex =
    | Center
    | North
    | East
    | South
    | West
    | NE
    | SE
    | NW
    | SW
    | Warp
    | IX of int

type PortalType =
    | AirPortal
    | WarpPortal
    | StairsPortal of bool * bool

type NpcType =
    | ShadeNpc
    | MaelNpc
    | RiainNpc
    | PericNpc
    | RavelNpc
    | AdvenNpc
    | EildaenNpc
    | NostrusNpc
    | MadTrixterNpc
    | HeavyArmorosNpc
    | AraneaImplicitumNpc
    
    static member exists advents specialty =
        match specialty with
        | ShadeNpc -> not (Set.contains ShadeRecruited advents)
        | MaelNpc -> not (Set.contains MaelRecruited advents)
        | RiainNpc -> not (Set.contains RiainRecruited advents)
        | PericNpc -> not (Set.contains PericRecruited advents)
        | MadTrixterNpc -> not (Set.contains MadTrixterDefeated advents)
        | HeavyArmorosNpc -> not (Set.contains HeavyArmorosDefeated advents)
        | AraneaImplicitumNpc -> not (Set.contains AraneaImplicitumDefeated advents)
        | RavelNpc | AdvenNpc | EildaenNpc | NostrusNpc -> true

type ShopkeepType =
    | RobehnShopkeep
    | SchaalShopkeep

type FlameType =
    | FatFlame
    | SkinnyFlame
    | SmallFlame
    | LargeFlame

type SwitchType =
    | ThrowSwitch
    
type SensorType =
    | AirSensor
    | HiddenSensor
    | StepPlateSensor

type PoiseType =
    | Poising
    | Defending
    | Charging

type AnimationType =
    | LoopedWithDirection
    | LoopedWithoutDirection
    | SaturatedWithDirection
    | SaturatedWithoutDirection

type CharacterAnimationType =
    | WalkAnimation
    | CelebrateAnimation
    | ReadyAnimation
    | PoiseAnimation of PoiseType
    | AttackAnimation
    | WoundAnimation
    | SpinAnimation
    | DamageAnimation
    | IdleAnimation
    | CastAnimation
    | Cast2Animation
    | SlashAnimation
    | WhirlAnimation
    | UnearthAnimation

type AllyType =
    | Jinn
    | Shade
    | Mael
    | Riain
    | Peric

type EnemyType =
    | DebugGoblin
    | DarkBat
    | BlueGoblin
    | MadMinotaur
    | MadTrixter
    | LowerGorgon
    | FacelessSoldier
    | Hawk
    | HeavyArmoros
    | PitViper
    | Cloak
    | BloodArmoros
    | AraneaImplicitum
    | Kyla

type CharacterType =
    | Ally of AllyType
    | Enemy of EnemyType

    static member getName characterType =
        match characterType with
        | Ally ty -> string ty
        | Enemy ty -> string ty

type SpawnEffectType =
    | Materialize
    | Unearth

type SpawnType =
    { EnemyType : EnemyType
      SpawnEffectType : SpawnEffectType }

type SpiritType =
    | WeakSpirit
    | NormalSpirit
    | StrongSpirit

    static member getColor spiritType =
        match spiritType with
        | WeakSpirit -> Color (byte 255, byte 255, byte 255, byte 127)
        | NormalSpirit -> Color (byte 255, byte 191, byte 191, byte 127)
        | StrongSpirit -> Color (byte 255, byte 127, byte 127, byte 127)

type CueTarget =
    | AvatarTarget // (field only)
    | CharacterTarget of CharacterType // (field only)
    | NpcTarget of NpcType // (field only)
    | ShopkeepTarget of ShopkeepType // (field only)
    | CharacterIndexTarget of CharacterIndex // (battle only)
    | SpriteTarget of string

type CuePredicate =
    | Gold of int
    | Item of ItemType
    | Items of ItemType list
    | Advent of Advent
    | Advents of Advent Set

type CueWait =
    | Wait
    | Timed of int64
    | NoWait

type MoveType =
    | Walk
    | Run
    | Mosey
    | Instant

    member this.MoveSpeedOpt =
        match this with
        | Walk -> Some Constants.Gameplay.CueWalkSpeed
        | Run -> Some Constants.Gameplay.CueRunSpeed
        | Mosey -> Some Constants.Gameplay.CueMoseySpeed
        | Instant -> None

    static member computeStepAndStepCount (translation : Vector3) (moveType : MoveType) =
        match moveType.MoveSpeedOpt with
        | Some moveSpeed ->
            let stepCount = translation.Magnitude / moveSpeed
            let step = translation / stepCount
            (step, int (ceil stepCount))
        | None -> (translation, 1)

[<Syntax
    ("Gold Item Items Advent Advents " +
     "Wait Timed NoWait " +
     "Fin PlaySound PlaySong FadeOutSong Face Glow Recruit Unseal " +
     "AddItem RemoveItem AddAdvent RemoveAdvent " +
     "Wait Animate Fade Warp Battle Dialog Prompt " +
     "If Not Define Assign Parallel Sequence",
     "", "", "", "",
     Constants.PrettyPrinter.DefaultThresholdMin,
     Constants.PrettyPrinter.DetailedThresholdMax)>]
type Cue =
    | Fin
    | PlaySound of single * Sound AssetTag
    | PlaySong of int64 * int64 * int64 * single * Song AssetTag
    | FadeOutSong of int64
    | Face of CueTarget * Direction
    | ClearSpirits
    | Recruit of AllyType
    | AddItem of ItemType
    | RemoveItem of ItemType
    | AddAdvent of Advent
    | RemoveAdvent of Advent
    | ReplaceAdvent of Advent * Advent
    | Wait of int64
    | WaitState of int64
    | Fade of CueTarget * int64 * bool
    | FadeState of int64 * CueTarget * int64 * bool
    | Animate of CueTarget * CharacterAnimationType * CueWait
    | AnimateState of int64 * CueWait
    | Move of CueTarget * Vector3 * MoveType
    | MoveState of int64 * CueTarget * Vector3 * Vector3 * MoveType
    | Warp of FieldType * Vector3 * Direction
    | WarpState
    | Battle of BattleType * Advent Set // TODO: P1: consider using three Cues (start, end, post) in battle rather than advents directly...
    | BattleState
    | Dialog of string * bool
    | DialogState
    | Prompt of string * (string * Cue) * (string * Cue)
    | PromptState
    | If of CuePredicate * Cue * Cue
    | Not of CuePredicate * Cue * Cue
    | Define of string * Cue
    | Assign of string * Cue
    | Expand of string
    | Parallel of Cue list
    | Sequence of Cue list
    static member isFin cue = match cue with Fin -> true | _ -> false
    static member notFin cue = match cue with Fin -> false | _ -> true
    static member isInterrupting (inventory : Inventory) (advents : Advent Set) cue =
        match cue with
        | Fin | PlaySound _ | PlaySong _ | FadeOutSong _ | Face _ | ClearSpirits | Recruit _ -> false
        | AddItem _ | RemoveItem _ | AddAdvent _ | RemoveAdvent _ | ReplaceAdvent _ -> false
        | Wait _ | WaitState _ | Fade _ | FadeState _ | Move _ | MoveState _ | Warp _ | WarpState _ | Battle _ | BattleState _ | Dialog _ | DialogState _ | Prompt _ | PromptState _ -> true
        | Animate (_, _, wait) | AnimateState (_, wait) -> match wait with Timed 0L | NoWait -> false | _ -> true
        | If (p, c, a) ->
            match p with
            | Gold gold -> if inventory.Gold >= gold then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Item itemType -> if Inventory.containsItem itemType inventory then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Items itemTypes -> if Inventory.containsItems itemTypes inventory then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Advent advent -> if advents.Contains advent then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Advents advents2 -> if advents.IsSupersetOf advents2 then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
        | Not (p, c, a) ->
            match p with
            | Gold gold -> if inventory.Gold < gold then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Item itemType -> if not (Inventory.containsItem itemType inventory) then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Items itemTypes -> if not (Inventory.containsItems itemTypes inventory) then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Advent advent -> if not (advents.Contains advent) then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
            | Advents advents2 -> if not (advents.IsSupersetOf advents2) then Cue.isInterrupting inventory advents c else Cue.isInterrupting inventory advents a
        | Define (_, _) | Assign (_, _) -> false
        | Expand _ -> true // NOTE: we just assume this expands into something interrupting to be safe.
        | Parallel cues -> List.exists (Cue.isInterrupting inventory advents) cues
        | Sequence cues -> List.exists (Cue.isInterrupting inventory advents) cues
    static member notInterrupting inventory advents cue = not (Cue.isInterrupting inventory advents cue)

type CueDefinitions =
    Map<string, Cue>

type Branch =
    { Cue : Cue
      Requirements : Advent Set }

[<RequireQualifiedAccess>]
module OmniSeedState =

    type [<SymbolicExpansion>] OmniSeedState =
        private
            { RandSeedState : uint64 }

    let rotate fieldType state =
        match fieldType with
        | EmptyField | DebugField | TombOuter | TombGround | TombBasement
        | CastleConnector -> state.RandSeedState
        | Castle n -> state.RandSeedState <<< n + 0 // increment constant by m for each level to ensure unique seed

    let makeFromSeedState randSeedState =
        { RandSeedState = randSeedState }

    let make () =
        { RandSeedState = Rand.DefaultSeedState }

type OmniSeedState = OmniSeedState.OmniSeedState

type WeaponData =
    { WeaponType : WeaponType // key
      WeaponSubtype : WeaponSubtype
      PowerBase : int
      MagicBase : int
      Cost : int
      Description : string }

type ArmorData =
    { ArmorType : ArmorType // key
      ArmorSubtype : ArmorSubtype
      EnduranceBase : int
      MindBase : int
      Cost : int
      Description : string }
    member this.EnduranceBaseDisplay = this.EnduranceBase / Constants.Gameplay.ArmorStatBaseDisplayDivisor
    member this.MindBaseDisplay = this.MindBase / Constants.Gameplay.ArmorStatBaseDisplayDivisor

type AccessoryData =
    { AccessoryType : AccessoryType // key
      ShieldBase : int
      CounterBase : int
      AffinityOpt : AffinityType option
      Immunities : StatusType Set
      Enchantments : StatusType Set
      Cost : int
      Description : string }

type ConsumableData =
    { ConsumableType : ConsumableType // key
      Scalar : single
      Curative : bool
      Techative : bool
      Revive : bool
      StatusesAdded : StatusType Set
      StatusesRemoved : StatusType Set
      AimType : AimType
      Cost : int
      Description : string }

type TechData =
    { TechType : TechType // key
      TechCost : int
      EffectType : EffectType
      Scalar : single
      Split : bool
      Curative : bool
      Cancels : bool
      Absorb : single // percentage of outcome that is absorbed by the caster
      AffinityOpt : AffinityType option
      StatusesAdded : StatusType Set
      StatusesRemoved : StatusType Set
      SpawnOpt : SpawnType list option
      TargetType : TargetType
      Description : string }

    member this.AimType =
        TargetType.getAimType this.TargetType

type ArchetypeData =
    { ArchetypeType : ArchetypeType // key
      Stamina : single // hit points scalar
      Strength : single // power scalar
      Intelligence : single // magic scalar
      Defense : single // defense scalar
      Absorb : single // absorb scalar
      Focus : single // tech points scalar
      Wealth : single // gold scalar
      Mythos : single // exp scalar
      WeaponSubtype : WeaponSubtype
      ArmorSubtype : ArmorSubtype
      Techs : Map<int, TechType> // tech availability according to level
      ChargeTechs : (int * int * TechType) list
      AffinityOpt : AffinityType option
      Immunities : StatusType Set
      Stature : StatureType
      Description : string }

type TechAnimationData =
    { TechType : TechType // key
      TechStart : int64
      TechingStart : int64
      AffectingStart : int64
      AffectingStop : int64
      TechingStop : int64
      TechStop : int64 }

type KeyItemData =
    { KeyItemData : unit }

type DoorData =
    { DoorType : DoorType // key
      DoorKeyOpt : string option
      OpenImage : Image AssetTag
      ClosedImage : Image AssetTag }

type ShopData =
    { ShopType : ShopType // key
      ShopItems : ItemType list }

type EnemyDescriptor =
    { EnemyType : EnemyType
      EnemyPosition : Vector3 }

type BattleData =
    { BattleType : BattleType // key
      BattleAllyPositions : Vector3 list
      BattleEnemies : EnemyType list
      BattleTileMap : TileMap AssetTag
      BattleTileIndexOffset : int
      BattleTileIndexOffsetRange : int * int
      BattleSongOpt : Song AssetTag option }

type EncounterData =
    { EncounterType : EncounterType // key
      BattleTypes : BattleType list }

type CharacterData =
    { CharacterType : CharacterType // key
      ArchetypeType : ArchetypeType
      LevelBase : int
      AbsorbCreep : single
      Boss : bool
      AnimationSheet : Image AssetTag
      PortraitOpt : Image AssetTag option
      WeaponOpt : WeaponType option
      ArmorOpt : ArmorType option
      Accessories : AccessoryType list
      TechProbabilityOpt : single option
      GoldScalar : single
      ExpScalar : single
      Description : string }

type CharacterAnimationData =
    { CharacterAnimationType : CharacterAnimationType // key
      AnimationType : AnimationType
      LengthOpt : int64 option
      Run : int
      Delay : int64
      Offset : Vector2i }

type PropData =
    | Sprite of string * Image AssetTag * Color * Blend * Color * Flip * bool
    | Portal of PortalType * PortalIndex * Direction * FieldType * PortalIndex * bool * Advent Set // leads to a different portal
    | Door of DoorType * KeyItemType option * Cue * Cue * Advent Set // for simplicity, we just have north / south doors
    | Chest of ChestType * ItemType * Guid * BattleType option * Cue * Advent Set
    | Switch of SwitchType * Cue * Cue * Advent Set
    | Sensor of SensorType * BodyShape option * Cue * Cue * Advent Set
    | Character of CharacterType * Direction * bool * bool * Cue * Advent Set
    | Npc of NpcType * Direction option * Cue * Advent Set
    | NpcBranching of NpcType * Direction option * Branch list * Advent Set
    | Shopkeep of ShopkeepType * Direction option * ShopType * Advent Set
    | Seal of Color * Cue * Advent Set
    | Flame of FlameType * bool
    | SavePoint
    | ChestSpawn
    | EmptyProp

type PropDescriptor =
    { PropPerimeter : Box3
      PropElevation : single
      PropData : PropData
      PropId : int }

type FieldTileMap =
    | FieldStatic of TileMap AssetTag
    | FieldConnector of TileMap AssetTag * TileMap AssetTag
    | FieldRandom of int * int * single * Origin * int * string

type FieldData =
    { FieldType : FieldType // key
      FieldTileMap : FieldTileMap
      FieldTileIndexOffset : int
      FieldTileIndexOffsetRange : int * int
      FieldBackgroundColor : Color
      FieldDebugAdvents : Advent Set
      FieldDebugKeyItems : KeyItemType list
      FieldSongOpt : Song AssetTag option
      ShowUnopenedChests : bool
      UseWindPortal : bool
      EncounterTypeOpt : EncounterType option
      Definitions : CueDefinitions
      Treasures : ItemType list }

[<RequireQualifiedAccess>]
module FieldData =

    let mutable tileMapsMemoized = Map.empty<uint64 * FieldType, Choice<TmxMap, TmxMap * TmxMap, TmxMap * Origin>>
    let mutable propObjectsMemoized = Map.empty<uint64 * FieldType, TmxMap * (TmxObjectGroup * TmxObject) list * Origin option>
    let mutable propDescriptorsMemoized = Map.empty<uint64 * FieldType, PropDescriptor list>

    let objectToPropOpt (object : TmxObject) (group : TmxObjectGroup) (tileMap : TmxMap) =
        let propPosition = v3 (single object.X) (single tileMap.Height * single tileMap.TileHeight - single object.Y) 0.0f // invert y
        let propSize = v3 (single object.Width) (single object.Height) 0.0f
        let propPerimeter = box3 propPosition propSize
        let propElevation =
            match group.Properties.TryGetValue Constants.TileMap.ElevationPropertyName with
            | (true, elevationStr) -> Constants.Field.ForegroundElevation + scvalue elevationStr
            | (false, _) -> Constants.Field.ForegroundElevation
        match object.Properties.TryGetValue Constants.TileMap.InfoPropertyName with
        | (true, propDataStr) ->
            let propData = scvalue propDataStr
            Some { PropPerimeter = propPerimeter; PropElevation = propElevation; PropData = propData; PropId = object.Id }
        | (false, _) -> None

    let tryGetTileMap omniSeedState fieldData =
        let rotatedSeedState = OmniSeedState.rotate fieldData.FieldType omniSeedState
        let memoKey = (rotatedSeedState, fieldData.FieldType)
        match Map.tryFind memoKey tileMapsMemoized with
        | None ->
            let tileMapOpt =
                match fieldData.FieldTileMap with
                | FieldStatic fieldAsset ->
                    match Metadata.tryGetTileMapMetadata fieldAsset with
                    | Some (_, _, tileMap) -> Some (Choice1Of3 tileMap)
                    | None -> None
                | FieldConnector (fieldAsset, fieldFadeAsset) ->
                    match (Metadata.tryGetTileMapMetadata fieldAsset, Metadata.tryGetTileMapMetadata fieldFadeAsset) with
                    | (Some (_, _, tileMap), Some (_, _, tileMapFade)) -> Some (Choice2Of3 (tileMap, tileMapFade))
                    | (_, _) -> None
                | FieldRandom (walkCount, walkLength, bias, origin, floor, fieldPath) ->
                    let rand = Rand.makeFromSeedState rotatedSeedState
                    let (cursor, randMap, _) = RandMap.makeFromRand walkCount walkLength bias Constants.Field.RandMapSize origin floor rand
                    let fieldName = FieldType.toFieldName fieldData.FieldType
                    let tileMap = RandMap.toTmx fieldName fieldPath origin cursor floor fieldData.UseWindPortal randMap
                    Some (Choice3Of3 (tileMap, origin))
            match tileMapOpt with
            | Some tileMapChc -> tileMapsMemoized <- Map.add memoKey tileMapChc tileMapsMemoized
            | None -> ()
            tileMapOpt
        | tileMapOpt -> tileMapOpt

    let getPropObjects omniSeedState fieldData =
        let rotatedSeedState = OmniSeedState.rotate fieldData.FieldType omniSeedState
        let memoKey = (rotatedSeedState, fieldData.FieldType)
        match Map.tryFind memoKey propObjectsMemoized with
        | None ->
            let result =
                match tryGetTileMap omniSeedState fieldData with
                | Some tileMapChc ->
                    match tileMapChc with
                    | Choice1Of3 tileMap ->
                        if tileMap.ObjectGroups.Contains Constants.Field.PropsGroupName then
                            let group = tileMap.ObjectGroups.Item Constants.Field.PropsGroupName
                            let propObjects = enumerable<TmxObject> group.Objects |> Seq.map (fun propObject -> (group, propObject)) |> Seq.toList
                            (tileMap, propObjects, None)
                        else (tileMap, [], None)
                    | Choice2Of3 (tileMap, _) ->
                        if tileMap.ObjectGroups.Contains Constants.Field.PropsGroupName then
                            let group = tileMap.ObjectGroups.Item Constants.Field.PropsGroupName
                            let propObjects = enumerable<TmxObject> group.Objects |> Seq.map (fun propObject -> (group, propObject)) |> Seq.toList
                            (tileMap, propObjects, None)
                        else (tileMap, [], None)
                    | Choice3Of3 (tileMap, origin) ->
                        if tileMap.ObjectGroups.Contains Constants.Field.PropsGroupName then
                            let group = tileMap.ObjectGroups.Item Constants.Field.PropsGroupName
                            let propObjects = enumerable<TmxObject> group.Objects |> Seq.map (fun propObject -> (group, propObject)) |> Seq.toList
                            (tileMap, propObjects, Some origin)
                        else (tileMap, [], None)
                | None -> (TmxMap.makeDefault (), [], None)
            propObjectsMemoized <- Map.add memoKey result propObjectsMemoized
            result
        | Some result -> result

    let getPropDescriptors omniSeedState fieldData =
        let rotatedSeedState = OmniSeedState.rotate fieldData.FieldType omniSeedState
        let memoKey = (rotatedSeedState, fieldData.FieldType)
        match Map.tryFind memoKey propDescriptorsMemoized with
        | None ->
            let rand = Rand.makeFromSeedState rotatedSeedState
            let (tileMap, propObjects, originOpt) = getPropObjects omniSeedState fieldData
            let props = List.choose (fun (group, object) -> objectToPropOpt object group tileMap) propObjects
            let (chestSpawnsUnsorted, nonChestSpawns) = List.split (fun prop -> match prop.PropData with ChestSpawn -> true | _ -> false) props
            let chestSpawns =
                match originOpt with
                | Some origin ->
                    let mapSize =
                        v2 // TODO: implement TotalWidth and TotalHeight extension properties for TmxMap.
                            (single (tileMap.Width * tileMap.TileWidth))
                            (single (tileMap.Height * tileMap.TileHeight))
                    let mapPerimeter = box2 v2Zero mapSize
                    let originPosition = Origin.approximatePosition mapPerimeter origin
                    chestSpawnsUnsorted |>
                    List.map (fun chestSpawn -> (Vector2.DistanceSquared (chestSpawn.PropPerimeter.Center.V2, originPosition), chestSpawn)) |>
                    List.sortBy fst |>
                    List.map snd
                | None -> chestSpawnsUnsorted
            let treasuresRepeat = match fieldData.Treasures.Length with 0 -> 0 | treasureCount -> inc (chestSpawnsUnsorted.Length / treasureCount)
            let treasuresIndexed = fieldData.Treasures |> List.rev |> List.indexed
            let treasures =
                Seq.init treasuresRepeat (constant treasuresIndexed) |>
                Seq.concat |>
                Seq.take chestSpawns.Length |>
                Seq.sortBy fst |>
                Seq.map snd |>
                Seq.toList
            let (chestSpawneds, _) =
                List.foldBack2 (fun chestSpawn treasure (chestSpawneds, rand) ->
                    let (probability, rand) = Rand.nextSingleUnder 1.0f rand
                    if probability < Constants.Field.TreasureProbability then
                        let (id, rand) = let (i, rand) = Rand.nextInt rand in let (j, rand) = Rand.nextInt rand in (Gen.idFromInts i j, rand)
                        let chestType = match fieldData.FieldType with Castle _ -> WoodenChest | _ -> SteelChest
                        let chestSpawned = { chestSpawn with PropData = Chest (chestType, treasure, id, None, Cue.Fin, Set.empty) }
#if DEV
                        let mapSize =
                            v2 // TODO: implement TotalWidth and TotalHeight extension properties for TmxMap.
                                (single (tileMap.Width * tileMap.TileWidth))
                                (single (tileMap.Height * tileMap.TileHeight))
                        let mapPerimeter = box2 v2Zero mapSize
                        match originOpt with
                        | Some origin ->
                            let originPosition = Origin.approximatePosition mapPerimeter origin
                            let distance = Vector2.Distance (chestSpawned.PropPerimeter.Center.V2, originPosition)
                            printfn "%A:%A" distance (scstring chestSpawned.PropData)
                        | None -> ()
#endif
                        (chestSpawned :: chestSpawneds, rand)
                    else (chestSpawneds, rand))
                    chestSpawns
                    treasures
                    ([], rand)
            let propDescriptors = chestSpawneds @ nonChestSpawns
            propDescriptorsMemoized <- Map.add memoKey propDescriptors propDescriptorsMemoized
            propDescriptors
        | Some propDescriptors -> propDescriptors

    let getPortals omniSeedState fieldData =
        let propDescriptors = getPropDescriptors omniSeedState fieldData
        List.filter (fun propDescriptor -> match propDescriptor.PropData with Portal _ -> true | _ -> false) propDescriptors

    let tryGetPortal omniSeedState portalIndex fieldData =
        let portals = getPortals omniSeedState fieldData
        List.tryFind (fun prop -> match prop.PropData with Portal (_, portalIndex2, _, _, _, _, _) -> portalIndex2 = portalIndex | _ -> failwithumf ()) portals

    let tryGetSpiritType omniSeedState avatarBottom fieldData =
        match tryGetTileMap omniSeedState fieldData with
        | Some tileMapChc ->
            match tileMapChc with
            | Choice3Of3 (tileMap, origin) ->
                match fieldData.FieldTileMap with
                | FieldRandom (_, _, _, _, _, _) ->
                    let tileMapPerimeter = box3 v3Zero (v3 (single tileMap.Width * single tileMap.TileWidth) (single tileMap.Height * single tileMap.TileHeight) 0.0f)
                    let distanceFromOriginMax =
                        let walkLengthScalar =
                            match origin with
                            | OriginC -> Constants.Field.WalkLengthScalarOpened
                            | _ -> Constants.Field.WalkLengthScalarClosed
                        let delta = tileMapPerimeter.Bottom - tileMapPerimeter.Top
                        delta.Magnitude * walkLengthScalar
                    let distanceFromOrigin =
                        match origin with
                        | OriginC -> let delta = avatarBottom - tileMapPerimeter.Center in delta.Magnitude
                        | OriginN -> let delta = avatarBottom - tileMapPerimeter.Top in delta.Magnitude
                        | OriginE -> let delta = avatarBottom - tileMapPerimeter.Right in delta.Magnitude
                        | OriginS -> let delta = avatarBottom - tileMapPerimeter.Bottom in delta.Magnitude
                        | OriginW -> let delta = avatarBottom - tileMapPerimeter.Left in delta.Magnitude
                        | OriginNE -> let delta = avatarBottom - tileMapPerimeter.TopRight in delta.Magnitude
                        | OriginNW -> let delta = avatarBottom - tileMapPerimeter.TopLeft in delta.Magnitude
                        | OriginSE -> let delta = avatarBottom - tileMapPerimeter.BottomRight in delta.Magnitude
                        | OriginSW -> let delta = avatarBottom - tileMapPerimeter.BottomLeft in delta.Magnitude
                    let battleIndex = int (5.0f / distanceFromOriginMax * distanceFromOrigin)
                    match battleIndex with
                    | 0 | 1 -> Some WeakSpirit
                    | 2 | 3 -> Some NormalSpirit
                    | _ -> Some StrongSpirit
                | FieldStatic _ | FieldConnector _ -> None
            | Choice1Of3 _ -> None
            | Choice2Of3 _ -> None
        | None -> None

[<RequireQualifiedAccess>]
module Data =

    type [<ReferenceEquality>] OmniData =
        { Weapons : Map<WeaponType, WeaponData>
          Armors : Map<ArmorType, ArmorData>
          Accessories : Map<AccessoryType, AccessoryData>
          Consumables : Map<ConsumableType, ConsumableData>
          Techs : Map<TechType, TechData>
          Archetypes : Map<ArchetypeType, ArchetypeData>
          Characters : Map<CharacterType, CharacterData>
          Shops : Map<ShopType, ShopData>
          Battles : Map<BattleType, BattleData>
          Encounters : Map<EncounterType, EncounterData>
          TechAnimations : Map<TechType, TechAnimationData>
          CharacterAnimations : Map<CharacterAnimationType, CharacterAnimationData>
          Fields : Map<FieldType, FieldData> }

    let private readSheet<'d, 'k when 'k : comparison> filePath (getKey : 'd -> 'k) =
        let text = File.ReadAllText filePath
        let symbol = Symbol.ofStringCsv true text (Some filePath)
        let value = symbolToValue<'d list> symbol
        Map.ofListBy (fun data -> getKey data, data) value

    let private readFromFiles () =
        { Weapons = readSheet Assets.Data.WeaponDataFilePath (fun data -> data.WeaponType)
          Armors = readSheet Assets.Data.ArmorDataFilePath (fun data -> data.ArmorType)
          Accessories = readSheet Assets.Data.AccessoryDataFilePath (fun data -> data.AccessoryType)
          Consumables = readSheet Assets.Data.ConsumableDataFilePath (fun data -> data.ConsumableType)
          Techs = readSheet Assets.Data.TechDataFilePath (fun data -> data.TechType)
          Archetypes = readSheet Assets.Data.ArchetypeDataFilePath (fun data -> data.ArchetypeType)
          Characters = readSheet Assets.Data.CharacterDataFilePath (fun data -> data.CharacterType)
          Shops = readSheet Assets.Data.ShopDataFilePath (fun data -> data.ShopType)
          Battles = readSheet Assets.Data.BattleDataFilePath (fun data -> data.BattleType)
          Encounters = readSheet Assets.Data.EncounterDataFilePath (fun data -> data.EncounterType)
          TechAnimations = readSheet Assets.Data.TechAnimationDataFilePath (fun data -> data.TechType)
          CharacterAnimations = readSheet Assets.Data.CharacterAnimationDataFilePath (fun data -> data.CharacterAnimationType)
          Fields = readSheet Assets.Data.FieldDataFilePath (fun data -> data.FieldType) }

    let Value =
        readFromFiles ()