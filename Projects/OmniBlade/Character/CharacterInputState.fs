﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace OmniBlade
open System
open Prime
open Nu
open OmniBlade

type CharacterInputState =
    | NoInput
    | RegularMenu
    | TechMenu
    | ItemMenu
    | AimReticles of string * AimType

    member this.AimType =
        match this with
        | NoInput | RegularMenu | TechMenu | ItemMenu -> NoAim
        | AimReticles (_, aimType) -> aimType