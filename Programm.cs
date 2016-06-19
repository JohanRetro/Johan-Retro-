using System;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions;
using Discord.Commands.Permissions.Levels;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RetroBot.Modules.Eval;
using Microsoft.CodeAnalysis;
using Discord.Modules;
using Discord.Audio;

namespace RetroBot_2._0
{
    class Program
    {
        public static DateTime startTime = DateTime.Now;

        static void Main(string[] args)
        {
            new Program().Start();
        }

        private DiscordClient bot;

        public void Start()
        {
            bot = new DiscordClient(x =>
            {
                x.AppName = "RetroBot";
                x.LogLevel = LogSeverity.Info;
                x.LogHandler = Log;
            }).UsingCommands(x =>
            {
                x.AllowMentionPrefix = true;
                x.HelpMode = HelpMode.Public;
                x.ExecuteHandler = OnCommandExecuted;
                x.ErrorHandler = OnCommandError;
                x.PrefixChar = Convert.ToChar("-");
            })
            .UsingAudio(x =>
            {
                x.Mode = AudioMode.Outgoing;
            })
           .UsingPermissionLevels((u, c) => (int)GetPermissions(u, c));

            Console.Title = "RetroBot 2.0";

            bot.JoinedServer += async (s, e) =>
            {
                await e.Server.DefaultChannel.SendMessage($"Hello! My name is RetroBot and I have just joined **{e.Server.Name}**. If you guys would like to know the commands of this bot, then simply just type -help in a server with me.");
                await e.Server.Owner.SendMessage($"Hi, I've just joined {e.Server.Name}! \n \n If you wanna view the RetroBot commands than simply just write in -help in a server with me, if you have any questions about RetroBot than join my server https://discord.gg/0tBYcqNmvDOr7ImX");
            };

            var evalBuilder = EvalModule.Builder.BuilderWithSystemAndLinq()
            .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(DiscordClient).Assembly.Location), "Discord"))
            .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(CommandEventArgs).Assembly.Location), "Discord.Commands"));

            bot.UsingModules();
            bot.AddModule(evalBuilder.Build((c, u, ch) => u.Id == 135463601371742208 || u.Id == 158964961471758337));


            var token = "";

            CreateCommand();

            bot.ExecuteAndWait(async () =>
            {
                await bot.Connect(token);
            });
        }
        private static PermissionLevel GetPermissions(User u, Channel c)
        {
            if (u.Id == 135463601371742208 || u.Id == 158964961471758337)
                return PermissionLevel.BotOwner;

            if (!c.IsPrivate)
            {
                if (u == c.Server.Owner)
                    return PermissionLevel.ServerOwner;

                var serverPerms = u.ServerPermissions;
                if (serverPerms.ManageRoles || u.Roles.Select(x => x.Name.ToLower()).Contains("Retromin")) // This doesnt work some fking reason
                    return PermissionLevel.ServerAdmin;
                if (serverPerms.ManageMessages && serverPerms.KickMembers && serverPerms.BanMembers)
                    return PermissionLevel.ServerModerator;

                var channelPerms = u.GetPermissions(c);
                if (channelPerms.ManagePermissions)
                    return PermissionLevel.ChannelAdmin;
                if (channelPerms.ManageMessages)
                    return PermissionLevel.ChannelModerator;
            }
            return PermissionLevel.User;
        }

        private void OnCommandError(object sender, CommandErrorEventArgs e)
        {
            string msg = e.Exception?.Message;
            if (msg == null) //No exception - show a generic message
            {
                switch (e.ErrorType)
                {
                    case CommandErrorType.Exception:
                        msg = "Unknown error.";
                        break;
                    case CommandErrorType.BadPermissions:
                        msg = "You do not have permission to run this command.";
                        break;
                    case CommandErrorType.BadArgCount:
                        msg = "You provided the incorrect number of arguments for this command.";
                        break;
                    case CommandErrorType.InvalidInput:
                        msg = "Unable to parse your command, please check your input.";
                        break;
                    case CommandErrorType.UnknownCommand:
                        msg = "Unknown command.";
                        break;
                }
            }
        }
        public void CreateCommand()
        {
            var music = bot.GetService<AudioService>();
            var client = bot.GetService<CommandService>();

            client.CreateCommand("Ping")
                .Alias("ping")
                .Alias("PING")
                .Description("Reply the message pong")
                .MinPermissions((int)PermissionLevel.ServerAdmin)

                .Do(async (e) =>
                {
                    await e.Channel.SendMessage("Pong!");
                });
            client.CreateCommand("Stats")
                .Alias("STATS")
                .Alias("stats")
                .Do(async e =>
                {
                    var delta = DateTime.Now - Program.startTime;
                    var uptimesec = delta.Seconds.ToString("n0");
                    var uptimemin = delta.Minutes.ToString("n0");
                    var uptimehour = delta.Hours.ToString("n0");
                    var uptimeday = delta.Days.ToString("n0");
                    var uptime = $"{uptimeday} day(s), {uptimehour} hour(s), {uptimemin} minute(s), {uptimesec} second(s)";

                    await e.Channel.SendMessage("```Uptime: " + $"{uptimeday} days, {uptimehour} hours, {uptimemin} minutes, {uptimesec} seconds " + "\n Connected to " + String.Join(", ", e.Server.Client.Servers.Count()) + " servers" + "\n Bot Id: 170921623564582912" + "\n Connected to: " + e.User.Client.Servers.Sum(x => x.AllChannels.Count()) + " channels. " + "\n All Commands: " + client.AllCommands.Count() + "\n Connected to: " + e.User.Client.Servers.Sum(x => x.VoiceChannels.Count()) + " voice channels. \n Connected to: " + e.User.Client.Servers.Sum(x => x.TextChannels.Count()) + " text channels." + "\n Seen " + e.User.Client.Servers.Sum(x => x.Users.Count()) + " users \n Library Discord.NET \n Bot owner: Johan Retro \n Bot Avatar: https://discordapp.com/api/users/170921623564582912/avatars/1f404a1dbcb856cf2257543a6e57a81a.jpg ```");
                });

            client.CreateCommand("kick")
                    .Description("Kicks a user from this server.")
                    .Parameter("user")
                    .Parameter("discriminator", ParameterType.Optional)
                    .MinPermissions((int)PermissionLevel.ServerAdmin)
                    .Do(async e =>
                    {
                        var user = await bot.FindUser(e, e.Args[0], e.Args[1]);
                        if (user == null) return;

                        await user.Kick();
                        await e.Channel.SendMessage("**" + e.User.Name + "** | Succesfully kicked " + user.Name);

                    });
            client.CreateCommand("ban")
                    .Description("bans a user from this server.")
                    .Parameter("user")
                    .Parameter("discriminator", ParameterType.Optional)
                    .MinPermissions((int)PermissionLevel.BotOwner)
                    .Do(async e =>
                    {
                        var user = await bot.FindUser(e, e.Args[0], e.Args[1]);
                        if (user == null) return;

                        await user.Server.Ban(user, pruneDays: 30);
                        await e.Channel.SendMessage("**" + e.User.Name + "** | Succesfully banned " + user.Name);
                    });

            client.CreateCommand("whois")
                  .Parameter("user")
                  .Parameter("discriminator", ParameterType.Optional)
                  .MinPermissions((int)PermissionLevel.BotOwner)
                  .Do(async e =>
                  {
                      var user = await bot.FindUser(e, e.Args[0], e.Args[1]);
                      if (user == null) return;
                      var nick = user.Nickname ?? "Nothing";
                      await e.Channel.SendMessage(" User information for **" + user.Name + "**" + "``` Name: " + user.Name + "\n Nickname: " + nick + "\n User ID: " + user.Id + "\n Discriminator: #" + user.Discriminator + "\n Status: " + user.Status + "\n Playing: " + user.CurrentGame + "\n Joined this server: " + e.Server.Name + ": " + user.JoinedAt + "\n Avatar: " + user.AvatarUrl + "\n Roles: " + String.Join(", ", user.Roles.Where(o => o != e.Server.EveryoneRole).Select(o => o.Name)) + "\n Last time online : " + user.LastOnlineAt + "```");

                  });
            client.CreateCommand("join")
                .Do(async (e) =>
                {
                    var voiceChannel = bot.FindServers("RetroBot Central").FirstOrDefault().VoiceChannels.FirstOrDefault(); // Finds the first VoiceChannel on the server 'Music Bot Server'

                    await music.Join(voiceChannel);

                    await e.Channel.SendMessage("It worked");
                });
        }

        private void OnCommandExecuted(object sender, CommandEventArgs e)
        {
            bot.Log.Info("Command", $"{e.Command.Text} ({e.User.Name})");

        }
        public void Log(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine($"[{e.Severity}] [{e.Source}] {e.Message}");

        }

    }

}

