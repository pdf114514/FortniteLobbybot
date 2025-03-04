﻿using System.Text.Json;
using FortniteCS;
using FortniteAuthBase = FortniteCS.AuthBase<FortniteCS.FortniteAuthSession, FortniteCS.FortniteAuthData>;

namespace Lobbybot;

public class Program {
    async static Task Main(string[] args) {
        FortniteAuthBase auth;
        if (File.Exists("deviceAuth.json")) {
            auth = new DeviceAuth(JsonSerializer.Deserialize<DeviceAuthObject>(File.ReadAllText("deviceAuth.json"))!);
        } else {
            Console.WriteLine("Enter your authorization code:");
            auth = new AuthorizationCodeAuth(Console.ReadLine()!);
        }
        await using var client = new FortniteClient(auth);
        client.Ready += async () => {
            Console.WriteLine($"Ready! {client.User.AccountId} / {client.User.DisplayName}");
            if (!File.Exists("deviceAuth.json")) {
                File.WriteAllText("deviceAuth.json", JsonSerializer.Serialize(await client.Session.CreateDeviceAuth()));
            }
            // Console.WriteLine(FortniteUtils.JsonSerialize(client.Party?.Members[client.User.AccountId].Meta));
        };
        // client.FriendRequestReceived += async friend => await client.AccpetFriendRequest(friend);
        client.FriendMessage += async m => {
            Console.WriteLine($"Message from {m.Friend.DisplayName}: {m.Message}");
            if (m.Message == "hi") {
                Console.WriteLine("Sending message back");
                await m.Friend.SendMessage($"Hello, {m.Friend.DisplayName}!");
            }
        };
        client.FriendPresence += presence => Console.WriteLine($"Presence: {presence.DisplayName} / {presence.Status}");
        client.PartyInvite += async invite => await client.AcceptInvite(invite);
        client.PartyJoinRequest += async request => await client.AcceptJoinRequest(request);
        client.PartyJoinConfirmation += confirmation => Console.WriteLine($"Someone is going to join the party: {confirmation.DisplayName}");
        client.PartyMemberJoined += member => {
            Console.WriteLine($"{member.DisplayName} joined the party!");
            client.Party?.SendMessage($"Hello {(member.AccountId == client.User.AccountId ? "I'm here" : member.DisplayName)}!");
        };
        client.PartyMemberLeft += member => Console.WriteLine($"{member.DisplayName} left the party!");
        client.PartyMemberEmoteUpdated += async member => await client.Party!.Me.SendPatch(new() { ["Default:FrontendEmote_j"] = member.Meta["Default:FrontendEmote_j"] }); // copys emote :D
        await client.Start();
        Console.WriteLine("Press enter to exit");
        Console.ReadLine();
    }
}

// Sample client class
public class MyFortniteClient(FortniteAuthBase auth) : FortniteClient(auth) {
    [FortniteClientEvent(FortniteClientEvent.Ready)]
    public async Task OnReady() {
        Console.WriteLine($"Ready! {User.AccountId} / {User.DisplayName}");
        if (!File.Exists("deviceAuth.json")) {
            File.WriteAllText("deviceAuth.json", JsonSerializer.Serialize(await Session.CreateDeviceAuth()));
        }
    }

    [FortniteClientEvent(FortniteClientEvent.FriendPresence)]
    public void OnFriendPresence(FortnitePresence presence) {
        Console.WriteLine($"Presence: {presence.DisplayName} / {presence.Status}");
    }

    [FortniteClientEvent(FortniteClientEvent.PartyInvite)]
    public async Task OnPartyInvite(FortnitePartyInvite invite) {
        await AcceptInvite(invite);
    }

    [FortniteClientEvent(FortniteClientEvent.PartyMemberJoined)]
    public void OnPartyMemberJoined(FortnitePartyMember member) {
        Console.WriteLine($"{member.DisplayName} joined the party!");
        Party?.SendMessage($"Hello {(member.AccountId == User.AccountId ? "I'm here" : member.DisplayName)}!");
    }

    [FortniteClientEvent(FortniteClientEvent.PartyMemberLeft)]
    public void OnPartyMemberLeft(FortnitePartyMember member) {
        Console.WriteLine($"{member.DisplayName} left the party!");
    }

    [FortniteClientEvent(FortniteClientEvent.PartyMemberEmoteUpdated)]
    public async Task OnPartyMemberEmoteUpdated(FortnitePartyMember member) {
        await Party!.Me.SendPatch(new() { ["Default:FrontendEmote_j"] = member.Meta["Default:FrontendEmote_j"] });
    }
}