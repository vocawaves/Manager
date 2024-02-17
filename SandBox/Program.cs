using AsyncAwaitBestPractices;

namespace SandBox;

internal class Program
{
    public static event AsyncEventHandler? TestEvent;

    public static async Task Main(string[] args)
    {
        TestEvent += async (sender, eventArgs) =>
        {
            await TestMethod(1, _iteration);
            _iteration++;
        };
        TestEvent += async (sender, eventArgs) =>
        {
            await Task.Delay(5000);
            await TestMethod(2, _iteration2);
            _iteration2++;
        };

        for (int i = 0; i < 20; i++)
        {
            TestEvent.InvokeAsync(null!, EventArgs.Empty).SafeFireAndForget();
            Console.WriteLine("after");
            await Task.Delay(1000);
        }
        Console.ReadLine();
    }

    private static int _iteration = 0;
    private static int _iteration2 = 0;

    private static ValueTask TestMethod(int num, int iter)
    {
        Console.WriteLine(num + " Iteration: " + iter);
        //Console.WriteLine(Stopwatch.GetTimestamp());
        return ValueTask.CompletedTask;
    }
}