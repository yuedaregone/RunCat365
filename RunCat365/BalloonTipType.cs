using RunCat365.Properties;

namespace RunCat365
{
    internal readonly struct BalloonTipInfo
    {
        internal string Title { get; }
        internal string Text { get; }

        internal BalloonTipInfo(string title, string text)
        {
            Title = title;
            Text = text;
        }
    }

    internal enum BalloonTipType
    {
        AppLaunched,
        TomatoClockCompleted,
    }

    internal static class BalloonTipTypeExtension
    {
        internal static BalloonTipInfo GetInfo(this BalloonTipType balloonTipType)
        {
            return balloonTipType switch
            {
                BalloonTipType.AppLaunched => new BalloonTipInfo("RunCat 365", Strings.Message_AppLaunched),
                BalloonTipType.TomatoClockCompleted => new BalloonTipInfo("RunCat 365", Strings.Message_TomatoClockCompleted),
                _ => new BalloonTipInfo("RunCat 365", string.Empty),
            };
        }
    }
}
