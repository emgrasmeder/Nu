﻿namespace Nu.Ecs
open System
open System.Collections.Generic
open System.IO
open System.Threading.Tasks
open Prime
open Nu

/// An Ecs event callback.
type private EcsCallback<'d, 's> =
    EcsEvent<'d, 's> -> Ecs -> 's -> 's

/// The type of Ecs event.
and [<StructuralEquality; NoComparison; Struct>] EcsEventType =
    | GlobalEvent
    | EntityEvent of Entity : EcsEntity
    | ComponentEvent of Entity2 : EcsEntity * ComponentEvent : string

/// An Ecs event.
and [<StructuralEquality; NoComparison; Struct>] EcsEvent =
    { EcsEventName : string
      EcsEventType : EcsEventType }

/// An Ecs event.
and [<NoEquality; NoComparison>] EcsEvent<'d, 's> =
    { EcsEventData : 'd }

/// Data for an Ecs registration event.
and [<NoEquality; NoComparison; Struct>] EcsChangeData =
    { EcsEntity : EcsEntity
      ComponentName : string }

/// Data for an Ecs registration event.
and [<NoEquality; NoComparison; Struct>] EcsRegistrationData =
    { EcsEntity : EcsEntity
      ComponentName : string }

/// The out-of-box events for the Ecs construct.
and [<AbstractClass; Sealed>] EcsEvents =
    static member Update = { EcsEventName = "Update"; EcsEventType = GlobalEvent }
    static member UpdateParallel = { EcsEventName = "UpdateParallel"; EcsEventType = GlobalEvent }
    static member PostUpdate = { EcsEventName = "PostUpdate"; EcsEventType = GlobalEvent }
    static member PostUpdateParallel = { EcsEventName = "PostUpdateParallel"; EcsEventType = GlobalEvent }
    static member Actualize = { EcsEventName = "Actualize"; EcsEventType = GlobalEvent }
    static member Register entity compName = { EcsEventName = "Register"; EcsEventType = ComponentEvent (entity, compName) }
    static member Unregistering entity compName = { EcsEventName = "Unregistering"; EcsEventType = ComponentEvent (entity, compName) }
    static member Change entity = { EcsEventName = "Change"; EcsEventType = EntityEvent entity }

/// An entity's place in an archetype.
and [<StructuralEquality; NoComparison; Struct>] ArchetypeSlot =
    { ArchetypeIndex : int
      Archetype : Archetype }

/// Identifies an archetype.
and ArchetypeId (terms : Map<string, Term>) =

    let hashCode = hash terms // OPTIMIZATION: hash is cached for speed

    new (intraComponents, subterms : Map<string, Term>) =
        ArchetypeId
            (let intraterms = intraComponents|> Seq.map (fun (compName, compTy) -> (Constants.Ecs.IntraComponentPrefix + compName, Intra (compName, compTy))) |> Map.ofSeq
             intraterms @@ subterms)

    new (intraComponentTypes : Type seq, subterms : Map<string, Term>) =
        ArchetypeId
            (let intraterms = intraComponentTypes |> Seq.map (fun compTy -> let compName = compTy.Name in (Constants.Ecs.IntraComponentPrefix + compName, Intra (compName, compTy))) |> Map.ofSeq
             intraterms @@ subterms)

    member this.Terms : Map<string, Term> =
        terms

    member this.HashCode =
        hashCode

    member this.AddTerm termName term =
        ArchetypeId (Map.add termName term terms)

    member this.RemoveTerm termName =
        ArchetypeId (Map.remove termName terms)

    static member equals (left : ArchetypeId) (right : ArchetypeId) =
        left.HashCode = right.HashCode &&
        Term.equalsMany left.Terms right.Terms

    override this.GetHashCode () =
        hashCode

    override this.Equals that =
        match that with
        | :? ArchetypeId as that -> ArchetypeId.equals this that
        | _ -> failwithumf ()

    static member make (intraComponents : obj seq, subterms) =
        let intraComponentTypes = Seq.map getType intraComponents
        let intraComponentNames = Seq.map (fun (ty : Type) -> ty.Name) intraComponentTypes
        let intraComponents = Seq.zip intraComponentNames intraComponentTypes
        ArchetypeId (intraComponents, subterms)

    static member make (intraComponents : (string * obj) seq, subterms) =
        let intraComponents = Seq.map (fun (compName, compValue) -> (compName, getType compValue)) intraComponents
        ArchetypeId (intraComponents, subterms)

    static member make ?subterms =
        ArchetypeId (([] : (string * Type) list), Option.getOrDefault Map.empty subterms)

    static member make<'c when
        'c : struct and 'c :> 'c Component>
        (?compName, ?subterms) =
        ArchetypeId
            ([(Option.getOrDefault typeof<'c>.Name compName, typeof<'c>)],
             Option.getOrDefault Map.empty subterms)

    static member make<'c, 'c2 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component>
        (?compName, ?comp2Name, ?subterms) =
        ArchetypeId
            ([(Option.getOrDefault typeof<'c>.Name compName, typeof<'c>)
              (Option.getOrDefault typeof<'c2>.Name comp2Name, typeof<'c2>)],
             Option.getOrDefault Map.empty subterms)

    static member make<'c, 'c2, 'c3 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component>
        (?compName, ?comp2Name, ?comp3Name, ?subterms) =
        ArchetypeId
            ([(Option.getOrDefault typeof<'c>.Name compName, typeof<'c>)
              (Option.getOrDefault typeof<'c2>.Name comp2Name, typeof<'c2>)
              (Option.getOrDefault typeof<'c3>.Name comp3Name, typeof<'c3>)],
             Option.getOrDefault Map.empty subterms)

    static member make<'c, 'c2, 'c3, 'c4 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?subterms) =
        ArchetypeId
            ([(Option.getOrDefault typeof<'c>.Name compName, typeof<'c>)
              (Option.getOrDefault typeof<'c2>.Name comp2Name, typeof<'c2>)
              (Option.getOrDefault typeof<'c3>.Name comp3Name, typeof<'c3>)
              (Option.getOrDefault typeof<'c4>.Name comp4Name, typeof<'c4>)],
             Option.getOrDefault Map.empty subterms)

    static member make<'c, 'c2, 'c3, 'c4, 'c5 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component and
        'c5 : struct and 'c5 :> 'c5 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?subterms) =
        ArchetypeId
            ([(Option.getOrDefault typeof<'c>.Name compName, typeof<'c>)
              (Option.getOrDefault typeof<'c2>.Name comp2Name, typeof<'c2>)
              (Option.getOrDefault typeof<'c3>.Name comp3Name, typeof<'c3>)
              (Option.getOrDefault typeof<'c4>.Name comp4Name, typeof<'c4>)
              (Option.getOrDefault typeof<'c5>.Name comp5Name, typeof<'c5>)],
             Option.getOrDefault Map.empty subterms)

    static member make<'c, 'c2, 'c3, 'c4, 'c5, 'c6 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component and
        'c5 : struct and 'c5 :> 'c5 Component and
        'c6 : struct and 'c6 :> 'c6 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?subterms) =
        ArchetypeId
            ([(Option.getOrDefault typeof<'c>.Name compName, typeof<'c>)
              (Option.getOrDefault typeof<'c2>.Name comp2Name, typeof<'c2>)
              (Option.getOrDefault typeof<'c3>.Name comp3Name, typeof<'c3>)
              (Option.getOrDefault typeof<'c4>.Name comp4Name, typeof<'c4>)
              (Option.getOrDefault typeof<'c5>.Name comp5Name, typeof<'c5>)
              (Option.getOrDefault typeof<'c6>.Name comp6Name, typeof<'c6>)],
             Option.getOrDefault Map.empty subterms)

    static member make<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component and
        'c5 : struct and 'c5 :> 'c5 Component and
        'c6 : struct and 'c6 :> 'c6 Component and
        'c7 : struct and 'c7 :> 'c7 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?subterms) =
        ArchetypeId
            ([(Option.getOrDefault typeof<'c>.Name compName, typeof<'c>)
              (Option.getOrDefault typeof<'c2>.Name comp2Name, typeof<'c2>)
              (Option.getOrDefault typeof<'c3>.Name comp3Name, typeof<'c3>)
              (Option.getOrDefault typeof<'c4>.Name comp4Name, typeof<'c4>)
              (Option.getOrDefault typeof<'c5>.Name comp5Name, typeof<'c5>)
              (Option.getOrDefault typeof<'c6>.Name comp6Name, typeof<'c6>)
              (Option.getOrDefault typeof<'c7>.Name comp7Name, typeof<'c7>)],
             Option.getOrDefault Map.empty subterms)

    static member make<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7, 'c8 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component and
        'c5 : struct and 'c5 :> 'c5 Component and
        'c6 : struct and 'c6 :> 'c6 Component and
        'c7 : struct and 'c7 :> 'c7 Component and
        'c8 : struct and 'c8 :> 'c8 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?comp8Name, ?subterms) =
        ArchetypeId
            ([(Option.getOrDefault typeof<'c>.Name compName, typeof<'c>)
              (Option.getOrDefault typeof<'c2>.Name comp2Name, typeof<'c2>)
              (Option.getOrDefault typeof<'c3>.Name comp3Name, typeof<'c3>)
              (Option.getOrDefault typeof<'c4>.Name comp4Name, typeof<'c4>)
              (Option.getOrDefault typeof<'c5>.Name comp5Name, typeof<'c5>)
              (Option.getOrDefault typeof<'c6>.Name comp6Name, typeof<'c6>)
              (Option.getOrDefault typeof<'c7>.Name comp7Name, typeof<'c7>)
              (Option.getOrDefault typeof<'c8>.Name comp8Name, typeof<'c8>)],
             Option.getOrDefault Map.empty subterms)

    static member make<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7, 'c8, 'c9 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component and
        'c5 : struct and 'c5 :> 'c5 Component and
        'c6 : struct and 'c6 :> 'c6 Component and
        'c7 : struct and 'c7 :> 'c7 Component and
        'c8 : struct and 'c8 :> 'c8 Component and
        'c9 : struct and 'c9 :> 'c9 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?comp8Name, ?comp9Name, ?subterms) =
        ArchetypeId
            ([(Option.getOrDefault typeof<'c>.Name compName, typeof<'c>)
              (Option.getOrDefault typeof<'c2>.Name comp2Name, typeof<'c2>)
              (Option.getOrDefault typeof<'c3>.Name comp3Name, typeof<'c3>)
              (Option.getOrDefault typeof<'c4>.Name comp4Name, typeof<'c4>)
              (Option.getOrDefault typeof<'c5>.Name comp5Name, typeof<'c5>)
              (Option.getOrDefault typeof<'c6>.Name comp6Name, typeof<'c6>)
              (Option.getOrDefault typeof<'c7>.Name comp7Name, typeof<'c7>)
              (Option.getOrDefault typeof<'c8>.Name comp8Name, typeof<'c8>)
              (Option.getOrDefault typeof<'c9>.Name comp9Name, typeof<'c9>)],
             Option.getOrDefault Map.empty subterms)

