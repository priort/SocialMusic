namespace SocialMusicLocations.Control

open Microsoft.Azure.ServiceBus
open Newtonsoft.Json
open SocialMusicLocations.Core
open SocialMusicLocations.Core.Domain
open SocialMusicLocations.Core.Domain.Events
open SocialMusicLocations.Core.Domain.Errors
open SocialMusicLocations.Core.Domain.State
open SocialMusicLocations.Core.Domain.Commands
open SocialMusicLocations.Core.CommandHandler
open SocialMusicLocations.EventStore
open SocialMusicLocations.EventStore.InMemoryEventStore
open System


type AgentResponse = 
    | Success of State * Event list
    | Failure of Error

type private AgentMessage = 
    | PostCommand of Command * AsyncReplyChannel<AgentResponse>
    | Stop

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

module Instrument =
    let toString = function 
        | DoubleBass -> "DoubleBass"
        | ElectricBass -> "ElectricBass" 
        | Drums -> "Drums"
        | Piano -> "Piano"
        | Guitar -> "Guitar"
        | Sax -> "Sax" 
        | Trumpet -> "Trumpet"
        | Trombone -> "Trombone"
        
module Location = 
    let toString = function
        | Tipperary -> "Tipperary"
        | Limerick -> "Limerick"
        | Belfast -> "Belfast"
        | Galway -> "Galway"
        | Dublin -> "Dublin"

type Agent(eventStore:Store, connectionString:string, queueName:string) = 
    
    let locationFromCommand = function
        | RegisterMusician (_, location) -> location
        | DeregisterMusician registeredMusician -> registeredMusician.Location
        
    let propagateEvent event = async {
        let queueClient = new QueueClient(connectionString, queueName);
        let (messageLabel, messageBody) = 
            match event with
            | MusicianRegistered registeredMusician ->
                    "musicianRegistered", JsonConvert.SerializeObject {
                        timestamp = DateTime.UtcNow.Ticks
                        musicianName = registeredMusician.Name
                        instrument = registeredMusician.Instrument |> Instrument.toString
                        musicianLocation = registeredMusician.Location |> Location.toString }
            | MusicianDeregistered (previousLocation, unRegisteredMusician) ->
                    "musicianDeregistered", JsonConvert.SerializeObject {
                        timestamp = DateTime.UtcNow.Ticks
                        musicianName = unRegisteredMusician.Name
                        musicianLocation = Location.toString previousLocation
                        instrument = Instrument.toString unRegisteredMusician.Instrument }

        let message = new Message(System.Text.Encoding.UTF8.GetBytes(messageBody))
        message.Label <- messageLabel
        try
            do! queueClient.SendAsync(message) |> Async.AwaitTask
        finally
            queueClient.CloseAsync() |> Async.AwaitTask |> Async.RunSynchronously
    }
    
    let propagateEvents = List.map propagateEvent
    
    let agent = MailboxProcessor<AgentMessage>.Start <| fun self ->
                
        let rec loop () = async {
            let! message = self.Receive()
            match message with
            | PostCommand (command, replyChannel) ->
                let currentState = eventStore.GenerateState(locationFromCommand command)
                let eventsResult = handle currentState command
                match eventsResult with
                | Ok (state, events) ->
                    eventStore.SaveEvents state events
                    events |> propagateEvents |> List.iter Async.Start
                    replyChannel.Reply (Success (state, events))
                | Error error -> replyChannel.Reply (Failure error)
                return! loop()
            | Stop -> return ()    
        }
        loop ()
    
    member x.Stop() = agent.Post Stop
    
    member x.HandleCommand(command:Command) = 
        let createMessage replyChannel = PostCommand (command, replyChannel)
        agent.PostAndReply(createMessage)
        
    
