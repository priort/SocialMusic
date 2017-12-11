module SocialMusicMatchMaker.Core.Projections
open Domain.Events
open SocialMusicMatchMaker.Core.Persistence

let projectEvent (dataStore:DataStore) event =
    match event with
    | MusicianRegistered(timestamp, location, musician) -> 
        dataStore.AddMusician timestamp location musician
    | MusicianDeregistered (timestamp, location, musician) -> 
        dataStore.RemoveMusician timestamp location musician
