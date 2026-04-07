using RunCat365.Properties;

namespace RunCat365
{
    public enum Runner
    {
        Cat,
        Parrot,
        Horse,
    }

    internal static class RunnerExtension
    {
        internal static string GetString(this Runner runner)
        {
            return runner switch
            {
                Runner.Cat => "Cat",
                Runner.Parrot => "Parrot",
                Runner.Horse => "Horse",
                _ => "",
            };
        }

        internal static string GetLocalizedString(this Runner runner)
        {
            return runner switch
            {
                Runner.Cat => Strings.Runner_Cat,
                Runner.Parrot => Strings.Runner_Parrot,
                Runner.Horse => Strings.Runner_Horse,
                _ => "",
            };
        }

        internal static int GetFrameNumber(this Runner runner)
        {
            return runner switch
            {
                Runner.Cat => 5,
                Runner.Parrot => 10,
                Runner.Horse => 5,
                _ => 0,
            };
        }
    }
}
