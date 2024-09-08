namespace Bot
{
    public interface ITaskManager
    {
        void RegisterUser(long chatId);

        void AddTask(long chatId, TaskItem task);

        void RemoveAllTasks(long chatId);

        void RemoveTask(long chatId, TaskItem task);

        List<TaskItem> GetTasks(long chatId);

        List<TaskItem> GetNearestTasks(long chatId);

        public IEnumerable<long> GetAllChatIds();
    }
}