/// A collection of component stores.
and Archetype (archetypeId : ArchetypeId) =

    let mutable freeIndex = 0
    let freeList = hashSetPlus<int> HashIdentity.Structural []
    let stores = dictPlus<string, Store> StringComparer.Ordinal []

    do
        let storeTypeGeneric = typedefof<EntityId Store>
        for termEntry in archetypeId.Terms do
            match termEntry.Value with
            | Intra (name, ty)
            | Extra (name, ty, _) ->
                let storeType = storeTypeGeneric.MakeGenericType [|ty|]
                let store = Activator.CreateInstance (storeType, name) :?> Store
                stores.[name] <- store
            | _ -> ()

    member this.Id = archetypeId
    member this.Length = freeIndex
    member this.Stores = stores
    member this.ComponentNames = hashSetPlus StringComparer.Ordinal stores.Keys

    member private this.Grow () =
        for storeEntry in stores do
            storeEntry.Value.Grow ()

    member private this.AllocIndex () =
        if freeList.Count > 0 then
            let index = Seq.head freeList
            freeList.Remove index |> ignore<bool>
            index
        else
            match Seq.tryHead stores with
            | Some headStoreEntry ->
                let index = freeIndex
                if index = headStoreEntry.Value.Length then this.Grow ()
                freeIndex <- inc freeIndex
                index
            | None ->
                let index = freeIndex
                freeIndex <- inc freeIndex
                index

    member private this.FreeIndex index =
        if index = dec freeIndex
        then freeIndex <- dec freeIndex
        else freeList.Add index |> ignore<bool>

    member this.Register (comps : Dictionary<string, obj>) =
        let index = this.AllocIndex ()
        for compEntry in comps do
            stores.[compEntry.Key].SetItem index compEntry.Value
        index

    member this.Unregister (index : int) =
        for storeEntry in stores do
            storeEntry.Value.ZeroItem index
        this.FreeIndex index

    member this.GetComponents index =
        let comps = dictPlus<string, obj> StringComparer.Ordinal []
        for storeEntry in stores do
            comps.Add (storeEntry.Key, storeEntry.Value.[index])
        comps

    member this.Read count (stream : FileStream) =
        let firstIndex = freeIndex
        let lastIndex = freeIndex + count
        match Seq.tryHead stores with
        | Some headStoreEntry ->
            while headStoreEntry.Value.Length <= lastIndex do
                this.Grow ()
        | None -> ()
        for storeEntry in stores do
            let store = storeEntry.Value
            store.Read count freeIndex stream
        freeIndex <- inc lastIndex
        (firstIndex, lastIndex)

