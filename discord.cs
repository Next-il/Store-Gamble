using Discord;
using Discord.Webhook;

namespace Store_Logs;

public class DiscordLog()
{
	public static void DiscordEmbed(DiscordWebhookClient client, string title, string description, Color color)
	{
		var embed = new EmbedBuilder
		{
			Title = title,
			Description = description,
			Color = color,
		};

		client.SendMessageAsync(embeds: [embed.Build()]);
	}
}
