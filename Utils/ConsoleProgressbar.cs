public class ConsoleProgressBar
{
    private readonly int _barSize;
    private readonly char _progressCharacter;
    private readonly int _startCursorTop;
    private readonly int _total;
    private int _current;

    public ConsoleProgressBar(int total, int barSize = 50, char progressCharacter = '#')
    {
        _total = total;
        _current = 0;
        _barSize = barSize;
        _progressCharacter = progressCharacter;
        _startCursorTop = Console.CursorTop;
    }

    public void Increment()
    {
        _current++;
        Draw();
    }

    private void Draw()
    {
        Console.SetCursorPosition(0, _startCursorTop);

        if (_current < _total)
        {
            float progress = (float)_current / _total;
            int progressWidth = (int)(_barSize * progress);

            Console.Write("[");
            Console.Write(new string(_progressCharacter, progressWidth));
            Console.Write(new string('-', _barSize - progressWidth));
            Console.Write("] ");

            int percentage = (int)(progress * 100);
            Console.Write($"{percentage}% Completed");
        }
        else
        {
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, _startCursorTop);
        }
    }
}