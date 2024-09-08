using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.BotBuilder.CalendarPicker.Interfaces;

namespace Bot
{
    internal class CalendarHandler : ICalendarHandler
    {
        public async Task HandlePickedDateAsync(
      ITelegramBotClient context,
      Message message,
      DateTime pickedDate,
      CancellationToken cancellationToken
      )
        {
            await context.SendTextMessageAsync(
                message.Chat,
                $"PickedDate: {pickedDate:d}",
                cancellationToken: cancellationToken
                );
        }
    }
}