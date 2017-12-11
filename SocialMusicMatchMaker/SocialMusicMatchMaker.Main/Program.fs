open EventsConsumer
open System

open SocialMusicMatchMaker.Core.Projections
open SocialMusicMatchMaker.Core.Domain
open SocialMusicMatchMaker.Peristence
open Suave
open Suave.Operators
open Suave.Successful
open Suave.Filters
open Newtonsoft.Json
open SocialMusicMatchMaker.Core.Persistence

[<EntryPoint>]
let main argv =
    
    let connectionString =
        "Endpoint=sb://social-music-locations-events.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=fsYImmnLpT43Bxd/0XrBA151gGhv9z7v5FtrKWfOFmY="
        
    let queueName = "locationevents"
    
    let eventsConsumer = new ServiceBusConsumer(connectionString, queueName, projectEvent InMemory.db)

    let handleGetMusiciansForLocation location (db:DataStore) httpContext = async {
            let musicians = db.GetMusiciansForLocation location 
            let response = 
                JsonConvert.SerializeObject(
                    musicians 
                    |> List.map (fun musician -> 
                        Map.ofList [ "name", musician.Name |> Name.toString 
                                     "instrument", musician.Instrument |> Instrument.toString ]))
            return! Successful.OK response httpContext
        }
    
    let app = GET >=> pathScan "/location/%s/musicians" (fun location -> 
        warbler (fun _ -> 
            match Location.fromString location with
            | Some location -> 
                handleGetMusiciansForLocation location InMemory.db
            | None -> 
                RequestErrors.BAD_REQUEST ("invalid location " + location) ))  
    startWebServer { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8083 ] } app
    
    0
