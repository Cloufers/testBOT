namespace Bot
{
    public class TaskState
    {
        public string Name { get; set; }
        public DateTime? DueDate { get; set; }
        public string Importance { get; set; }
        public string CurrentStep { get; set; }
    }
}