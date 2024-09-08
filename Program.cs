namespace Bot
{
    using Npgsql;
    using Telegram.Bot;
    using Telegram.BotBuilder.CalendarPicker;

    internal class Program
    {
        private static void Main()
        {
            var client = new TelegramBotClient("6782678829:AAEod-hs_PM6yIBYta1eNapt9yIuSg_8tmQ");
            string connectionString = "Host=localhost;Database=taskmanagerdb;Username=postgres;Password=papi123";
            var taskManager = new TaskManager(connectionString);
            var botHandler = new BotHandler(client, taskManager);
            CheckDatabaseConnection(connectionString);

            client.StartReceiving(botHandler.Update, botHandler.Error);
            Console.ReadLine();
        }

        private static void CheckDatabaseConnection(string connectionString)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    Console.WriteLine("Database connection successful!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
            }
        }

        private static async Task StartNotificationService(ITelegramBotClient botClient, ITaskManager taskManager)
        {
            var botHandler = new BotHandler(botClient, taskManager);
            _ = botHandler.CheckTasksForNotifications();
        }
    }
}