using Discord;
using Discord.Interactions;

namespace Mutterblack.App.Modules;

[IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
public class InviteModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("invite", "Get an invite link to add this bot to your server!")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task GetInviteAsync()
    {
        var inviteLink = $"https://discord.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot%20applications.commands";
        var inviteText = $"Please visit <{inviteLink}> to add {Context.Client.CurrentUser.Username} to your server.";

        await RespondAsync(inviteText, ephemeral: true);
    }
}
