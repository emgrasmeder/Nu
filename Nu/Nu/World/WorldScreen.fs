﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.IO
open Prime
open Nu

[<AutoOpen; ModuleBinding>]
module WorldScreenModule =

    type Screen with

        member this.GetDispatcher world = World.getScreenDispatcher this world
        member this.Dispatcher = lensReadOnly (nameof this.Dispatcher) this this.GetDispatcher
        member this.GetModelGeneric<'a> world = World.getScreenModel<'a> this world
        member this.SetModelGeneric<'a> value world = World.setScreenModel<'a> false value this world |> snd'
        member this.ModelGeneric<'a> () = lens Constants.Engine.ModelPropertyName this this.GetModelGeneric<'a> this.SetModelGeneric<'a>
        member this.GetEcs world = World.getScreenEcs this world
        member this.Ecs = lensReadOnly (nameof this.Ecs) this this.GetEcs
        member this.GetTransitionState world = World.getScreenTransitionState this world
        member this.SetTransitionState value world = World.setScreenTransitionState value this world |> snd'
        member this.TransitionState = lens (nameof this.TransitionState) this this.GetTransitionState this.SetTransitionState
        member this.GetIncoming world = World.getScreenIncoming this world
        member this.SetIncoming value world = World.setScreenIncoming value this world |> snd'
        member this.Incoming = lens (nameof this.Incoming) this this.GetIncoming this.SetIncoming
        member this.GetOutgoing world = World.getScreenOutgoing this world
        member this.SetOutgoing value world = World.setScreenOutgoing value this world |> snd'
        member this.Outgoing = lens (nameof this.Outgoing) this this.GetOutgoing this.SetOutgoing
        member this.GetSlideOpt world = World.getScreenSlideOpt this world
        member this.SetSlideOpt value world = World.setScreenSlideOpt value this world |> snd'
        member this.SlideOpt = lens (nameof this.SlideOpt) this this.GetSlideOpt this.SetSlideOpt
        member this.GetProtected world = World.getScreenProtected this world
        member this.Protected = lensReadOnly (nameof this.Protected) this this.GetProtected
        member this.GetPersistent world = World.getScreenPersistent this world
        member this.SetPersistent value world = World.setScreenPersistent value this world |> snd'
        member this.Persistent = lens (nameof this.Persistent) this this.GetPersistent this.SetPersistent
        member this.GetDestroying world = World.getScreenDestroying this world
        member this.Destroying = lensReadOnly (nameof this.Destroying) this this.GetDestroying
        member this.GetScriptFrame world = World.getScreenScriptFrame this world
        member this.ScriptFrame = lensReadOnly (nameof this.ScriptFrame) this this.GetScriptFrame
        member this.GetOrder world = World.getScreenOrder this world
        member this.Order = lensReadOnly (nameof this.Order) this this.GetOrder
        member this.GetId world = World.getScreenId this world
        member this.Id = lensReadOnly (nameof this.Id) this this.GetId

        member this.RegisterEvent = Events.Register --> this
        member this.UnregisteringEvent = Events.Unregistering --> this
        member this.ChangeEvent propertyName = Events.Change propertyName --> this
        member this.UpdateEvent = Events.Update --> this
        member this.PostUpdateEvent = Events.PostUpdate --> this
        member this.RenderEvent = Events.Render --> this
        member this.SelectEvent = Events.Select --> this
        member this.DeselectingEvent = Events.Deselecting --> this
        member this.IncomingStartEvent = Events.IncomingStart --> this
        member this.IncomingFinishEvent = Events.IncomingFinish --> this
        member this.OutgoingStartEvent = Events.OutgoingStart --> this
        member this.OutgoingFinishEvent = Events.OutgoingFinish --> this

        /// Try to get a property value and type.
        member this.TryGetProperty propertyName world =
            let mutable property = Unchecked.defaultof<_>
            let found = World.tryGetScreenProperty (propertyName, this, world, &property)
            if found then Some property else None

        /// Get a property value and type.
        member this.GetProperty propertyName world =
            World.getScreenProperty propertyName this world

        /// Get an xtension property value.
        member this.TryGet<'a> propertyName world : 'a =
            World.tryGetScreenXtensionValue<'a> propertyName this world

        /// Get an xtension property value.
        member this.Get<'a> propertyName world : 'a =
            World.getScreenXtensionValue<'a> propertyName this world

        /// Try to set a property value with explicit type.
        member this.TrySetProperty propertyName property world =
            World.trySetScreenProperty propertyName property this world

        /// Set a property value with explicit type.
        member this.SetProperty propertyName property world =
            World.setScreenProperty propertyName property this world |> snd'

        /// To try set an xtension property value.
        member this.TrySet<'a> propertyName (value : 'a) world =
            let property = { PropertyType = typeof<'a>; PropertyValue = value }
            World.trySetScreenXtensionProperty propertyName property this world

        /// Set an xtension property value.
        member this.Set<'a> propertyName (value : 'a) world =
            let property = { PropertyType = typeof<'a>; PropertyValue = value }
            World.setScreenXtensionProperty propertyName property this world

        /// Check that a screen is in an idling state (not transitioning in nor out).
        member this.IsIdling world = match this.GetTransitionState world with IdlingState _ -> true | _ -> false

        /// Check that a screen is selected.
        member this.IsSelected world =
            let gameState = World.getGameState world
            match gameState.OmniScreenOpt with
            | Some omniScreen when Address.head this.ScreenAddress = Address.head omniScreen.ScreenAddress -> true
            | _ ->
                match gameState.SelectedScreenOpt with
                | Some screen when Address.head this.ScreenAddress = Address.head screen.ScreenAddress -> true
                | _ -> false

        /// Check that a screen exists in the world.
        member this.Exists world = World.getScreenExists this world

        /// Check that a screen dispatches in the same manner as the dispatcher with the given type.
        member this.Is (dispatcherType, world) = Reflection.dispatchesAs dispatcherType (this.GetDispatcher world)

        /// Check that a screen dispatches in the same manner as the dispatcher with the given type.
        member this.Is<'a> world = this.Is (typeof<'a>, world)

        /// Get a screen's change event address.
        member this.GetChangeEvent propertyName = Events.Change propertyName --> this.ScreenAddress

        /// Try to signal a screen.
        member this.TrySignal signal world = (this.GetDispatcher world).TrySignal (signal, this, world)

    type World with

        static member internal updateScreen (screen : Screen) world =

            // update ecs
            let ecs = World.getScreenEcs screen world
            let world = ecs.Notify Ecs.EcsEvents.Update () world
            let world = ecs.Publish Ecs.EcsEvents.Update () world

            // update via dispatcher
            let dispatcher = World.getScreenDispatcher screen world
            let world = dispatcher.Update (screen, world)

            // publish update event
            let eventTrace = EventTrace.debug "World" "updateScreen" "" EventTrace.empty
            World.publishPlus () (Events.Update --> screen) eventTrace Simulants.Game false false world

        static member internal postUpdateScreen (screen : Screen) world =

            // post-update ecs
            let ecs = World.getScreenEcs screen world
            let world = ecs.Notify Ecs.EcsEvents.PostUpdate () world
            let world = ecs.Publish Ecs.EcsEvents.PostUpdate () world

            // post-update via dispatcher
            let dispatcher = World.getScreenDispatcher screen world
            let world = dispatcher.PostUpdate (screen, world)

            // publish post-update event
            let eventTrace = EventTrace.debug "World" "postUpdateScreen" "" EventTrace.empty
            World.publishPlus () (Events.PostUpdate --> screen) eventTrace Simulants.Game false false world

        static member internal renderScreen (screen : Screen) world =

            // render ecs
            let ecs = World.getScreenEcs screen world
            let world = ecs.Notify Ecs.EcsEvents.Render () world
            let world = ecs.Publish Ecs.EcsEvents.Render () world

            // render via dispatcher
            let dispatcher = screen.GetDispatcher world
            let world = dispatcher.Render (screen, world)

            // publish render event
            let eventTrace = EventTrace.debug "World" "renderScreen" "" EventTrace.empty
            World.publishPlus () (Events.Render --> screen) eventTrace Simulants.Game false false world

        /// Get all the screens in the world.
        [<FunctionBinding>]
        static member getScreens world =
            let simulants = World.getSimulants world
            match simulants.TryGetValue (Simulants.Game :> Simulant) with
            | (true, screensOpt) ->
                match screensOpt with
                | Some screens -> screens |> Seq.map cast<Screen>
                | None -> Seq.empty
            | (false, _) -> Seq.empty

        /// Set the dissolve properties of a screen.
        [<FunctionBinding>]
        static member setScreenDissolve dissolveDescriptor songOpt (screen : Screen) world =
            let dissolveImageOpt = Some dissolveDescriptor.DissolveImage
            let world = screen.SetIncoming { Transition.make Incoming with TransitionLifeTime = dissolveDescriptor.IncomingTime; DissolveImageOpt = dissolveImageOpt; SongOpt = songOpt } world
            let world = screen.SetOutgoing { Transition.make Outgoing with TransitionLifeTime = dissolveDescriptor.OutgoingTime; DissolveImageOpt = dissolveImageOpt; SongOpt = songOpt } world
            world

        /// Destroy a screen in the world immediately. Can be dangerous if existing in-flight publishing depends on the
        /// screen's existence. Consider using World.destroyScreen instead.
        static member destroyScreenImmediate (screen : Screen) world =
            let world = World.tryRemoveSimulantFromDestruction screen world
            EventSystemDelegate.cleanEventAddressCache screen.ScreenAddress
            if World.getScreenExists screen world then
                let groups = World.getGroups screen world
                let world = World.unregisterScreen screen world
                let world = World.removeTasklets screen world
                let world = World.destroyGroupsImmediate groups world
                World.removeScreenState screen world
            else world

        /// Destroy a screen in the world at the end of the current update.
        [<FunctionBinding>]
        static member destroyScreen (screen : Screen) world =
            World.addSimulantToDestruction screen world

        /// Create a screen and add it to the world.
        [<FunctionBinding "createScreen">]
        static member createScreen3 dispatcherName nameOpt world =
            let dispatchers = World.getScreenDispatchers world
            let dispatcher =
                match Map.tryFind dispatcherName dispatchers with
                | Some dispatcher -> dispatcher
                | None -> failwith ("Could not find ScreenDispatcher named '" + dispatcherName + "'.")
            let ecs = world.WorldExtension.Plugin.MakeEcs ()
            let screenState = ScreenState.make world.GameTime nameOpt dispatcher ecs
            let screenState = Reflection.attachProperties ScreenState.copy screenState.Dispatcher screenState world
            let screen = ntos screenState.Name
            let world =
                if World.getScreenExists screen world then
                    if screen.GetDestroying world
                    then World.destroyScreenImmediate screen world
                    else failwith ("Screen '" + scstring screen + "' already exists and cannot be created."); world
                else world
            let world = World.addScreen false screenState screen world
            (screen, world)

        /// Create a screen from a simulant descriptor.
        static member createScreen2 descriptor world =
            let (screen, world) =
                let screenNameOpt =
                    match descriptor.SimulantSurnamesOpt with
                    | None -> None
                    | Some [|name|] -> Some name
                    | Some _ -> failwith "Screen cannot have multiple names."
                World.createScreen3 descriptor.SimulantDispatcherName screenNameOpt world
            let world =
                List.fold (fun world (propertyName, property) ->
                    World.setScreenProperty propertyName property screen world |> snd')
                    world descriptor.SimulantProperties
            let world =
                List.fold (fun world childDescriptor ->
                    World.createGroup3 childDescriptor screen world |> snd)
                    world descriptor.SimulantChildren
            (screen, world)

        /// Create a screen and add it to the world.
        static member createScreen<'d when 'd :> ScreenDispatcher> nameOpt world =
            World.createScreen3 typeof<'d>.Name nameOpt world

        /// Create a screen with a dissolving transition, and add it to the world.
        [<FunctionBinding "createDissolveScreen">]
        static member createDissolveScreen5 dispatcherName nameOpt dissolveDescriptor songOpt world =
            let (screen, world) = World.createScreen3 dispatcherName nameOpt world
            let world = World.setScreenDissolve dissolveDescriptor songOpt screen world
            (screen, world)
        
        /// Create a screen with a dissolving transition, and add it to the world.
        static member createDissolveScreen<'d when 'd :> ScreenDispatcher> nameOpt dissolveDescriptor songOpt world =
            World.createDissolveScreen5 typeof<'d>.Name nameOpt dissolveDescriptor songOpt world

        /// Write a screen to a screen descriptor.
        static member writeScreen screen (screenDescriptor : ScreenDescriptor) world =
            let screenState = World.getScreenState screen world
            let screenDispatcherName = getTypeName screenState.Dispatcher
            let screenDescriptor = { screenDescriptor with ScreenDispatcherName = screenDispatcherName }
            let getScreenProperties = Reflection.writePropertiesFromTarget tautology3 screenDescriptor.ScreenProperties screenState
            let screenDescriptor = { screenDescriptor with ScreenProperties = getScreenProperties }
            let groups = World.getGroups screen world
            { screenDescriptor with GroupDescriptors = World.writeGroups groups world }

        /// Write multiple screens to a game descriptor.
        static member writeScreens screens world =
            screens |>
            Seq.sortBy (fun (screen : Screen) -> screen.GetOrder world) |>
            Seq.filter (fun (screen : Screen) -> screen.GetPersistent world) |>
            Seq.fold (fun screenDescriptors screen -> World.writeScreen screen ScreenDescriptor.empty world :: screenDescriptors) [] |>
            Seq.rev |>
            Seq.toList

        /// Write a screen to a file.
        [<FunctionBinding>]
        static member writeScreenToFile (filePath : string) screen world =
            let filePathTmp = filePath + ".tmp"
            let prettyPrinter = (SyntaxAttribute.defaultValue typeof<GameDescriptor>).PrettyPrinter
            let screenDescriptor = World.writeScreen screen ScreenDescriptor.empty world
            let screenDescriptorStr = scstring screenDescriptor
            let screenDescriptorPretty = PrettyPrinter.prettyPrint screenDescriptorStr prettyPrinter
            File.WriteAllText (filePathTmp, screenDescriptorPretty)
            File.Delete filePath
            File.Move (filePathTmp, filePath)

        /// Read a screen from a screen descriptor.
        static member readScreen screenDescriptor nameOpt world =

            // make the dispatcher
            let dispatcherName = screenDescriptor.ScreenDispatcherName
            let dispatchers = World.getScreenDispatchers world
            let dispatcher =
                match Map.tryFind dispatcherName dispatchers with
                | Some dispatcher -> dispatcher
                | None -> failwith ("Could not find a ScreenDispatcher named '" + dispatcherName + "'.")

            // make the ecs
            let ecs = world.WorldExtension.Plugin.MakeEcs ()

            // make the screen state and populate its properties
            let screenState = ScreenState.make world.GameTime None dispatcher ecs
            let screenState = Reflection.attachProperties ScreenState.copy screenState.Dispatcher screenState world
            let screenState = Reflection.readPropertiesToTarget ScreenState.copy screenDescriptor.ScreenProperties screenState

            // apply the name if one is provided
            let screenState =
                match nameOpt with
                | Some name -> { screenState with Name = name }
                | None -> screenState

            // add the screen's state to the world
            let screen = Screen (ntoa screenState.Name)
            let world = World.addScreen true screenState screen world
            
            // read the screen's groups
            let world = World.readGroups screenDescriptor.GroupDescriptors screen world |> snd
            (screen, world)

        /// Read multiple screens from a game descriptor.
        static member readScreens screenDescriptors world =
            let (screensRev, world) =
                List.fold
                    (fun (screens, world) screenDescriptor ->
                        let screenNameOpt = ScreenDescriptor.getNameOpt screenDescriptor
                        let (screen, world) = World.readScreen screenDescriptor screenNameOpt world
                        (screen :: screens, world))
                    ([], world)
                    screenDescriptors
            (List.rev screensRev, world)

        /// Read a screen from a file.
        [<FunctionBinding>]
        static member readScreenFromFile (filePath : string) nameOpt world =
            let screenDescriptorStr = File.ReadAllText filePath
            let screenDescriptor = scvalue<ScreenDescriptor> screenDescriptorStr
            World.readScreen screenDescriptor nameOpt world

        /// Apply a screen behavior to a screen.
        static member applyScreenBehavior setScreenSlide behavior (screen : Screen) world =
            match behavior with
            | Vanilla ->
                world
            | Dissolve (dissolveDescriptor, songOpt) ->
                World.setScreenDissolve dissolveDescriptor songOpt screen world
            | Slide (dissolveDescriptor, slideDescriptor, songOpt, destination) ->
                let world = World.setScreenDissolve dissolveDescriptor songOpt screen world
                setScreenSlide slideDescriptor destination screen world
            | OmniScreen ->
                World.setOmniScreen screen world