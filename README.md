# Social Music Platform

Includes two F# .NET Core 2.0 solutions:

SocialMusicLocations and SocialMusicMatchMaker

This is prototype system to capture the movement of musicians between locations asynchronously using CQRS, distributed event propagation and  F# async  features. The idea is that consumers of musician location events can update read models - for example, SocialMusicMatchMaker is a read model showing musicians per location so that a client app could suggest possible groups of musicians that could meet up and have jam sessions.

