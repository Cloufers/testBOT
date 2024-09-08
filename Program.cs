namespace Bot
{
    using Telegram.Bot;

    internal class Program
    {
        private static void Main()
        {
            var client = new TelegramBotClient("6782678829:AAEod-hs_PM6yIBYta1eNapt9yIuSg_8tmQ");
            var taskManager = new TaskManager();
            var botHandler = new BotHandler(client, taskManager);

            client.StartReceiving(botHandler.Update, botHandler.Error);
            Console.ReadLine();
        }

        private static async Task StartNotificationService(ITelegramBotClient botClient, ITaskManager taskManager)
        {
            var botHandler = new BotHandler(botClient, taskManager);
            _ = botHandler.CheckTasksForNotifications();
        }
    }
}