﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace OmniBlade
open System
open Prime
open Nu
open OmniBlade

[<RequireQualifiedAccess>]
module Algorithms =

    let expReqs =
        [0; 10; 20
         40; 80; 160; 320; 640; 1280 // 2x increase
         1920; 2880; 4320; 6480; 9720 // 1.5x increase
         12150; 15187; 18984; 23730; 29663 // 1.25x increase
         33370; 37542; 42235; 47514; 53453; 60135 // 1.125x increase
         63893; 67886; 72129; 76637; 81427; 86516] // 1.0625 increate

    let expReqRanges =
        List.pairwise expReqs

    let levelMax =
        List.length expReqs

    let levelToExpPointsRange level =
        expReqRanges |>
        List.tryItem (dec level) |> // level 1 is the minimum
        Option.defaultValue (List.last expReqs, Int32.MaxValue)

    let levelToExpPoints level =
        fst (levelToExpPointsRange level)

    let expPointsToLevel expPoints =
        expReqRanges |>
        List.tryFindIndex (fun (low, high) -> expPoints >= low && expPoints < high) |>
        Option.map inc |> // level 1 is the minimum
        Option.defaultValue levelMax

    let expPointsForNextLevel expPoints =
        let level = expPointsToLevel expPoints
        let (_, nextExp) = levelToExpPointsRange level
        nextExp
        
    let expPointsRemainingForNextLevel expPoints =
        match expPointsForNextLevel expPoints with
        | Int32.MaxValue -> 0
        | nextExp -> nextExp - expPoints

    let expPointsToTechs expPoints archetypeType =
        match Data.Value.Archetypes.TryGetValue archetypeType with
        | (true, archetypeData) ->
            let level = expPointsToLevel expPoints
            archetypeData.Techs |>
            Map.filter (fun key _ -> key <= level)  |>
            Map.toValueList |>
            Set.ofList
        | (false, _) -> Set.empty

    let expPointsToTechs3 expPoints expPointsDelta archetypeType =
        let techs = expPointsToTechs expPoints archetypeType
        let techs2 = expPointsToTechs (expPoints + expPointsDelta) archetypeType
        Set.difference techs2 techs

    let hitPointsMax armorOpt archetypeType level =
        let stamina = 
            match Map.tryFind archetypeType Data.Value.Archetypes with
            | Some archetypeData -> archetypeData.Stamina
            | None -> 1.0f
        let enduaranceBase =
            match armorOpt with
            | Some armor ->
                match Map.tryFind armor Data.Value.Armors with
                | Some armorData -> single armorData.EnduranceBase
                | None -> single level * 1.5f
            | None -> single level * 1.5f
        (enduaranceBase + single level) * stamina |> int |> max 1

    let techPointsMax armorOpt archetypeType level =
        let focus = 
            match Map.tryFind archetypeType Data.Value.Archetypes with
            | Some archetypeData -> archetypeData.Focus
            | None -> 1.0f
        let mindBase =
            match armorOpt with
            | Some armor ->
                match Map.tryFind armor Data.Value.Armors with
                | Some armorData -> single armorData.MindBase
                | None -> single level
            | None -> single level
        (mindBase + single level) * focus |> int |> max 0

    let affinityOpt accessories archetypeType (level : int) =
        ignore level
        let affinityOpt =
            match Map.tryFind archetypeType Data.Value.Archetypes with
            | Some archetypeData -> archetypeData.AffinityOpt
            | None -> None
        List.fold
            (fun affinityOpt accessoryType ->
                match Map.tryFind accessoryType Data.Value.Accessories with
                | Some accessoryData -> match accessoryData.AffinityOpt with Some _ as opt -> opt | None -> affinityOpt
                | None -> affinityOpt)
            affinityOpt
            accessories

    let immunities accessories archetypeType (level : int) =
        ignore level
        let immunities =
            match Map.tryFind archetypeType Data.Value.Archetypes with
            | Some archetypeData -> archetypeData.Immunities
            | None -> Set.empty
        List.fold
            (fun immunities accessoryType ->
                match Map.tryFind accessoryType Data.Value.Accessories with
                | Some accessoryData -> Set.union accessoryData.Immunities immunities
                | None -> immunities)
            immunities
            accessories

    let enchantments accessories (archetypeType : ArchetypeType) (level : int) =
        ignore archetypeType
        ignore level
        List.fold
            (fun enchantments accessoryType ->
                match Map.tryFind accessoryType Data.Value.Accessories with
                | Some accessoryData -> Set.union accessoryData.Enchantments enchantments
                | None -> enchantments)
            Set.empty
            accessories

    let power weaponOpt statuses archetypeType level =
        let powerBuff =
            statuses |>
            Map.tryFindKey (function Power (_, _) -> constant true | _ -> constant false) |>
            Option.mapOrDefaultValue (function Power (false, false) -> 0.667f | Power (false, true) -> 0.333f | Power (true, false) -> 1.333f | Power (true, true) -> 2.0f | _ -> 1.0f) 1.0f
        let strength = 
            match Map.tryFind archetypeType Data.Value.Archetypes with
            | Some archetypeData -> archetypeData.Strength
            | None -> 1.0f
        let powerBase =
            match weaponOpt with
            | Some weapon ->
                match Map.tryFind weapon Data.Value.Weapons with
                | Some weaponData -> single weaponData.PowerBase
                | None -> 1.0f
            | None -> 1.0f
        (powerBase + single level) * powerBuff * strength |> int |> max 1

    let magic isMetal weaponOpt statuses archetypeType level =
        let magicBuff =
            if isMetal then
                statuses |>
                Map.tryFindKey (function Power (_, _) -> constant true | _ -> constant false) |>
                Option.mapOrDefaultValue (function Power (false, false) -> 0.667f | Power (false, true) -> 0.333f | Power (true, false) -> 1.333f | Power (true, true) -> 2.0f | _ -> 1.0f) 1.0f
            else
                statuses |>
                Map.tryFindKey (function Magic (_, _) -> constant true | _ -> constant false) |>
                Option.mapOrDefaultValue (function Magic (false, false) -> 0.667f | Magic (false, true) -> 0.333f | Magic (true, false) -> 1.333f | Magic (true, true) -> 2.0f | _ -> 1.0f) 1.0f
        let intelligence = 
            match Map.tryFind archetypeType Data.Value.Archetypes with
            | Some archetypeData -> archetypeData.Intelligence
            | None -> 1.0f
        let magicBase =
            match weaponOpt with
            | Some weapon ->
                match Map.tryFind weapon Data.Value.Weapons with
                | Some weaponData -> single weaponData.MagicBase
                | None -> 1.0f
            | None -> 1.0f
        (magicBase + single level) * magicBuff * intelligence |> int |> max 1

    let shield effectType absorbCreep accessories statuses archetypeType level =
        let shieldBuff =
            statuses |>
            Map.tryFindKey (function Shield (_, _) -> constant true | _ -> constant false) |>
            Option.mapOrDefaultValue (function Shield (false, false) -> 0.667f | Shield (false, true) -> 0.333f | Shield (true, false) -> 1.333f | Shield (true, true) -> 2.0f | _ -> 1.0f) 1.0f
        let (defense, absorbUncrept) = 
            match Map.tryFind archetypeType Data.Value.Archetypes with
            | Some archetypeData -> (archetypeData.Defense, archetypeData.Absorb)
            | None -> (1.0f, 1.0f)
        let absorb = absorbUncrept * absorbCreep
        let shieldBase =
            List.fold
                (fun shieldBase accessoryType ->
                    match Map.tryFind accessoryType Data.Value.Accessories with
                    | Some accessoryData -> single accessoryData.ShieldBase + shieldBase
                    | None -> shieldBase)
                0.0f
                accessories
        let scalar = match effectType with Physical -> defense * 0.5f | Magical -> absorb * 0.5f
        (shieldBase + single level) * shieldBuff * scalar |> int |> max 0

    let defense accessories statuses archetypeType level =
        shield Physical 1.0f accessories statuses archetypeType level

    let absorb absorbCreep accessories statuses archetypeType level =
        shield Magical absorbCreep accessories statuses archetypeType level

    let techs archetypeType level =
        let techs =
            match Map.tryFind archetypeType Data.Value.Archetypes with
            | Some archetypeData -> archetypeData.Techs
            | None -> Map.empty
        match techs |> Map.toList |> List.tryFindIndexBack (fun (levelReq, _) -> level >= levelReq) with
        | Some index -> techs |> Map.toList |> List.take (inc index) |> List.map snd |> Set.ofList
        | None -> Set.empty

    let chargeTechs archetypeType level =
        match Map.tryFind archetypeType Data.Value.Archetypes with
        | Some archetypeData ->
            archetypeData.ChargeTechs |>
            List.filter (fun (_, levelReq, _) -> level >= levelReq) |>
            List.map (fun (chargeRate, _, chargeTech) -> (chargeRate, chargeTech))
        | None -> []

    let goldPrize archetypeType scalar (level : int) =
        let wealth =
            match Map.tryFind archetypeType Data.Value.Archetypes with
            | Some archetypeData -> archetypeData.Wealth
            | None -> 1.0f
        let algo = single level * 1.5f
        max (int (wealth * scalar * algo)) 1

    let expPrize archetypeType scalar (level : int) =
        let mythos =
            match Map.tryFind archetypeType Data.Value.Archetypes with
            | Some archetypeData -> archetypeData.Mythos
            | None -> 1.0f
        let algo = single level * 1.75f
        max (int (mythos * scalar * algo)) 1

    let itemPrizeOpt (_ : ArchetypeType) (_ : int) =
        // TODO: pull this from data.
        if Gen.randomf < Constants.Battle.ItemDropRate
        then Some (Consumable GreenHerb)
        else None