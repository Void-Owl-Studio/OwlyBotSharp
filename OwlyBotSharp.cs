using Telegram.Bot.Types.Enums;
using Owl;

using var ur = new UpdateRouter<TUpdateRunner, TypedUpdate, UpdateType>();
var telerunner = new TUpdateRunner();

ur.Start(telerunner);

Console.WriteLine("Bot is running... Press Enter to terminate");

Console.ReadLine();
