using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

public abstract class ParameterRange<T> : IEnumerable<T>, IParameterRange
{
    protected const int MaxSteps = 10000;
    public string Name { get; set; } = "Unnamed Parameter";

    public string CurrentValue => Current.ToString();


    public abstract bool IsRange { get; }

    public T Current { get; protected set; }

    public abstract IEnumerator<T> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IParameterRange.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        return $"{Name}: {Current}";
    }
}


public interface IParameterRange
{
    string Name { get; }
    string CurrentValue { get; }
    bool IsRange { get; }
    IEnumerator GetEnumerator();
}

[System.Serializable]
public class BoolParameterSet : ParameterRange<bool>
{
    public bool On;
    public bool Off = true;

    public override bool IsRange => On && Off;

    public override IEnumerator<bool> GetEnumerator()
    {
        Assert.IsTrue(On || Off, "BoolParameterSet must use either on or off");
        if (On)
        {
            Current = true;
            yield return true;
        }

        if (Off)
        {
            Current = false;
            yield return false;
        }
    }
}

[System.Serializable]
public class FloatParameterRange : ParameterRange<float>
{
    public float Min = 1;
    public float Max = 1;
    public float StepSize = 1;

    public override bool IsRange => ((Max - Min) / StepSize) >= 1;


    public override IEnumerator<float> GetEnumerator()
    {
        Assert.IsTrue((Max - Min) / StepSize < MaxSteps, $"Settings for {Name} would result in more than {MaxSteps} steps.");
        for (float val = Min; val <= Max; val += StepSize)
        {
            // It is kind of sketchy to set STATE from the enumerator
            Current = val;
            yield return val;
        }
    }

    public static implicit operator FloatParameterRange(float value)
    {
        return new FloatParameterRange {Min = value, Max = value, StepSize = 1};
    }
}

[System.Serializable]
public class IntParameterRange : ParameterRange<int>
{
    public int Min = 1;
    public int Max = 1;
    public int StepSize = 1;

    public override bool IsRange => ((Max - Min) / StepSize) >= 1;


    public override IEnumerator<int> GetEnumerator()
    {
        Assert.IsTrue((Max - Min) / StepSize < MaxSteps, $"Settings for {Name} would result in more than {MaxSteps} steps.");
        for (int val = Min; val <= Max; val += StepSize)
        {
            Current = val;
            yield return val;
        }
    }

    public static implicit operator IntParameterRange(int value)
    {
        return new IntParameterRange {Min = value, Max = value, StepSize = 1};
    }
}


[System.Serializable]
public class StringParameterSet : ParameterRange<string>
{
    public List<string> Options;

    public override bool IsRange => Options.Count > 1;

    public override IEnumerator<string> GetEnumerator()
    {
        Assert.IsTrue(Options.Count > 0, $"No options for {Name}");

        foreach (var option in Options)
        {
            Current = option;
            yield return option;
        }
    }
}