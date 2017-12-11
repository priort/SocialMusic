module SocialMusicMatchMaker.Core.Domain
open System

type Name = private Name of string

module Name = 
    let create (name:string) = 
        if String.IsNullOrWhiteSpace(name) then 
            None 
        else 
            Some (Name name)
    
    let toString (Name name) = name 
    
let (|Name|) (Name name) = Name name

type Instrument =
    | DoubleBass
    | ElectricBass 
    | Drums
    | Piano 
    | Guitar
    | Sax 
    | Trumpet
    | Trombone

module Instrument =
    let toString = function
        | DoubleBass -> "Double bass"
        | ElectricBass ->  "Electric bass"
        | Drums -> "Drums" 
        | Piano -> "Piano" 
        | Guitar -> "Guitar" 
        | Sax -> "Sax" 
        | Trumpet -> "Trumpet" 
        | Trombone -> "Trombone"  
            
type Location = 
    | Tipperary
    | Limerick
    | Belfast
    | Galway
    | Dublin

type Musician  = {
    Name : Name
    Instrument : Instrument
}

module Events = 
    
    type Event = 
        | MusicianRegistered of Timestamp:int64 * Location * Musician
        | MusicianDeregistered of Timestamp:int64 * Location * Musician
    
    module Event = 
        let createMusicianRegistered timestamp (name:Name) (instrument:Instrument) (location:Location) = 
            MusicianRegistered (timestamp, location, { Name = name; Instrument = instrument })
        
        let createMusicianDeregistered timestamp (name:Name) (instrument:Instrument) (location:Location) = 
            MusicianDeregistered (timestamp, location, { Name = name; Instrument = instrument })