module SocialMusicLocations.Core.StateGeneration

open SocialMusicLocations.Core.Domain.Events
open SocialMusicLocations.Core.Domain.State

let apply state event = 
    match state, event with
    | OccupiedLocation locationDetails, MusicianDeregistered _ when locationDetails.MusicianCount = 1 ->
        EmptyLocation locationDetails.Location
    | OccupiedLocation l, MusicianDeregistered _ ->
        OccupiedLocation { l with MusicianCount = l.MusicianCount - 1 }
    | EmptyLocation location, MusicianRegistered _ ->
        OccupiedLocation { Location = location; MusicianCount = 1 }
    | OccupiedLocation locationDetails, MusicianRegistered _ ->
        OccupiedLocation { locationDetails with MusicianCount = locationDetails.MusicianCount + 1 }
    | _ -> state