/// An archetype-based Ecs construct.
and Ecs () =

    let mutable subscriptionIdCurrent = 0u
    let mutable entityIdCurrent = 0UL
    let archetypes = dictPlus<ArchetypeId, Archetype> HashIdentity.Structural []
    let archetypeSlots = dictPlus<uint64, ArchetypeSlot> HashIdentity.Structural []
    let componentTypes = dictPlus<string, Type> StringComparer.Ordinal []
    let subscriptions = dictPlus<EcsEvent, Dictionary<uint32, obj>> HashIdentity.Structural []
    let subscribedEntities = dictPlus<EcsEntity, int> HashIdentity.Structural []
    let queries = List<Query> ()

    let createArchetype (archetypeId : ArchetypeId) =
        let archetype = Archetype archetypeId
        archetypes.Add (archetypeId, archetype)
        for query in queries do
            query.TryRegisterArchetype archetype
        archetype

    member private this.AllocSubscriptionId () =
        subscriptionIdCurrent <- inc subscriptionIdCurrent
        if subscriptionIdCurrent = UInt32.MaxValue then failwith "Unbounded use of Ecs subscription ids not supported."
        subscriptionIdCurrent

    member private this.BoxCallback<'d, 's> (callback : EcsCallback<'d, 's>) =
        let boxableCallback = fun (evt : EcsEvent<obj, 's>) store ->
            let evt = { EcsEventData = evt.EcsEventData :?> 'd }
            callback evt store
        boxableCallback :> obj

    member private this.RegisterEntityInternal comps archetypeId entity =
        let archetype =
            match archetypes.TryGetValue archetypeId with
            | (true, archetype) -> archetype
            | (false, _) -> createArchetype archetypeId
        let archetypeIndex = archetype.Register comps
        archetypeSlots.Add (entity.EntityId, { ArchetypeIndex = archetypeIndex; Archetype = archetype })

    member private this.UnregisterEntityInternal archetypeSlot entity =
        let archetype = archetypeSlot.Archetype
        let comps = archetype.GetComponents archetypeSlot.ArchetypeIndex
        archetypeSlots.Remove entity.EntityId |> ignore<bool>
        archetype.Unregister archetypeSlot.ArchetypeIndex
        comps

    member internal this.IndexArchetypeSlot (entity : EcsEntity) =
        archetypeSlots.[entity.EntityId]

    member this.Entity =
        entityIdCurrent <- inc entityIdCurrent
        if entityIdCurrent = UInt64.MaxValue then failwith "Unbounded use of Ecs entity ids not supported."
        { EntityId = entityIdCurrent; Ecs = this }

    member this.Publish<'d, 's> event (eventData : 'd) (state : 's) : 's =
        let mutable stateOpt = state
        match subscriptions.TryGetValue event with
        | (true, callbacks) ->
            for entry in callbacks do
                match entry.Value with
                | :? EcsCallback<obj, 's> as objCallback ->
                    let evt = { EcsEventData = eventData :> obj }
                    stateOpt <- match objCallback evt this stateOpt :> obj with null -> Unchecked.defaultof<'s> | state -> state :?> 's
                | :? EcsCallback<obj, obj> as objCallback ->
                    let evt = { EcsEventData = eventData } : EcsEvent<obj, obj>
                    stateOpt <- match objCallback evt this stateOpt with null -> Unchecked.defaultof<'s> | state -> state :?> 's
                | _ -> ()
        | (false, _) -> ()
        if notNull (state :> obj) && isNull (stateOpt :> obj)
        then state // return orginal state because the conversion process for an unused state may have dropped it
        else stateOpt

    member this.PublishAsync<'d, 's> event (eventData : 'd) =
        let vsync =
            match subscriptions.TryGetValue event with
            | (true, callbacks) ->
                callbacks |>
                Seq.map (fun entry ->
                    Task.Run (fun () ->
                        match entry.Value with
                        | :? EcsCallback<obj, 's> as objCallback ->
                            let evt = { EcsEventData = eventData :> obj }
                            objCallback evt this Unchecked.defaultof<'s> |> ignore<'s>
                        | :? EcsCallback<obj, obj> as objCallback ->
                            let evt = { EcsEventData = eventData } : EcsEvent<obj, obj>
                            objCallback evt this Unchecked.defaultof<obj> |> ignore<obj>
                        | _ -> ()) |> Vsync.AwaitTask) |>
                Vsync.Parallel
            | (false, _) -> Vsync.Parallel []
        Vsync.StartAsTask vsync

    member this.SubscribePlus<'d, 's> subscriptionId event (callback : EcsCallback<'d, 's>) =
        let subscriptionId =
            match subscriptions.TryGetValue event with
            | (true, callbacks) ->
                callbacks.Add (subscriptionId, this.BoxCallback<'d, 's> callback)
                subscriptionId
            | (false, _) ->
                let callbacks = dictPlus HashIdentity.Structural [(subscriptionId, this.BoxCallback<'d, 's> callback)]
                subscriptions.Add (event, callbacks)
                subscriptionId
        match event.EcsEventType with
        | ComponentEvent (entity, _) ->
            match subscribedEntities.TryGetValue entity with
            | (true, count) -> subscribedEntities.[entity] <- inc count
            | (false, _) -> subscribedEntities.Add (entity, 1)
        | _ -> ()
        subscriptionId

    member this.Subscribe<'d, 's> event callback =
        this.SubscribePlus<'d, 's> (this.AllocSubscriptionId ()) event callback |> ignore

    member this.Unsubscribe event subscriptionId =
        let result =
            match subscriptions.TryGetValue event with
            | (true, callbacks) -> callbacks.Remove subscriptionId
            | (false, _) -> false
        if result then
            match event.EcsEventType with
            | ComponentEvent (entity, _) ->
                match subscribedEntities.TryGetValue entity with
                | (true, count) ->
                    if count = 1
                    then subscribedEntities.Remove entity |> ignore<bool>
                    else subscribedEntities.[entity] <- inc count
                | (false, _) -> failwith "Subscribed entities count mismatch."
            | _ -> failwith "Subscribed entities count mismatch."
        result

    member this.RegisterComponentName<'c when 'c : struct and 'c :> 'c Component> componentName =
        match componentTypes.TryGetValue componentName with
        | (true, _) -> failwith "Component type already registered."
        | (false, _) -> componentTypes.Add (componentName, typeof<'c>)

    member this.RegisterTerm (termName : string) term (entity : EcsEntity) =
        if termName.StartsWith Constants.Ecs.IntraComponentPrefix then failwith "Term names that start with '@' are for internal use only."
        if (match term with Intra _ -> true | _ -> false) then failwith "Intra components are for internal use only."
        match archetypeSlots.TryGetValue entity.EntityId with
        | (true, archetypeSlot) ->
            let comps = this.UnregisterEntityInternal archetypeSlot entity
            match term with Extra (compName, _, comp) -> comps.Add (compName, comp.Value) | _ -> ()
            let archetypeId = archetypeSlot.Archetype.Id.AddTerm termName term
            this.RegisterEntityInternal comps archetypeId entity
        | (false, _) ->
            let archetypeId = ArchetypeId.make (Map.singleton termName term)
            let comps = dictPlus StringComparer.Ordinal []
            match term with Extra (compName, _, comp) -> comps.Add (compName, comp.Value) | _ -> ()
            this.RegisterEntityInternal comps archetypeId entity

    member this.UnregisterTerm (termName : string) (entity : EcsEntity) =
        if termName.StartsWith Constants.Ecs.IntraComponentPrefix then failwith "Term names that start with '@' are for internal use only."
        match archetypeSlots.TryGetValue entity.EntityId with
        | (true, archetypeSlot) ->
            let comps = this.UnregisterEntityInternal archetypeSlot entity
            let archetypeId = archetypeSlot.Archetype.Id.RemoveTerm termName
            if archetypeId.Terms.Count > 0 then this.RegisterEntityInternal comps archetypeId entity
        | (false, _) -> ()

    member this.RegisterComponentPlus<'c, 's when 'c : struct and 'c :> 'c Component> compName (comp : 'c) (entity : EcsEntity) (state : 's) =
        match archetypeSlots.TryGetValue entity.EntityId with
        | (true, archetypeSlot) ->
            let comps = this.UnregisterEntityInternal archetypeSlot entity
            comps.Add (compName, comp)
            let archetypeId = archetypeSlot.Archetype.Id.AddTerm (Constants.Ecs.IntraComponentPrefix + compName) (Intra (compName, typeof<'c>))
            this.RegisterEntityInternal comps archetypeId entity
            let eventData = { EcsEntity = entity; ComponentName = compName }
            this.Publish<EcsRegistrationData, obj> (EcsEvents.Register entity compName) eventData (state :> obj) :?> 's
        | (false, _) ->
            let archetypeId = ArchetypeId (Map.singleton (Constants.Ecs.IntraComponentPrefix + compName) (Intra (compName, typeof<'c>)))
            let comps = Dictionary.singleton StringComparer.Ordinal compName (comp :> obj)
            this.RegisterEntityInternal comps archetypeId entity
            let eventData = { EcsEntity = entity; ComponentName = compName }
            this.Publish<EcsRegistrationData, obj> (EcsEvents.Register entity compName) eventData (state :> obj) :?> 's

    member this.RegisterComponent<'c, 's when 'c : struct and 'c :> 'c Component> (comp : 'c) (entity : EcsEntity) (state : 's) =
        this.RegisterComponentPlus<'c, 's> (typeof<'c>.Name) comp (entity : EcsEntity) state

    member this.UnregisterComponentPlus<'c, 's when 'c : struct and 'c :> 'c Component> compName (entity : EcsEntity) (state : 's) =
        match archetypeSlots.TryGetValue entity.EntityId with
        | (true, archetypeSlot) ->
            let eventData = { EcsEntity = entity; ComponentName = compName }
            let state = this.Publish<EcsRegistrationData, obj> (EcsEvents.Unregistering entity compName) eventData (state :> obj) :?> 's
            let comps = this.UnregisterEntityInternal archetypeSlot entity
            let archetypeId = archetypeSlot.Archetype.Id.RemoveTerm (Constants.Ecs.IntraComponentPrefix + compName)
            if archetypeId.Terms.Count > 0 then
                comps.Remove compName |> ignore<bool>
                this.RegisterEntityInternal comps archetypeId entity
                state
            else state
        | (false, _) -> state

    member this.UnregisterComponent<'c, 's when 'c : struct and 'c :> 'c Component> (entity : EcsEntity) (state : 's) =
        this.UnregisterComponentPlus<'c, 's> typeof<'c>.Name entity state

    member this.RegisterEntity elideEvents comps archetypeId state =
        let archetype =
            match archetypes.TryGetValue archetypeId with
            | (true, archetype) -> archetype
            | (false, _) -> createArchetype archetypeId
        let entity = this.Entity
        let archetypeIndex = archetype.Register comps
        archetypeSlots.Add (entity.EntityId, { ArchetypeIndex = archetypeIndex; Archetype = archetype })
        let mutable state = state
        if not elideEvents then
            for compName in archetype.Stores.Keys do
                let eventData = { EcsEntity = entity; ComponentName = compName }
                state <- this.Publish<EcsRegistrationData, obj> (EcsEvents.Unregistering entity compName) eventData (state :> obj) :?> 's
        (entity, state)

    member this.UnregisterEntity (entity : EcsEntity) (state : 's) =
        match archetypeSlots.TryGetValue entity.EntityId with
        | (true, archetypeSlot) ->
            let archetype = archetypeSlot.Archetype
            let mutable state = state
            if subscribedEntities.ContainsKey entity then
                for compName in archetype.Stores.Keys do
                    let eventData = { EcsEntity = entity; ComponentName = compName }
                    state <- this.Publish<EcsRegistrationData, obj> (EcsEvents.Unregistering entity compName) eventData (state :> obj) :?> 's
            archetype.Unregister archetypeSlot.ArchetypeIndex
            state
        | (false, _) -> state

    member this.RegisterEntitiesPlus elideEvents count comps archetypeId state =

        // get archetype
        let archetype =
            match archetypes.TryGetValue archetypeId with
            | (true, archetype) -> archetype
            | (false, _) -> createArchetype archetypeId

        // register entities to archetype
        let mutable state = state
        let entitys = SegmentedArray.zeroCreate count
        for i in 0 .. dec count do
            let entity = this.Entity
            let archetypeIndex = archetype.Register comps
            archetypeSlots.Add (entity.EntityId, { ArchetypeIndex = archetypeIndex; Archetype = archetype })
            entitys.[i] <- entity
            if not elideEvents then
                for compName in archetype.Stores.Keys do
                    let eventData = { EcsEntity = entity; ComponentName = compName }
                    state <- this.Publish<EcsRegistrationData, obj> (EcsEvents.Unregistering entity compName) eventData (state :> obj) :?> 's

        // fin
        (entitys, state)

    member this.RegisterEntities elideEvents count comps archetypeId state =
        let comps = dictPlus StringComparer.Ordinal (Seq.map (fun comp -> (getTypeName comp, comp)) comps)
        this.RegisterEntitiesPlus elideEvents count comps archetypeId state

    member this.RegisterQuery (query : Query) =
        for archetypeEntry in archetypes do
            query.TryRegisterArchetype archetypeEntry.Value
        queries.Add query
        query

    member this.ReadComponents count archetypeId stream =
        let archetype =
            match archetypes.TryGetValue archetypeId with
            | (true, archetype) -> archetype
            | (false, _) -> createArchetype archetypeId
        let (firstIndex, lastIndex) = archetype.Read count stream
        let entitys = SegmentedArray.zeroCreate count
        for i in firstIndex .. lastIndex do
            let entity = this.Entity
            archetypeSlots.Add (entity.EntityId, { ArchetypeIndex = i; Archetype = archetype })
            entitys.[i - firstIndex] <- entity
        entitys

and [<StructuralEquality; NoComparison; Struct>] EcsEntity =
    { EntityId : uint64
      Ecs : Ecs }

    member inline private this.IndexStore<'c when 'c : struct and 'c :> 'c Component> compName archetypeId (stores : Dictionary<string, Store>) =
        match stores.TryGetValue compName with
        | (true, store) -> store :?> 'c Store
        | (false, _) -> failwith ("Invalid entity frame for archetype " + scstring archetypeId + ".")

    member this.RegisterPlus<'c, 's when 'c : struct and 'c :> 'c Component> compName (comp : 'c) (state : 's) =
        this.Ecs.RegisterComponentPlus<'c, 's> compName comp this state

    member this.Register<'c, 's when 'c : struct and 'c :> 'c Component> (comp : 'c) (state : 's) =
        this.Ecs.RegisterComponent<'c, 's> comp this state

    member this.UnregisterPlus<'c, 's when 'c : struct and 'c :> 'c Component> compName (state : 's) =
        this.Ecs.UnregisterComponentPlus<'c, 's> compName this state

    member this.Unregister<'c, 's when 'c : struct and 'c :> 'c Component> (state : 's) =
        this.Ecs.UnregisterComponent<'c, 's> this state

    member this.RegisterTerm termName term =
        this.Ecs.RegisterTerm termName term this

    member this.UnregisterTerm termName =
        this.Ecs.UnregisterTerm termName this

    member this.ValidatePlus compName =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let stores = archetypeSlot.Archetype.Stores
        stores.ContainsKey compName

    member this.Validate<'c when 'c : struct and 'c :> 'c Component> () =
        this.ValidatePlus typeof<'c>.Name

    member this.ValidateTerm termName =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let terms = archetypeSlot.Archetype.Id.Terms
        terms.ContainsKey termName

    member this.IndexPlus<'c when 'c : struct and 'c :> 'c Component> compName =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let stores = archetypeSlot.Archetype.Stores
        let store = stores.[compName] :?> 'c Store
        let i = archetypeSlot.ArchetypeIndex
        &store.[i]

    member this.Index<'c when 'c : struct and 'c :> 'c Component> () =
        this.IndexPlus<'c> typeof<'c>.Name

    member this.IndexTerm termName =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let terms = archetypeSlot.Archetype.Id.Terms
        terms.[termName]

    member this.UpdateTerm updater termName =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let terms = archetypeSlot.Archetype.Id.Terms
        let term = updater terms.[termName]
        this.UnregisterTerm termName
        this.RegisterTerm termName term

    member this.MutatePlus<'c when 'c : struct and 'c :> 'c Component> compName (comp : 'c) =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let stores = archetypeSlot.Archetype.Stores
        let store = stores.[compName] :?> 'c Store
        let i = archetypeSlot.ArchetypeIndex
        store.[i] <- comp

    member this.Mutate<'c when 'c : struct and 'c :> 'c Component> (comp : 'c) =
        this.MutatePlus<'c> typeof<'c>.Name comp

    member this.ChangePlus<'c, 's when 'c : struct and 'c :> 'c Component> compName (comp : 'c) (state : 's) =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let stores = archetypeSlot.Archetype.Stores
        let store = stores.[compName] :?> 'c Store
        let i = archetypeSlot.ArchetypeIndex
        store.[i] <- comp
        this.Ecs.Publish (EcsEvents.Change this) { EcsEntity = this; ComponentName = compName } state

    member this.Change<'c, 's when 'c : struct and 'c :> 'c Component> (comp : 'c) (state : 's) =
        this.ChangePlus<'c, 's> typeof<'c>.Name comp state

    member this.Frame (statement : Statement<'c, 's>, ?compName, ?state : 's) =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let archetype = archetypeSlot.Archetype
        let archetypeId = archetype.Id
        let stores = archetype.Stores
        let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
        let i = archetypeSlot.ArchetypeIndex
        statement.Invoke (&store.[i], Option.getOrDefault Unchecked.defaultof<'s> state)

    member this.Frame
        (statement : Statement<'c, 'c2, 's>,
         ?compName, ?comp2Name, ?state : 's) =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let archetype = archetypeSlot.Archetype
        let archetypeId = archetype.Id
        let stores = archetype.Stores
        let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
        let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
        let i = archetypeSlot.ArchetypeIndex
        statement.Invoke
            (&store.[i], &store2.[i],
             Option.getOrDefault Unchecked.defaultof<'s> state)

    member this.Frame
        (statement : Statement<'c, 'c2, 'c3, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?state : 's) =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let archetype = archetypeSlot.Archetype
        let archetypeId = archetype.Id
        let stores = archetype.Stores
        let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
        let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
        let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
        let i = archetypeSlot.ArchetypeIndex
        statement.Invoke
            (&store.[i], &store2.[i], &store3.[i],
             Option.getOrDefault Unchecked.defaultof<'s> state)

    member this.Frame
        (statement : Statement<'c, 'c2, 'c3, 'c4, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?state : 's) =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let archetype = archetypeSlot.Archetype
        let archetypeId = archetype.Id
        let stores = archetype.Stores
        let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
        let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
        let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
        let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
        let i = archetypeSlot.ArchetypeIndex
        statement.Invoke
            (&store.[i], &store2.[i], &store3.[i], &store4.[i],
             Option.getOrDefault Unchecked.defaultof<'s> state)

    member this.Frame
        (statement : Statement<'c, 'c2, 'c3, 'c4, 'c5, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?state : 's) =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let archetype = archetypeSlot.Archetype
        let archetypeId = archetype.Id
        let stores = archetype.Stores
        let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
        let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
        let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
        let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
        let store5 = this.IndexStore<'c5> (Option.getOrDefault typeof<'c5>.Name comp5Name) archetypeId stores
        let i = archetypeSlot.ArchetypeIndex
        statement.Invoke
            (&store.[i], &store2.[i], &store3.[i], &store4.[i], &store5.[i],
             Option.getOrDefault Unchecked.defaultof<'s> state)

    member this.Frame
        (statement : Statement<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?state : 's) =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let archetype = archetypeSlot.Archetype
        let archetypeId = archetype.Id
        let stores = archetype.Stores
        let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
        let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
        let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
        let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
        let store5 = this.IndexStore<'c5> (Option.getOrDefault typeof<'c5>.Name comp5Name) archetypeId stores
        let store6 = this.IndexStore<'c6> (Option.getOrDefault typeof<'c6>.Name comp6Name) archetypeId stores
        let i = archetypeSlot.ArchetypeIndex
        statement.Invoke
            (&store.[i], &store2.[i], &store3.[i], &store4.[i], &store5.[i], &store6.[i],
             Option.getOrDefault Unchecked.defaultof<'s> state)

    member this.Frame
        (statement : Statement<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?state : 's) =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let archetype = archetypeSlot.Archetype
        let archetypeId = archetype.Id
        let stores = archetype.Stores
        let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
        let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
        let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
        let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
        let store5 = this.IndexStore<'c5> (Option.getOrDefault typeof<'c5>.Name comp5Name) archetypeId stores
        let store6 = this.IndexStore<'c6> (Option.getOrDefault typeof<'c6>.Name comp6Name) archetypeId stores
        let store7 = this.IndexStore<'c7> (Option.getOrDefault typeof<'c7>.Name comp7Name) archetypeId stores
        let i = archetypeSlot.ArchetypeIndex
        statement.Invoke
            (&store.[i], &store2.[i], &store3.[i], &store4.[i], &store5.[i], &store6.[i], &store7.[i],
             Option.getOrDefault Unchecked.defaultof<'s> state)

    member this.Frame
        (statement : Statement<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7, 'c8, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?comp8Name, ?state : 's) =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let archetype = archetypeSlot.Archetype
        let archetypeId = archetype.Id
        let stores = archetype.Stores
        let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
        let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
        let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
        let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
        let store5 = this.IndexStore<'c5> (Option.getOrDefault typeof<'c5>.Name comp5Name) archetypeId stores
        let store6 = this.IndexStore<'c6> (Option.getOrDefault typeof<'c6>.Name comp6Name) archetypeId stores
        let store7 = this.IndexStore<'c7> (Option.getOrDefault typeof<'c7>.Name comp7Name) archetypeId stores
        let store8 = this.IndexStore<'c8> (Option.getOrDefault typeof<'c8>.Name comp8Name) archetypeId stores
        let i = archetypeSlot.ArchetypeIndex
        statement.Invoke
            (&store.[i], &store2.[i], &store3.[i], &store4.[i], &store5.[i], &store6.[i], &store7.[i], &store8.[i],
             Option.getOrDefault Unchecked.defaultof<'s> state)

    member this.Frame
        (statement : Statement<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7, 'c8, 'c9, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?comp8Name, ?comp9Name, ?state : 's) =
        let archetypeSlot = this.Ecs.IndexArchetypeSlot this
        let archetype = archetypeSlot.Archetype
        let archetypeId = archetype.Id
        let stores = archetype.Stores
        let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
        let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
        let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
        let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
        let store5 = this.IndexStore<'c5> (Option.getOrDefault typeof<'c5>.Name comp5Name) archetypeId stores
        let store6 = this.IndexStore<'c6> (Option.getOrDefault typeof<'c6>.Name comp6Name) archetypeId stores
        let store7 = this.IndexStore<'c7> (Option.getOrDefault typeof<'c7>.Name comp7Name) archetypeId stores
        let store8 = this.IndexStore<'c8> (Option.getOrDefault typeof<'c8>.Name comp8Name) archetypeId stores
        let store9 = this.IndexStore<'c9> (Option.getOrDefault typeof<'c9>.Name comp9Name) archetypeId stores
        let i = archetypeSlot.ArchetypeIndex
        statement.Invoke
            (&store.[i], &store2.[i], &store3.[i], &store4.[i], &store5.[i], &store6.[i], &store7.[i], &store8.[i], &store9.[i],
             Option.getOrDefault Unchecked.defaultof<'s> state)

and Query (compNames : string HashSet, subqueries : Subquery seq) =

    let archetypes = dictPlus<ArchetypeId, Archetype> HashIdentity.Structural []
    let subqueries = List subqueries

    do
        for compName in compNames do
            subqueries.Add (Tagged (Var (Constants.Ecs.IntraComponentPrefix + compName)))

    member inline private this.IndexStore<'c when 'c : struct and 'c :> 'c Component> compName archetypeId (stores : Dictionary<string, Store>) =
        match stores.TryGetValue compName with
        | (true, store) -> store :?> 'c Store
        | (false, _) -> failwith ("Invalid entity frame for archetype " + scstring archetypeId + ".")

    member this.Subqueries =
        seq subqueries

    member this.TryRegisterArchetype (archetype : Archetype) =
        if  not (archetypes.ContainsKey archetype.Id) &&
            Subquery.evalMany archetype.Id.Terms subqueries then
            archetypes.Add (archetype.Id, archetype)

    member this.Iterate (statement : Statement<'c, 's>, ?compName, ?state : 's) : 's =
        let mutable state = Option.getOrDefault Unchecked.defaultof<'s> state
        for archetypeEntry in archetypes do
            let archetype = archetypeEntry.Value
            let archetypeId = archetype.Id
            let length = archetype.Length
            let stores = archetype.Stores
            let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
            let mutable i = 0
            while i < store.Length && i < length do
                let comp = &store.[i]
                if comp.Active then
                    state <- statement.Invoke (&comp, state)
                    i <- inc i
        state

    member this.Iterate
        (statement : Statement<'c, 'c2, 's>,
         ?compName, ?comp2Name, ?state : 's) : 's =
        let mutable state = Option.getOrDefault Unchecked.defaultof<'s> state
        for archetypeEntry in archetypes do
            let archetype = archetypeEntry.Value
            let archetypeId = archetype.Id
            let length = archetype.Length
            let stores = archetype.Stores
            let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
            let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
            let mutable i = 0
            while i < store.Length && i < length do
                let comp = &store.[i]
                if comp.Active then
                    state <- statement.Invoke (&comp, &store2.[i], state)
                    i <- inc i
        state

    member this.Iterate
        (statement : Statement<'c, 'c2, 'c3, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?state : 's) : 's =
        let mutable state = Option.getOrDefault Unchecked.defaultof<'s> state
        for archetypeEntry in archetypes do
            let archetype = archetypeEntry.Value
            let archetypeId = archetype.Id
            let length = archetype.Length
            let stores = archetype.Stores
            let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
            let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
            let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
            let mutable i = 0
            while i < store.Length && i < length do
                let comp = &store.[i]
                if comp.Active then
                    state <- statement.Invoke (&comp, &store2.[i], &store3.[i], state)
                    i <- inc i
        state

    member this.Iterate
        (statement : Statement<'c, 'c2, 'c3, 'c4, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?state : 's) : 's =
        let mutable state = Option.getOrDefault Unchecked.defaultof<'s> state
        for archetypeEntry in archetypes do
            let archetype = archetypeEntry.Value
            let archetypeId = archetype.Id
            let length = archetype.Length
            let stores = archetype.Stores
            let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
            let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
            let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
            let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
            let mutable i = 0
            while i < store.Length && i < length do
                let comp = &store.[i]
                if comp.Active then
                    state <- statement.Invoke (&comp, &store2.[i], &store3.[i], &store4.[i], state)
                    i <- inc i
        state

    member this.Iterate
        (statement : Statement<'c, 'c2, 'c3, 'c4, 'c5, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?state : 's) : 's =
        let mutable state = Option.getOrDefault Unchecked.defaultof<'s> state
        for archetypeEntry in archetypes do
            let archetype = archetypeEntry.Value
            let archetypeId = archetype.Id
            let length = archetype.Length
            let stores = archetype.Stores
            let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
            let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
            let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
            let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
            let store5 = this.IndexStore<'c5> (Option.getOrDefault typeof<'c5>.Name comp5Name) archetypeId stores
            let mutable i = 0
            while i < store.Length && i < length do
                let comp = &store.[i]
                if comp.Active then
                    state <- statement.Invoke (&comp, &store2.[i], &store3.[i], &store4.[i], &store5.[i], state)
                    i <- inc i
        state

    member this.Iterate
        (statement : Statement<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?state : 's) : 's =
        let mutable state = Option.getOrDefault Unchecked.defaultof<'s> state
        for archetypeEntry in archetypes do
            let archetype = archetypeEntry.Value
            let archetypeId = archetype.Id
            let length = archetype.Length
            let stores = archetype.Stores
            let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
            let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
            let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
            let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
            let store5 = this.IndexStore<'c5> (Option.getOrDefault typeof<'c5>.Name comp5Name) archetypeId stores
            let store6 = this.IndexStore<'c6> (Option.getOrDefault typeof<'c6>.Name comp6Name) archetypeId stores
            let mutable i = 0
            while i < store.Length && i < length do
                let comp = &store.[i]
                if comp.Active then
                    state <- statement.Invoke (&comp, &store2.[i], &store3.[i], &store4.[i], &store5.[i], &store6.[i], state)
                    i <- inc i
        state

    member this.Iterate
        (statement : Statement<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?state : 's) : 's =
        let mutable state = Option.getOrDefault Unchecked.defaultof<'s> state
        for archetypeEntry in archetypes do
            let archetype = archetypeEntry.Value
            let archetypeId = archetype.Id
            let length = archetype.Length
            let stores = archetype.Stores
            let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
            let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
            let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
            let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
            let store5 = this.IndexStore<'c5> (Option.getOrDefault typeof<'c5>.Name comp5Name) archetypeId stores
            let store6 = this.IndexStore<'c6> (Option.getOrDefault typeof<'c6>.Name comp6Name) archetypeId stores
            let store7 = this.IndexStore<'c7> (Option.getOrDefault typeof<'c7>.Name comp7Name) archetypeId stores
            let mutable i = 0
            while i < store.Length && i < length do
                let comp = &store.[i]
                if comp.Active then
                    state <- statement.Invoke (&comp, &store2.[i], &store3.[i], &store4.[i], &store5.[i], &store6.[i], &store7.[i], state)
                    i <- inc i
        state

    member this.Iterate
        (statement : Statement<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7, 'c8, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?comp8Name, ?state : 's) : 's =
        let mutable state = Option.getOrDefault Unchecked.defaultof<'s> state
        for archetypeEntry in archetypes do
            let archetype = archetypeEntry.Value
            let archetypeId = archetype.Id
            let length = archetype.Length
            let stores = archetype.Stores
            let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
            let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
            let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
            let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
            let store5 = this.IndexStore<'c5> (Option.getOrDefault typeof<'c5>.Name comp5Name) archetypeId stores
            let store6 = this.IndexStore<'c6> (Option.getOrDefault typeof<'c6>.Name comp6Name) archetypeId stores
            let store7 = this.IndexStore<'c7> (Option.getOrDefault typeof<'c7>.Name comp7Name) archetypeId stores
            let store8 = this.IndexStore<'c8> (Option.getOrDefault typeof<'c8>.Name comp8Name) archetypeId stores
            let mutable i = 0
            while i < store.Length && i < length do
                let comp = &store.[i]
                if comp.Active then
                    state <- statement.Invoke (&comp, &store2.[i], &store3.[i], &store4.[i], &store5.[i], &store6.[i], &store7.[i], &store8.[i], state)
                    i <- inc i
        state

    member this.Iterate
        (statement : Statement<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7, 'c8, 'c9, 's>,
         ?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?comp8Name, ?comp9Name, ?state : 's) : 's =
        let mutable state = Option.getOrDefault Unchecked.defaultof<'s> state
        for archetypeEntry in archetypes do
            let archetype = archetypeEntry.Value
            let archetypeId = archetype.Id
            let length = archetype.Length
            let stores = archetype.Stores
            let store = this.IndexStore<'c> (Option.getOrDefault typeof<'c>.Name compName) archetypeId stores
            let store2 = this.IndexStore<'c2> (Option.getOrDefault typeof<'c2>.Name comp2Name) archetypeId stores
            let store3 = this.IndexStore<'c3> (Option.getOrDefault typeof<'c3>.Name comp3Name) archetypeId stores
            let store4 = this.IndexStore<'c4> (Option.getOrDefault typeof<'c4>.Name comp4Name) archetypeId stores
            let store5 = this.IndexStore<'c5> (Option.getOrDefault typeof<'c5>.Name comp5Name) archetypeId stores
            let store6 = this.IndexStore<'c6> (Option.getOrDefault typeof<'c6>.Name comp6Name) archetypeId stores
            let store7 = this.IndexStore<'c7> (Option.getOrDefault typeof<'c7>.Name comp7Name) archetypeId stores
            let store8 = this.IndexStore<'c8> (Option.getOrDefault typeof<'c8>.Name comp8Name) archetypeId stores
            let store9 = this.IndexStore<'c9> (Option.getOrDefault typeof<'c9>.Name comp9Name) archetypeId stores
            let mutable i = 0
            while i < store.Length && i < length do
                let comp = &store.[i]
                if comp.Active then
                    state <- statement.Invoke (&comp, &store2.[i], &store3.[i], &store4.[i], &store5.[i], &store6.[i], &store7.[i], &store8.[i], &store9.[i], state)
                    i <- inc i
        state

    static member make
        (?subqueries) =
        Query
            (hashSetPlus HashIdentity.Structural [],
             Option.getOrDefault [] subqueries)

    static member make<'c when
        'c : struct and 'c :> 'c Component>
        (?compName, ?subqueries) =
        Query
            (hashSetPlus HashIdentity.Structural
                [Option.getOrDefault typeof<'c>.Name compName],
             Option.getOrDefault [] subqueries)

    static member make<'c, 'c2 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component>
        (?compName, ?comp2Name, ?subqueries) =
        Query
            (hashSetPlus HashIdentity.Structural
                [Option.getOrDefault typeof<'c>.Name compName
                 Option.getOrDefault typeof<'c2>.Name comp2Name],
             Option.getOrDefault [] subqueries)

    static member make<'c, 'c2, 'c3 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component>
        (?compName, ?comp2Name, ?comp3Name, ?subqueries) =
        Query
            (hashSetPlus HashIdentity.Structural
                [Option.getOrDefault typeof<'c>.Name compName
                 Option.getOrDefault typeof<'c2>.Name comp2Name
                 Option.getOrDefault typeof<'c3>.Name comp3Name],
             Option.getOrDefault [] subqueries)

    static member make<'c, 'c2, 'c3, 'c4 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?subqueries) =
        Query
            (hashSetPlus HashIdentity.Structural
                [Option.getOrDefault typeof<'c>.Name compName
                 Option.getOrDefault typeof<'c2>.Name comp2Name
                 Option.getOrDefault typeof<'c3>.Name comp3Name
                 Option.getOrDefault typeof<'c4>.Name comp4Name],
             Option.getOrDefault [] subqueries)

    static member make<'c, 'c2, 'c3, 'c4, 'c5 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component and
        'c5 : struct and 'c5 :> 'c5 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?subqueries) =
        Query
            (hashSetPlus HashIdentity.Structural
                [Option.getOrDefault typeof<'c>.Name compName
                 Option.getOrDefault typeof<'c2>.Name comp2Name
                 Option.getOrDefault typeof<'c3>.Name comp3Name
                 Option.getOrDefault typeof<'c4>.Name comp4Name
                 Option.getOrDefault typeof<'c5>.Name comp5Name],
             Option.getOrDefault [] subqueries)

    static member make<'c, 'c2, 'c3, 'c4, 'c5, 'c6 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component and
        'c5 : struct and 'c5 :> 'c5 Component and
        'c6 : struct and 'c6 :> 'c6 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?subqueries) =
        Query
            (hashSetPlus HashIdentity.Structural
                [Option.getOrDefault typeof<'c>.Name compName
                 Option.getOrDefault typeof<'c2>.Name comp2Name
                 Option.getOrDefault typeof<'c3>.Name comp3Name
                 Option.getOrDefault typeof<'c4>.Name comp4Name
                 Option.getOrDefault typeof<'c5>.Name comp5Name
                 Option.getOrDefault typeof<'c6>.Name comp6Name],
             Option.getOrDefault [] subqueries)

    static member make<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component and
        'c5 : struct and 'c5 :> 'c5 Component and
        'c6 : struct and 'c6 :> 'c6 Component and
        'c7 : struct and 'c7 :> 'c7 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?subqueries) =
        Query
            (hashSetPlus HashIdentity.Structural
                [Option.getOrDefault typeof<'c>.Name compName
                 Option.getOrDefault typeof<'c2>.Name comp2Name
                 Option.getOrDefault typeof<'c3>.Name comp3Name
                 Option.getOrDefault typeof<'c4>.Name comp4Name
                 Option.getOrDefault typeof<'c5>.Name comp5Name
                 Option.getOrDefault typeof<'c6>.Name comp6Name
                 Option.getOrDefault typeof<'c7>.Name comp7Name],
             Option.getOrDefault [] subqueries)

    static member make<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7, 'c8 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component and
        'c5 : struct and 'c5 :> 'c5 Component and
        'c6 : struct and 'c6 :> 'c6 Component and
        'c7 : struct and 'c7 :> 'c7 Component and
        'c8 : struct and 'c8 :> 'c8 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?comp8Name, ?subqueries) =
        Query
            (hashSetPlus HashIdentity.Structural
                [Option.getOrDefault typeof<'c>.Name compName
                 Option.getOrDefault typeof<'c2>.Name comp2Name
                 Option.getOrDefault typeof<'c3>.Name comp3Name
                 Option.getOrDefault typeof<'c4>.Name comp4Name
                 Option.getOrDefault typeof<'c5>.Name comp5Name
                 Option.getOrDefault typeof<'c6>.Name comp6Name
                 Option.getOrDefault typeof<'c7>.Name comp7Name
                 Option.getOrDefault typeof<'c8>.Name comp8Name],
             Option.getOrDefault [] subqueries)

    static member make<'c, 'c2, 'c3, 'c4, 'c5, 'c6, 'c7, 'c8, 'c9 when
        'c : struct and 'c :> 'c Component and
        'c2 : struct and 'c2 :> 'c2 Component and
        'c3 : struct and 'c3 :> 'c3 Component and
        'c4 : struct and 'c4 :> 'c4 Component and
        'c5 : struct and 'c5 :> 'c5 Component and
        'c6 : struct and 'c6 :> 'c6 Component and
        'c7 : struct and 'c7 :> 'c7 Component and
        'c8 : struct and 'c8 :> 'c8 Component and
        'c9 : struct and 'c9 :> 'c9 Component>
        (?compName, ?comp2Name, ?comp3Name, ?comp4Name, ?comp5Name, ?comp6Name, ?comp7Name, ?comp8Name, ?comp9Name, ?subqueries) =
        Query
            (hashSetPlus HashIdentity.Structural
                [Option.getOrDefault typeof<'c>.Name compName
                 Option.getOrDefault typeof<'c2>.Name comp2Name
                 Option.getOrDefault typeof<'c3>.Name comp3Name
                 Option.getOrDefault typeof<'c4>.Name comp4Name
                 Option.getOrDefault typeof<'c5>.Name comp5Name
                 Option.getOrDefault typeof<'c6>.Name comp6Name
                 Option.getOrDefault typeof<'c7>.Name comp7Name
                 Option.getOrDefault typeof<'c8>.Name comp8Name
                 Option.getOrDefault typeof<'c9>.Name comp9Name],
             Option.getOrDefault [] subqueries)