// Learn more about F# at http://fsharp.org

open System
open SocialMusicLocations.EventStore
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Json
open System.Runtime.Serialization
open Newtonsoft.Json
open System.Text
open SocialMusicLocations.Core.Domain.Commands
open SocialMusicLocations.Core.Domain.State
open SocialMusicLocations.Core.Domain.Events
open SocialMusicLocations.Control
open SocialMusicLocations.Core.Domain

type UnvalidatedCommand = {
    command : string
    name : string 
    instrument : string
    location : string     
}

module CommandValidation =

    type ValidationResult<'a> = Success of 'a | Error of string list
    
    type ValidCommandType = Register | DeRegister
   
    module ValidationResult =
        let apply (f: ValidationResult<'a -> 'b>) (validationResult: ValidationResult<'a>) =
            match f, validationResult with
            | Success f', Success validationResult -> Success <| f' validationResult
            | Success _, Error errors -> Error errors
            | Error errorsA, Error errorsB -> Error <| errorsA @ errorsB
            | Error errors, Success validationResult -> Error errors
     
    let validateCommandType (commandType:string) = 
        match commandType with
        | "registerMusician" -> Success Register
        | "deRegisterMusician" -> Success DeRegister
        | _ -> Error <| [ sprintf "commandType, %s, is not a valid command type" commandType ]
        
    let validateMusicianName (name:string) = 
        if String.IsNullOrWhiteSpace name then 
            Error [ "Musician name cannot be blank" ] 
        else 
            Success name
            
    let validateInstrument (instrument:string) =
        match instrument with 
        | "DoubleBass" -> Success DoubleBass
        | "ElectricBass" -> Success ElectricBass 
        | "Drums" -> Success Drums
        | "Piano" -> Success Piano 
        | "Guitar" -> Success Guitar
        | "Sax" -> Success Sax 
        | "Trumpet" -> Success Trumpet
        | "Trombone" -> Success Trombone
        | _ -> Error [ sprintf "instrument, %s, is not a valid instrument" instrument ]
    
    let validateLocation (location:string) =
        match location with
        | "Tipperary" -> Success Tipperary
        | "Limerick" -> Success Limerick
        | "Belfast" -> Success Belfast
        | "Galway" -> Success Galway
        | "Dublin" -> Success Dublin
        | _ -> Error [ sprintf "location, %s, is not a valid location" location ]
        

    let toCommand (commandType:ValidCommandType) (name:string) (instrument:Instrument) (location:Location) = 
            match commandType with
            | Register ->
                RegisterMusician ({ Name = name; Instrument = instrument }, location)
            | DeRegister ->
                DeregisterMusician ({ Name = name; Instrument = instrument; Location = location })
        
    let validateCommand unvalidatedCommand = 
        let (<*>) = ValidationResult.apply
        Success toCommand
        <*> validateCommandType unvalidatedCommand.command
        <*> validateMusicianName unvalidatedCommand.name
        <*> validateInstrument unvalidatedCommand.instrument
        <*> validateLocation unvalidatedCommand.location
        
open CommandValidation
open Suave.RequestErrors
        
type ErrorResponse = {
    errors : string
}

type SuccessfulResponse = {
    state : State
    events : Event list
}

let handleCommand (agent:Agent) ctx = async {
    
    let requestJson = System.Text.Encoding.UTF8.GetString(ctx.request.rawForm)
    let validationResult = 
        JsonConvert.DeserializeObject<UnvalidatedCommand>(requestJson)
        |> validateCommand
    match validationResult with
    | Success command ->
        let agentResponse = agent.HandleCommand(command)
        match agentResponse with
        | AgentResponse.Success (state, events) ->
            let response = { state = state; events = events } |> JsonConvert.SerializeObject
            return! Successful.OK response ctx
        | AgentResponse.Failure error -> 
            let response =  error |> Errors.toString |> JsonConvert.SerializeObject
            return! RequestErrors.UNPROCESSABLE_ENTITY response ctx
    | Error errors -> 
        let response = { errors = String.Join("; ", errors |> List.toArray) } |> JsonConvert.SerializeObject
        return! RequestErrors.UNPROCESSABLE_ENTITY response ctx
}

[<EntryPoint>]
let main argv =
    let agent = Agent( InMemoryEventStore.createInMemoryEventStore(), 
                       "Endpoint=sb://social-music-locations-events.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=fsYImmnLpT43Bxd/0XrBA151gGhv9z7v5FtrKWfOFmY=",
                       "locationevents")
    let app = POST >=> path "/locations/command" >=> warbler (fun _ -> handleCommand agent)
    startWebServer defaultConfig app
    0
