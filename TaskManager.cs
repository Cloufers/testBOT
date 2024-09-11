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

        public void RegisterUser(long chatId, string firstName, string lastName)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                    INSERT INTO users (chat_id, first_name, last_name)
                    VALUES (@chatId, @firstName, @lastName)
                    ON CONFLICT (chat_id) DO UPDATE
                    SET first_name = EXCLUDED.first_name, last_name = EXCLUDED.last_name;
                ";
                    cmd.Parameters.AddWithValue("chatId", chatId);
                    cmd.Parameters.AddWithValue("firstName", firstName);
                    cmd.Parameters.AddWithValue("lastName", lastName);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void AddTask(long chatId, TaskItem task)
        {
            RegisterUser(chatId, "", "");

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

        public TaskItem GetTask(long chatId, string taskName)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                    SELECT name, due_date, importance, description, comments, links FROM tasks
                    WHERE user_id = (SELECT id FROM users WHERE chat_id = @chatId) AND name = @name;
                ";
                    cmd.Parameters.AddWithValue("chatId", chatId);
                    cmd.Parameters.AddWithValue("name", taskName);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new TaskItem
                            {
                                Name = reader.GetString(0),
                                DueDate = reader.GetDateTime(1),
                                Importance = reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Comments = reader.IsDBNull(4) ? new List<string>() : reader.GetFieldValue<string[]>(4).ToList(),
                                Links = reader.IsDBNull(5) ? new List<string>() : reader.GetFieldValue<string[]>(5).ToList()
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void UpdateTaskDetails(long chatId, string taskName, string field, string value)
        {
            Console.WriteLine($"Updating task: ChatId={chatId}, TaskName={taskName}, Field={field}, Value={value}");
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;

                    string updateSql = "";
                    if (field == "description")
                    {
                        updateSql = "UPDATE tasks SET description = @value WHERE user_id = (SELECT id FROM users WHERE chat_id = @chatId) AND name = @name";
                    }
                    else if (field == "comments" || field == "links")
                    {
                        updateSql = $"UPDATE tasks SET {field} = COALESCE({field}, ARRAY[]::text[]) || @value::text WHERE user_id = (SELECT id FROM users WHERE chat_id = @chatId) AND name = @name";
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid field: {field}");
                    }

                    cmd.CommandText = updateSql;
                    cmd.Parameters.AddWithValue("chatId", chatId);
                    cmd.Parameters.AddWithValue("name", taskName);
                    cmd.Parameters.AddWithValue("value", value);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    Console.WriteLine($"Rows affected: {rowsAffected}");

                    if (rowsAffected == 0)
                    {
                        throw new Exception($"Failed to update task '{taskName}' for chat ID {chatId}. Task may not exist.");
                    }
                }
            }
        }
    }
}