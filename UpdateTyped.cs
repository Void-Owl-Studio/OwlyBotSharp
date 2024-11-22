using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Owl;

class TypedUpdate : Update, IUpdateMsg<UpdateType> {}
