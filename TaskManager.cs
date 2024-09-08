using Npgsql;

namespace Bot
{
    public class TaskManager : ITaskManager
    {
        private readonly Dictionary<long, List<TaskItem>> tasks = new Dictionary<long, List<TaskItem>>();
        private readonly string? connectionString;

        public TaskManager(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void RegisterUser(long chatId)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                    INSERT INTO users (chat_id)
                    VALUES (@chatId)
                    ON CONFLICT (chat_id) DO NOTHING;
                ";
                    cmd.Parameters.AddWithValue("chatId", chatId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void AddTask(long chatId, TaskItem task)
        {
            RegisterUser(chatId);

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                        INSERT INTO tasks (user_id, name, due_date, importance)
                        VALUES ((SELECT id FROM users WHERE chat_id = @chatId), @name, @dueDate, @importance);
                    ";
                    cmd.Parameters.AddWithValue("chatId", chatId);
                    cmd.Parameters.AddWithValue("name", task.Name);
                    cmd.Parameters.AddWithValue("dueDate", task.DueDate);
                    cmd.Parameters.AddWithValue("importance", task.Importance);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<TaskItem> GetTasks(long chatId)
        {
            var tasks = new List<TaskItem>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                        SELECT name, due_date, importance FROM tasks
                        WHERE user_id = (SELECT id FROM users WHERE chat_id = @chatId);
                    ";
                    cmd.Parameters.AddWithValue("chatId", chatId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new TaskItem
                            {
                                Name = reader.GetString(0),
                                DueDate = reader.GetDateTime(1),
                                Importance = reader.GetString(2)
                            });
                        }
                    }
                }
            }
            return tasks;
        }

        public List<TaskItem> GetNearestTasks(long chatId)
        {
            var tasks = new List<TaskItem>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                        SELECT name, due_date, importance FROM tasks
                        WHERE user_id = (SELECT id FROM users WHERE chat_id = @chatId) AND due_date >= @today
                        ORDER BY due_date
                        LIMIT 5;
                    ";
                    cmd.Parameters.AddWithValue("chatId", chatId);
                    cmd.Parameters.AddWithValue("today", DateTime.Today);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new TaskItem
                            {
                                Name = reader.GetString(0),
                                DueDate = reader.GetDateTime(1),
                                Importance = reader.GetString(2)
                            });
                        }
                    }
                }
            }
            return tasks;
        }

        public void RemoveAllTasks(long chatId)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                        DELETE FROM tasks
                        WHERE user_id = (SELECT id FROM users WHERE chat_id = @chatId);
                    ";
                    cmd.Parameters.AddWithValue("chatId", chatId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void RemoveTask(long chatId, TaskItem task)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                        DELETE FROM tasks
                        WHERE user_id = (SELECT id FROM users WHERE chat_id = @chatId) AND name = @name;
                    ";
                    cmd.Parameters.AddWithValue("chatId", chatId);
                    cmd.Parameters.AddWithValue("name", task.Name);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<long> GetAllChatIds()
        {
            var chatIds = new List<long>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT chat_id FROM users;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            chatIds.Add(reader.GetInt64(0));
                        }
                    }
                }
            }
            return chatIds;
        }
    }
}