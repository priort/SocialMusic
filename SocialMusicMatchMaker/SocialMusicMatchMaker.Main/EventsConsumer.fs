module EventsConsumer
open SocialMusicMatchMaker.Core.Domain.Events
open SocialMusicMatchMaker.Core.Domain

open Microsoft.Azure.ServiceBus
open Microsoft.Azure.ServiceBus.Core
open Newtonsoft.Json
open System

module Option = 
    let apply (f: ('a -> 'b) option) (a: 'a option) =
        match f, a with
        | Some f', Some a' -> Some (f' a')
        | _, _ -> None 

type MusicianRegisteredPropagationMessage = {
    timestamp : int64
    musicianName : string
    musicianLocation : string
    instrument : string    
}

type MusicianDeregisteredPropagationMessage = {
    timestamp : int64
    musicianName : string
    musicianLocation : string
    instrument : string    
}

type PropagationMessage = 
    | MusicianRegistered of MusicianRegisteredPropagationMessage
    | MusicianDeRegistered of MusicianDeregisteredPropagationMessage

let (|MusicianRegisteredEventMessage|_|) (message:Message) = 
    match message.Label with
    | "musicianRegistered" -> 
        try
            JsonConvert.DeserializeObject<MusicianRegisteredPropagationMessage>(System.Text.Encoding.UTF8.GetString(message.Body)) 
            |> MusicianRegistered
            |> Some
        with _ -> None
    | _ -> None

let (|MusicianDeregisteredEventMessage|_|) (message:Message) = 
    match message.Label with
    | "musicianDeregistered" -> 
        try
            JsonConvert.DeserializeObject<MusicianDeregisteredPropagationMessage>(System.Text.Encoding.UTF8.GetString(message.Body)) 
            |> MusicianDeRegistered
            |> Some
        with _ -> None
    | _ -> None

module Instrument = 
    
    let fromString instrument =
        match instrument with
        | "DoubleBass" -> Some DoubleBass
        | "ElectricBass" -> Some ElectricBass 
        | "Drums" -> Some Drums
        | "Piano" -> Some Piano
        | "Guitar" -> Some Guitar
        | "Sax" -> Some Sax
        | "Trumpet" -> Some Trumpet
        | "Trombone" -> Some Trombone 
        | _ -> None 

module Location = 
    
    let fromString location = 
        match location with
        | "Limerick" -> Some Limerick
        | "Belfast" -> Some Belfast
        | "Galway" -> Some Galway
        | "Dublin" -> Some Dublin
        | _ -> None
 

let convertToEvent (message:Message) =

    let (<*>) = Option.apply
    
    match message with
    | MusicianRegisteredEventMessage (MusicianRegistered propagationMessage) -> 
         Some (Event.createMusicianRegistered propagationMessage.timestamp)
         <*> (Name.create propagationMessage.musicianName)
         <*> (Instrument.fromString propagationMessage.instrument)
         <*> (Location.fromString propagationMessage.musicianLocation)
        
    | MusicianDeregisteredEventMessage (MusicianDeRegistered propagationMessage) ->
         Some (Event.createMusicianDeregistered propagationMessage.timestamp)
         <*> (Name.create propagationMessage.musicianName)
         <*> (Instrument.fromString propagationMessage.instrument)
         <*> (Location.fromString propagationMessage.musicianLocation)
    | _ -> None 
    
type ServiceBusConsumer(connectionString, queueName, projectEvent: Event -> unit) =

    let messageReceiver = new MessageReceiver(connectionString, queueName, ReceiveMode.ReceiveAndDelete)
    do messageReceiver.RegisterMessageHandler(
        (fun message _ -> 
                System.Threading.Tasks.Task.Run (fun _ -> 
                        printfn "Event message received %s" (System.Text.Encoding.UTF8.GetString(message.Body))
                        let event = convertToEvent message
                        do event |> Option.iter projectEvent
                )),
        (fun _ -> System.Threading.Tasks.Task.Run (fun _ -> printfn "event consumer failure") )) 
