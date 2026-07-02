using UnityEngine;

public enum ExpressiveMusicAttribute
{
    Happy,
    Sad,
    Mysterious,
    Heroic,
    Tender,
    Dark,
    Energetic,
    Calm,
    Threatening,
    Magical,
    Childlike,
    Epic
}

public enum MusicalRootNote
{
    C,
    CSharp,
    D,
    DSharp,
    E,
    F,
    FSharp,
    G,
    GSharp,
    A,
    ASharp,
    B
}

public enum MusicalMode
{
    Major,
    Minor
}

public enum MusicalMeter
{
    FourFour,
    ThreeFour,
    SixEight
}

public enum TrackRole
{
    Melody,
    Bass,
    Harmony1,
    Harmony2,
    Harmony3,
    Harmony4,
    Kick,
    Snare,
    HiHat
}

public enum OSCGroupRole
{
    Melody,
    Harmony,
    Bass,
    Kick,
    Snare,
    HiHat
}

public enum ChordDegree
{
    I,
    II,
    III,
    IV,
    V,
    VI,
    VII
}

public enum DrumType
{
    Kick,
    Snare,
    HiHat
}

public enum ChordQuality
{
    Major,
    Minor,
    Diminished,
    Suspended
}

public enum AccompanimentType
{
    Sustained,
    Pulsed,
    Arpeggiated,
    Sparse,
    Driving
}

public enum DrumPatternStyle
{
    Minimal,
    Basic,
    Active,
    March,
    Waltz,
    SixEight,
    DarkSparse
}

public static class MusicalMeterUtility
{
    public static float GetBeatsPerBar(MusicalMeter meter)
    {
        switch (meter)
        {
            case MusicalMeter.ThreeFour:
                return 3f;
            case MusicalMeter.SixEight:
                return 6f;
            default:
                return 4f;
        }
    }

    public static string GetDisplayName(MusicalMeter meter)
    {
        switch (meter)
        {
            case MusicalMeter.ThreeFour:
                return "3/4";
            case MusicalMeter.SixEight:
                return "6/8";
            default:
                return "4/4";
        }
    }

    public static bool IsStrongBeat(MusicalMeter meter, float beatInBar)
    {
        float roundedBeat = Mathf.Round(beatInBar * 100f) / 100f;

        if (Mathf.Approximately(roundedBeat, 0f))
            return true;

        switch (meter)
        {
            case MusicalMeter.FourFour:
                return Mathf.Approximately(roundedBeat, 2f);
            case MusicalMeter.SixEight:
                return Mathf.Approximately(roundedBeat, 3f);
            default:
                return false;
        }
    }
}
