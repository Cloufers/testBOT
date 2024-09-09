using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Text;

namespace Bot
{
    public class BotHandler
    {
        private readonly ITelegramBotClient botClient;
        private readonly ITaskManager taskManager;
        private readonly string connectionString;
        private DateTime currentDate = DateTime.Now;
        private static Dictionary<long, TaskState> taskStates = new Dictionary<long, TaskState>();

        public BotHandler(ITelegramBotClient botClient, ITaskManager taskManager)
        {
            this.botClient = botClient;
            this.taskManager = taskManager;
        }

        public async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                await HandleMessage(update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQuery(update.CallbackQuery);
            }
        }

        private async Task HandleMessage(Message message)
        {
            Console.WriteLine($"{message.Chat.FirstName} {message.Chat.LastName} | {message.Text}"); // logs

            if (message.Text.StartsWith("/"))
            {
                await HandleCommand(message);
            }
            else if (taskStates.ContainsKey(message.Chat.Id))
            {
                await AddTask(message);
            }
        }

        private async Task HandleCommand(Message message)
        {
            switch (message.Text.Split(' ')[0])
            {
                case "/addtask":
                    await StartAddTask(message);
                    break;

                case "/showtasks":
                    await ShowTasks(message);
                    break;

                case "/shownearest":
                    await ShowNearestTasks(message);
                    break;

                case "/deletetask":
                    await DeleteTask(message);
                    break;

                case "/start":
                    var welcomeMessage = @"Welcome to the Task Manager Bot! 🚀

Here are the available commands:

/addtask - Create a new task.
/showtasks - Show all existing tasks.
/shownearest - Show tasks with the nearest deadlines.
/cancel - Cancel task creation process.
/done - Mark task as completed.

Let's get started! 🎉";
                    await botClient.SendTextMessageAsync(message.Chat.Id, welcomeMessage);
                    break;

                case "/cancel":
                    await HandleCancelCommand(message);
                    break;

                case "/done":
                    await HandleDoneCommand(message);
                    break;

                default:
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Unknown command.");
                    break;
            }
        }

        private async Task StartAddTask(Message message)
        {
            taskStates[message.Chat.Id] = new TaskState { CurrentStep = "Name" };
            await botClient.SendTextMessageAsync(message.Chat.Id, "Please enter the title of the task:");
        }

