using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Task_Manager;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<TaskItem> _tasks = new();
    private readonly ObservableCollection<TaskItem> _history = new();
    private readonly TaskRepository _repository;

    public MainWindow()
    {
        InitializeComponent();

        _repository = new TaskRepository();
        _repository.Initialize();

        DueDatePicker.SelectedDate = DateTime.Today;
        TaskListBox.ItemsSource = _tasks;
        HistoryListBox.ItemsSource = _history;

        LoadPersistedTasks();
        RefreshUrgentNotice();
    }

    private void LoadPersistedTasks()
    {
        _tasks.Clear();
        _history.Clear();

        foreach (var task in _repository.GetActiveTasks())
        {
            _tasks.Add(task);
        }

        foreach (var task in _repository.GetHistoryTasks())
        {
            _history.Add(task);
        }
    }

    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleTextBox.Text.Trim();
        var description = DescriptionTextBox.Text.Trim();
        var dueDate = DueDatePicker.SelectedDate ?? DateTime.Today;
        var priority = ((ComboBoxItem)PriorityComboBox.SelectedItem)?.Content?.ToString() ?? "Normal";

        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("Task title is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var task = new TaskItem
        {
            Title = title,
            Description = description,
            DueDate = dueDate,
            Priority = priority
        };

        _repository.AddTask(task);
        _tasks.Add(task);

        TitleTextBox.Clear();
        DescriptionTextBox.Clear();
        DueDatePicker.SelectedDate = DateTime.Today;
        PriorityComboBox.SelectedIndex = 0;

        RefreshUrgentNotice();
    }

    private void TaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TaskListBox.SelectedItem is not TaskItem selectedTask)
            return;

        var result = MessageBox.Show(
            $"Mark task as finished?\n\n{selectedTask.Title}\nDue: {selectedTask.DueDate:MMM dd, yyyy}\nPriority: {selectedTask.Priority}",
            "Complete Task",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _repository.MarkTaskComplete(selectedTask.Id);
            selectedTask.IsCompleted = true;
            selectedTask.CompletedAt = DateTime.Now;
            _history.Add(selectedTask);
            _tasks.Remove(selectedTask);
            TaskListBox.SelectedItem = null;
            RefreshUrgentNotice();
        }
    }

    private void RefreshUrgentNotice()
    {
        var urgentTasks = _tasks.Where(t => t.IsUrgent).ToList();

        if (urgentTasks.Count == 0)
        {
            UrgentNoticeText.Text = "No urgent tasks today.";
            return;
        }

        var names = string.Join(", ", urgentTasks.Select(t => t.Title));
        UrgentNoticeText.Text = $"Urgent tasks: {names}";
    }
}
