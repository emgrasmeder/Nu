﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace OmniBlade
open System
open System.Numerics
open Prime
open Nu
open OmniBlade

type PropState =
    | DoorState of bool
    | SwitchState of bool
    | CharacterState of Color * CharacterAnimationState
    | SpriteState of Image AssetTag * Color * Blend * Color * Flip * bool
    | NilState

[<RequireQualifiedAccess>]
module Prop =

    [<Syntax
        ("", "", "", "", "",
         Constants.PrettyPrinter.DefaultThresholdMin,
         Constants.PrettyPrinter.DetailedThresholdMax)>]
    type [<SymbolicExpansion>] Prop =
        private
            { Perimeter_ : Box3
              Elevation_ : single
              PropData_ : PropData
              PropState_ : PropState
              PropId_ : int }

        (* Perimeter Properties *)
        member this.Perimeter = this.Perimeter_
        member this.Center = this.Perimeter_.Center
        member this.Bottom = this.Perimeter_.Bottom
        member this.BottomInset = this.Perimeter_.Bottom + Constants.Field.CharacterBottomOffset
        member this.Size = this.Perimeter_.Size

        (* Local Properties *)
        member this.Elevation = this.Elevation_
        member this.PropData = this.PropData_
        member this.PropState = this.PropState_
        member this.PropId = this.PropId_

    let updatePerimeter updater (prop : Prop) =
        { prop with Perimeter_ = updater prop.Perimeter_ }

    let updateCenter updater (prop : Prop) =
        { prop with Perimeter_ = prop.Center |> updater |> prop.Perimeter.WithCenter }

    let updateBottom updater (prop : Prop) =
        { prop with Perimeter_ = prop.Bottom |> updater |> prop.Perimeter.WithBottom }

    let updatePropState updater (prop : Prop) =
        { prop with PropState_ = updater prop.PropState_ }

    let make bounds elevation propData propState propId =
        { Perimeter_ = bounds
          Elevation_ = elevation
          PropData_ = propData
          PropState_ = propState
          PropId_ = propId }

    let empty =
        { Perimeter_ = box3 v3Zero Constants.Gameplay.TileSize
          Elevation_ = 0.0f
          PropData_ = EmptyProp
          PropState_ = NilState
          PropId_ = 0 }

type Prop = Prop.Prop