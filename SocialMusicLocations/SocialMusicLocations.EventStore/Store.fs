namespace SocialMusicLocations.EventStore

open System
open NEventStore
open SocialMusicLocations.Core
open SocialMusicLocations.Core.Domain
open SocialMusicLocations.Core.Domain.Events
open SocialMusicLocations.Core.Domain.State

type Store = {
    SaveEvents : State -> Event list -> unit
    GenerateState : Location -> State
}

module InMemoryEventStore = 
    
    let private locationFromState = function 
        | EmptyLocation location -> location
        | OccupiedLocation locationDetails -> locationDetails.Location
    
    let private locationToId = function
        | Tipperary -> "tipperary"
        | Limerick -> "limerick"
        | Belfast -> "belfast"
        | Galway -> "galway"
        | Dublin -> "dublin"
        
    let createInMemoryEventStore () = 
        let eventStore = Wireup.Init().UsingInMemoryPersistence().Build()
        
        let saveEvent state (event:Event) =
            let location = locationFromState state
            use stream = eventStore.OpenStream(locationToId location)
            stream.Add(new EventMessage(Body = event))
            stream.CommitChanges(Guid.NewGuid())
        
        let saveEvents state (events:seq<Event>) = 
            events |> Seq.iter (saveEvent state)
        
        let generateState location = 
            use stream = eventStore.OpenStream(locationToId location)
            let events = 
                stream.CommittedEvents
                |> Seq.map(fun e -> e.Body)
                |> Seq.cast<Event>
            events
            |> Seq.fold (StateGeneration.apply) (EmptyLocation location)
            
        { SaveEvents = saveEvents
          GenerateState = generateState }
