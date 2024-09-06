namespace Bot
{
    public class TaskManager : ITaskManager
    {
        private readonly Dictionary<long, List<TaskItem>> tasks = new Dictionary<long, List<TaskItem>>();

        public void AddTask(long chatId, TaskItem task)
        {
            if (!tasks.ContainsKey(chatId))
            {
                tasks[chatId] = new List<TaskItem>();
            }
            tasks[chatId].Add(task);
        }

        public List<TaskItem> GetTasks(long chatId)
        {
            return tasks.ContainsKey(chatId) ? tasks[chatId] : new List<TaskItem>();
        }

        public List<TaskItem> GetNearestTasks(long chatId)
        {
            if (!tasks.ContainsKey(chatId))
            {
                return new List<TaskItem>();
            }

            return tasks[chatId]
                .Where(t => t.DueDate >= DateTime.Today)
                .OrderBy(t => t.DueDate)
                .Take(5)
                .ToList();
        }

        public void RemoveAllTasks(long chatId)
        {
            if (tasks.ContainsKey(chatId))
            {
                tasks[chatId].Clear(); // Clears all tasks for the specified chatId
            }
        }

        public void RemoveTask(long chatId, TaskItem task)
        {
            if (tasks.ContainsKey(chatId))
            {
                tasks[chatId].Remove(task); // Removes the specific task for the chatId
            }
        }
    }
}