module SocialMusicLocations.Core.CommandHandler

open SocialMusicLocations.Core.Domain
open Domain.Commands
open Domain.Events
open Domain.State
open Domain.Errors

let registerMusician unRegisteredMusician state = 
    match state with
    | EmptyLocation location ->
        Ok [ MusicianRegistered { 
                Name = unRegisteredMusician.Name
                Location = location
                Instrument = unRegisteredMusician.Instrument } ]
    | OccupiedLocation locationDetails -> 
        Ok [ MusicianRegistered { 
                Name = unRegisteredMusician.Name
                Location = locationDetails.Location
                Instrument = unRegisteredMusician.Instrument } ]
             
let deregisterMusician (registeredMusician:RegisteredMusician) state = 
    match state with
    | EmptyLocation locationDetails -> Error CannotDeregisterFromEmptyLocation 
    | OccupiedLocation locationDetails -> 
        Ok [ MusicianDeregistered (locationDetails.Location, {
                Name = registeredMusician.Name
                Instrument = registeredMusician.Instrument }) ]

let handle (state:State) (command:Command) =
    let eventsResult = 
        match command with
        | RegisterMusician (unRegisteredMusician, _) ->
            registerMusician unRegisteredMusician state
        | DeregisterMusician registeredMusician ->
            deregisterMusician registeredMusician state
    eventsResult 
    |> Result.map (fun events ->
        let newState = events |> List.fold StateGeneration.apply state
        newState, events)
        