        private async Task AddTask(Message message)
        {
            var state = taskStates[message.Chat.Id];

            switch (state.CurrentStep)
            {
                case "Name":
                    state.Name = message.Text;
                    state.CurrentStep = "DueDate";
                    await SendCalendar(message.Chat.Id);
                    break;

                case "DueDate":
                    // This case will be handled by the callback query handler
                    break;

                case "Importance":
                    await SendImportanceKeyboard(message.Chat.Id);
                    break;
            }
        }

        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            if (data.StartsWith("date_"))
            {
                await HandleDateSelection(chatId, data);
            }
            else if (data.StartsWith("importance_"))
            {
                await HandleImportanceSelection(chatId, data);
            }
            else if (data.StartsWith("prev_") || data.StartsWith("next_"))
            {
                await HandleMonthNavigation(chatId, callbackQuery.Message.MessageId, data);
            }
        }

        private async Task HandleDateSelection(long chatId, string data)
        {
            var dateString = data.Substring(5);
            if (DateTime.TryParse(dateString, out var date))
            {
                var state = taskStates[chatId];
                state.DueDate = date;
                state.CurrentStep = "Importance";
                await SendImportanceKeyboard(chatId);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Invalid date. Please try again.");
            }
        }

        private async Task HandleImportanceSelection(long chatId, string data)
        {
            var importance = data.Substring(11);
            var state = taskStates[chatId];
            state.Importance = importance;

            var task = new TaskItem
            {
                Name = state.Name,
                DueDate = state.DueDate.Value,
                Importance = state.Importance
            };

            taskManager.AddTask(chatId, task);
            taskStates.Remove(chatId);
            await botClient.SendTextMessageAsync(chatId, "Task added successfully.");
        }

        private async Task HandleMonthNavigation(long chatId, int messageId, string data)
        {
            if (data.StartsWith("prev_"))
            {
                currentDate = currentDate.AddMonths(-1);
            }
            else if (data.StartsWith("next_"))
            {
                currentDate = currentDate.AddMonths(1);
            }

            var keyboard = new InlineKeyboardMarkup(GenerateCalendar());
            await botClient.EditMessageReplyMarkupAsync(chatId, messageId, keyboard);
        }

        private async Task SendCalendar(long chatId)
        {
            var keyboard = new InlineKeyboardMarkup(GenerateCalendar());
            await botClient.SendTextMessageAsync(chatId, "Пожалуйста, выберите дату:", replyMarkup: keyboard);
        }

        private InlineKeyboardButton[][] GenerateCalendar()
        {
            var buttons = new List<InlineKeyboardButton[]>();

            // Добавляем строку с месяцем и годом
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("<<", $"prev_{currentDate:yyyy-MM}"),
        InlineKeyboardButton.WithCallbackData($"{currentDate:MMMM yyyy}", $"month_{currentDate:yyyy-MM}"),
        InlineKeyboardButton.WithCallbackData(">>", $"next_{currentDate:yyyy-MM}")
    });

            // Добавляем строку с названиями дней недели
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("Вс", "ignore"),
        InlineKeyboardButton.WithCallbackData("Пн", "ignore"),
        InlineKeyboardButton.WithCallbackData("Вт", "ignore"),
        InlineKeyboardButton.WithCallbackData("Ср", "ignore"),
        InlineKeyboardButton.WithCallbackData("Чт", "ignore"),
        InlineKeyboardButton.WithCallbackData("Пт", "ignore"),
        InlineKeyboardButton.WithCallbackData("Сб", "ignore")
    });

            // Генерируем кнопки с датами
            var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);

            var currentRow = new List<InlineKeyboardButton>();
            for (int i = 0; i < (int)firstDayOfMonth.DayOfWeek; i++)
            {
                currentRow.Add(InlineKeyboardButton.WithCallbackData(" ", "ignore"));
            }

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(currentDate.Year, currentDate.Month, day);
                currentRow.Add(InlineKeyboardButton.WithCallbackData(
                    day.ToString(),
                    $"date_{date:yyyy-MM-dd}"
                ));

                if (currentRow.Count == 7 || day == daysInMonth)
                {
                    buttons.Add(currentRow.ToArray());
                    currentRow = new List<InlineKeyboardButton>();
                }
            }

            return buttons.ToArray();
        }

        private async Task SendImportanceKeyboard(long chatId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("🔴 Красный", "importance_red"),
                    InlineKeyboardButton.WithCallbackData("🔵 Синий", "importance_blue"),
                    InlineKeyboardButton.WithCallbackData("🟢 Зеленый", "importance_green"),
                }
            });

            await botClient.SendTextMessageAsync(chatId, "Please select the importance:", replyMarkup: keyboard);
        }

        private async Task ShowTasks(Message message)
        {
            var taskList = taskManager.GetTasks(message.Chat.Id);

            if (taskList.Count == 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "У вас пока нет задач. Используйте /addtask, чтобы добавить новую задачу.");
                return;
            }

            var groupedTasks = taskList.GroupBy(t => t.Importance)
                                       .OrderBy(g => g.Key == "red" ? 0 : g.Key == "blue" ? 1 : 2);

            var response = new StringBuilder("📋 Ваши задачи:\n\n");

            foreach (var group in groupedTasks)
            {
                string emoji = group.Key == "red" ? "🔴" : group.Key == "blue" ? "🔵" : "🟢";
                response.AppendLine($"{emoji} {char.ToUpper(group.Key[0]) + group.Key.Substring(1)}:");

                foreach (var task in group.OrderBy(t => t.DueDate))
                {
                    string dueDate = task.DueDate.ToString("dd.MM.yyyy");
                    response.AppendLine($"  • {task.Name} (до {dueDate})");
                }

                response.AppendLine();
            }

            await botClient.SendTextMessageAsync(message.Chat.Id, response.ToString());
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

        private async Task HandleCancelCommand(Message message)
        {
            if (taskStates.ContainsKey(message.Chat.Id))
            {
                taskStates.Remove(message.Chat.Id);
                await botClient.SendTextMessageAsync(message.Chat.Id, "Task creation canceled.");
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "No task creation in progress.");
            }
        }

        private async Task HandleDoneCommand(Message message)
        {
            var parts = message.Text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Usage: /done <Task Name>");
                return;
            }

            var taskName = parts[1];
            var tasks = taskManager.GetTasks(message.Chat.Id);
            var taskToMarkDone = tasks.FirstOrDefault(t => t.Name.Equals(taskName, StringComparison.OrdinalIgnoreCase));

            if (taskToMarkDone == null)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Task not found.");
                return;
            }

            taskManager.RemoveTask(message.Chat.Id, taskToMarkDone);
            await botClient.SendTextMessageAsync(message.Chat.Id, $"Task '{taskToMarkDone.Name}' marked as done and removed from the list.");
        }
    }
}