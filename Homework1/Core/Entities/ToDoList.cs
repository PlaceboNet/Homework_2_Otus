namespace Homework1.Core.Entities
{
    public class ToDoList
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public ToDoUser? User { get; set; }
        public List<ToDoItem>? Tasks { get; set; }
    }
}
