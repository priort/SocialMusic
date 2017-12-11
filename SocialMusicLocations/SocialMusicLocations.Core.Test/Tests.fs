module Tests

open System
open Xunit
open FsUnit.Xunit
open SocialMusicLocations.Core.Domain
open SocialMusicLocations.Core.Domain.Commands
open SocialMusicLocations.Core.Domain.Errors
open SocialMusicLocations.Core.Domain.Events
open SocialMusicLocations.Core.Domain.State
open SocialMusicLocations.Core.CommandHandler
open SocialMusicLocations.Core.StateGeneration

let unregisteredMusician = { Name = "Paul Chambers"; Instrument = DoubleBass }
let registeredMusician = { Name = "Paul Chambers"; Instrument = DoubleBass; Location = Limerick }

[<Fact>]
let ``Empty location - registering a musician - occupied location, count 1, MusicianRegistered event.`` () =
    let initialState = EmptyLocation Limerick
    let result = handle initialState <| RegisterMusician (unregisteredMusician, Limerick)
    let expected = (OccupiedLocation { Location = Limerick; MusicianCount = 1 },
                    [ MusicianRegistered registeredMusician ])
    match result with
    | Ok r -> r |> should equal expected
    | Error _ -> failwith "Result of RegisterMusician should not be Error."

[<Fact>]
let ``Occupied location - registering a musician - occupied location, count + 1, MusicianRegistered event.`` () =
    let initialState = OccupiedLocation { Location = Limerick; MusicianCount = 3 }
    let result = handle initialState <| RegisterMusician (unregisteredMusician, Limerick)
    let expected = (OccupiedLocation { Location = Limerick; MusicianCount = 4 },
                    [ MusicianRegistered registeredMusician ])
    match result with
    | Ok r -> r |> should equal expected
    | Error _ -> failwith "Result of RegisterMusician should not be Error."
    
[<Fact>]
let ``Empty location - deregistering a musician - error`` () =
    let initialState = EmptyLocation Limerick
    let result = handle initialState <| DeregisterMusician registeredMusician
    match result with
    | Error error -> error |> should equal CannotDeregisterFromEmptyLocation
    | Ok r -> failwith "Result of DeregisterMusician from empty location should be an error."

[<Fact>]
let ``Occupied location with count 1 - deregistering a musician - empty location, MusicianDeregistered event.`` () =
    let initialState = OccupiedLocation { Location = Limerick; MusicianCount = 1 }
    let result = handle initialState <| DeregisterMusician registeredMusician
    let expected = (EmptyLocation Limerick,
                    [ MusicianDeregistered (Limerick, unregisteredMusician) ])
    match result with
    | Ok r -> r |> should equal expected
    | Error _ -> failwith "Result of DeregisterMusician should not be Error."
    
[<Fact>]
let ``Occupied location with count not 1 - deregistering a musician - occupied location, count - 1, MusicianDeregistered event.`` () =
    let initialState = OccupiedLocation { Location = Limerick; MusicianCount = 3 }
    let result = handle initialState <| DeregisterMusician registeredMusician
    let expected = (OccupiedLocation { Location = Limerick; MusicianCount = 2 },
                    [ MusicianDeregistered (Limerick, unregisteredMusician) ])
    match result with
    | Ok r -> r |> should equal expected
    | Error _ -> failwith "Result of DeregisterMusician should not be Error."