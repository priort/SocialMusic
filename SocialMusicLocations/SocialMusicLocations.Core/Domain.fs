namespace SocialMusicLocations.Core.Domain


type Location = 
    | Tipperary
    | Limerick
    | Belfast
    | Galway
    | Dublin

type Instrument =
    | DoubleBass
    | ElectricBass 
    | Drums
    | Piano 
    | Guitar
    | Sax 
    | Trumpet
    | Trombone

type RegisteredMusician = {
    Name : string
    Location : Location
    Instrument : Instrument
}

type UnRegisteredMusician = {
    Name : string
    Instrument : Instrument
}

type Musician = 
    | RegisteredMusician of RegisteredMusician
    | UnRegisteredMusician of UnRegisteredMusician
    
module Commands =

    type Command =
        | RegisterMusician of UnRegisteredMusician * Location
        | DeregisterMusician of RegisteredMusician

module Events = 
    
    type Event =
        | MusicianRegistered of RegisteredMusician
        | MusicianDeregistered of PreviousLocation:Location * UnRegisteredMusician
        
module State = 
    
    type LocationDetails = {
        Location : Location
        MusicianCount : int
    }
    
    type State = 
        | EmptyLocation of Location
        | OccupiedLocation of LocationDetails
        
module Errors = 

    type Error = CannotDeregisterFromEmptyLocation
    
    let toString = function | CannotDeregisterFromEmptyLocation -> "Cannot deregister from an empty location"
    