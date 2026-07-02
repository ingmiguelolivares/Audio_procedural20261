using System;

[Serializable]
public class MusicGenerationSettings
{
    public ExpressiveMusicAttribute selectedAttribute = ExpressiveMusicAttribute.Happy;
    public MusicalMeter meter = MusicalMeter.FourFour;
    public int bpm = 100;
    public bool overrideProfileTempoAndMeter = true;
    public int barsPartA = 4;
    public int barsPartB = 4;
    public int randomSeed = 12345;
    public bool useRandomSeed = true;
    public ExpressiveMusicProfile generatedProfile;
}
