namespace Bot
{
    public class TaskItem
    {
        public string Name { get; set; }
        public DateTime DueDate { get; set; }
        public string Importance { get; set; }
        public string Description { get; set; }
        public List<string> Comments { get; set; } // Changed to List<string>
        public List<string> Links { get; set; } // Changed to List<string>
    }
}