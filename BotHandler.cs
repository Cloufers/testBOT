using Telegram.Bot.Types;
using Telegram.Bot;

namespace Bot
{
    public class BotHandler
    {
        private readonly ITelegramBotClient botClient;
        private readonly ITaskManager taskManager;
        private readonly string connectionString;
        private static Dictionary<long, TaskState> taskStates = new Dictionary<long, TaskState>();

        public BotHandler(ITelegramBotClient botClient, ITaskManager taskManager)
        {
            this.botClient = botClient;
            this.taskManager = taskManager;
        }

        public async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;

            if (message.Text != null)
            {
                Console.WriteLine($"{message.Chat.FirstName} {message.Chat.LastName} | {message.Text}"); // logs

                if (message.Text.StartsWith("/addtask"))
                {
                    await AddTask(message);
                    return;
                }

                if (message.Text.StartsWith("/showtasks"))
                {
                    await ShowTasks(message);
                    return;
                }

                if (message.Text.StartsWith("/shownearest"))
                {
                    await ShowNearestTasks(message);
                    return;
                }

                if (message.Text.StartsWith("/deletetask"))
                {
                    await DeleteTask(message);
                    return;
                }

                if (message.Text.ToLower().Contains("ресурс") || message.Text.ToLower().Contains("jiafei"))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Hello sweetheart");
                    return;
                }

                // Check if user is in the middle of adding a task
                if (taskStates.ContainsKey(message.Chat.Id))
                {
                    await AddTask(message);
                    return;
                }
            }

            if (message.Document != null)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Accepter wait for review");

                var fileId = update.Message.Document.FileId;
                var fileInfo = await botClient.GetFileAsync(fileId, token);
                var filePath = fileInfo.FilePath;
                var chatID = message.Chat.Id.ToString();

                string userFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), chatID);
                Directory.CreateDirectory(userFolderPath);

                string destinationFilePath = Path.Combine(userFolderPath, message.Document.FileName);

                await using Stream fileStream = System.IO.File.Create(destinationFilePath);
                await botClient.DownloadFileAsync(
                    filePath: filePath,
                    destination: fileStream,
                    cancellationToken: token);

                return;
            }

            if (message.Dice != null)
            {
                var diceValue = message.Dice.Value;
                await botClient.SendTextMessageAsync(message.Chat.Id, $"You threw dice with value:  {diceValue}");
            }
        }

        private async Task AddTask(Message message)
        {
            if (!taskStates.ContainsKey(message.Chat.Id))
            {
                taskStates[message.Chat.Id] = new TaskState { CurrentStep = "Name" };
                await botClient.SendTextMessageAsync(message.Chat.Id, "Please enter the title of the task:");
            }
            else
            {
                var state = taskStates[message.Chat.Id];

                switch (state.CurrentStep)
                {
                    case "Name":
                        state.Name = message.Text;
                        state.CurrentStep = "DueDate";
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Please enter the deadline date (yyyy-MM-dd):");
                        break;

                    case "DueDate":
                        if (!DateTime.TryParse(message.Text, out var date))
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Invalid date format. Please use yyyy-MM-dd.");
                            return;
                        }
                        state.DueDate = date;
                        state.CurrentStep = "Importance";
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Please enter the importance (red, blue, green):");
                        break;

                    case "Importance":
                        var importance = message.Text.ToLower();
                        if (importance != "red" && importance != "blue" && importance != "green")
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Invalid importance. Please use red, blue, or green.");
                            return;
                        }
                        state.Importance = importance;

                        var task = new TaskItem
                        {
                            Name = state.Name,
                            DueDate = state.DueDate.Value,
                            Importance = state.Importance
                        };

                        taskManager.AddTask(message.Chat.Id, task);
                        taskStates.Remove(message.Chat.Id);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Task added successfully.");
                        break;
                }
            }
        }

        private async Task ShowTasks(Message message)
        {
            // Retrieve the list of tasks from the task manager
            var taskList = taskManager.GetTasks(message.Chat.Id);

            // Notify the user if there are no tasks
            if (taskList.Count == 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "No tasks found.");
                return;
            }

            // Format and display the task list
            var response = string.Join("\n", taskList.Select(t => $"{t.Name} - {t.DueDate.ToShortDateString()} - {t.Importance}"));
            await botClient.SendTextMessageAsync(message.Chat.Id, response);
        }

        private async Task ShowNearestTasks(Message message)
        {
            // Retrieve the list of tasks from the task manager
            var taskList = taskManager.GetTasks(message.Chat.Id);

            // Notify the user if there are no tasks
            if (taskList.Count == 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "No tasks found.");
                return;
            }

            // Sort tasks by the nearest due date
            var nearestTasks = taskList.OrderBy(t => t.DueDate).Take(5);

            // Format and display the nearest tasks
            var response = string.Join("\n", nearestTasks.Select(t => $"{t.Name} - {t.DueDate.ToShortDateString()} - {t.Importance}"));
            await botClient.SendTextMessageAsync(message.Chat.Id, response);
        }

        private async Task DeleteTask(Message message)
        {
            var parts = message.Text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Usage: /deletetask <name>");
                return;
            }

            var name = parts[1];
            var tasks = taskManager.GetTasks(message.Chat.Id);
            var taskToRemove = tasks.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (taskToRemove == null)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Task not found.");
                return;
            }

            tasks.Remove(taskToRemove);
            await botClient.SendTextMessageAsync(message.Chat.Id, "Task deleted successfully.");
        }

        protected internal async Task CheckTasksForNotifications()
        {
            while (true)
            {
                foreach (var chatId in taskManager.GetAllChatIds())
                {
                    var tasks = taskManager.GetTasks(chatId);
                    var tasksToNotify = tasks.Where(t => t.DueDate.Date == DateTime.Today.AddDays(1)).ToList();

                    foreach (var task in tasksToNotify)
                    {
                        await botClient.SendTextMessageAsync(chatId, $"Reminder: Task '{task.Name}' is due tomorrow!");
                    }
                }

                // Wait for 24 hours before checking again
                await Task.Delay(TimeSpan.FromHours(24));
            }
        }

        public Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine($"Error: {exception.Message}");
            return Task.CompletedTask;
            throw new NotImplementedException();
        }
    }
}