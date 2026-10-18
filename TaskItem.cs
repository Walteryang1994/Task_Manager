using System;

namespace Task_Manager;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; } = DateTime.Today;
    public string Priority { get; set; } = "Normal";
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    public bool IsUrgent => !IsCompleted && DueDate.Date <= DateTime.Today.AddDays(1);
}
