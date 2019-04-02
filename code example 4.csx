
public class FlipCounter
{
    private readonly int desiredSampleLength;

    private int currentCount;
    private bool isInSilentMode;

    public FlipCounter(int desiredSampleLength)
    {
        this.desiredSampleLength = desiredSampleLength;
    }

    public double Process(double input)
    {
        if (this.currentCount >= this.desiredSampleLength)
        {
            this.currentCount = 0;
            this.isInSilentMode = !this.isInSilentMode;
        }
        else
        {
            this.currentCount++;
        }

        return this.isInSilentMode ? 0.0 : input;
    }
}

// public static class ProcessingFactories
// {
public static Func<double> FlipCounter(int desiredSampleLength)
{
    var state = new
    {
        count = 0,
        isInSilentMode = false
    };

    return new Func<double>(() =>
    {
        state =
            state.count >= desiredSampleLength
            ? new
                {
                    count = 0,
                    isInSilentMode = false
                }
            : new
                {
                    count = state.count + 1,
                    isInSilentMode = state.isInSilentMode
                };

        return state.isInSilentMode ? 0.0 : 1.0;
    });
}