namespace SocialMusicMatchMaker.Core.Persistence
open SocialMusicMatchMaker.Core.Domain

type DataStore = {
    RemoveMusician : int64 -> Location -> Musician -> unit
    AddMusician : int64 -> Location -> Musician -> unit
    GetMusiciansForLocation : Location -> Musician list
}