public class ConsoleSpinner
{
    private int _counter;
    private bool _active;
    private readonly Task _task;

    public ConsoleSpinner()
    {
        _task = Task.Run(() => Spin());
    }

    public void Start()
    {
        _active = true;
    }

    public void Stop()
    {
        _active = false;
        Console.Write("");
        _task.Wait();
    }

    private void Spin()
    {
        while (_active)
        {
            switch (_counter % 4)
            {
                case 0:
                    Console.Write("/");
                    break;
                case 1:
                    Console.Write("-");
                    break;
                case 2:
                    Console.Write("\\");
                    break;
                case 3:
                    Console.Write("|");
                    break;
            }

            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            _counter++;
            Thread.Sleep(50);
        }
    }
}