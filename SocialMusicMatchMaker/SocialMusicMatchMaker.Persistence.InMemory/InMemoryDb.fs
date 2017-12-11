module SocialMusicMatchMaker.Peristence.InMemory

open System.Collections.Generic
open System.Threading
open SocialMusicMatchMaker.Core.Domain
open SocialMusicMatchMaker.Core.Persistence

type Timestamp = int64

let private inMemoryDb = new Dictionary<Location, Set<Musician * Timestamp>>()

let private performDbTransaction f = 
    
    Monitor.Enter inMemoryDb
    try
        f inMemoryDb
    finally
        Monitor.Exit inMemoryDb

let private removeMusician timestamp location musician =
    performDbTransaction <| fun db ->
        if db.ContainsKey(location) then
            let musicians = db.[location]
            musicians
            |> Seq.tryFind (fun (m, t) -> musician = m && timestamp > t)
            |> Option.iter (fun (m, t) -> 
                db.[location] <- Set.remove (m, t) musicians)
            
let private addMusician timestamp location musician = 
    performDbTransaction <| fun db ->
        if db.ContainsKey(location) then
            let musicians = db.[location]
            if musicians 
            |> Set.exists (fun (m, t) -> musician = m && timestamp > t) |> not then
               db.[location] <- Set.add (musician, timestamp) musicians
        else 
            db.Add(location, Set.ofList [ (musician,timestamp) ])

let private getMusiciansForLocation location = 
    if inMemoryDb.ContainsKey(location) then
        inMemoryDb.[location]
        |> Set.map fst |> Set.toList
    else
        []
        
let db = {
    RemoveMusician = removeMusician
    AddMusician = addMusician
    GetMusiciansForLocation = getMusiciansForLocation
}