using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Measure
{
    FramesToSleep,
    FramesToCollapse,
    AmountThatSlept,
    SimulationTime,
    AmountTimeout,
    AmountCollapsed
}

[System.Serializable]
public class ExperimentResults
{
    public Dictionary<Measure, List<float>> Results = new Dictionary<Measure, List<float>>();
    public Dictionary<Measure, float> Averages = new Dictionary<Measure, float>();

    public void Add(Measure measure, float value)
    {
        if (Results.TryGetValue(measure, out var resultsForMeasure) == false)
        {
            resultsForMeasure = new List<float>();
            Results.Add(measure, resultsForMeasure);
        }

        resultsForMeasure.Add(value);
    }

    void ComputeAverages()
    {
        foreach (Measure e in Enum.GetValues(typeof(Measure)))
        {
            Averages[e] = 0;
        }

        foreach (var pair in Results)
        {
            Averages[pair.Key] = pair.Value.Average();
        }
    }

    public static string CSVHeader()
    {
        return string.Join(",", Enum.GetValues(typeof(Measure)).OfType<Measure>().Select(e => e.ToString()));
    }

    public string CSVValues()
    {
        ComputeAverages();

        return string.Join(",", Enum.GetValues(typeof(Measure)).OfType<Measure>().Select(e => $"{Averages[e]:0.######}"));
    }

    public string Report()
    {
        ComputeAverages();
        return string.Join(", ", Averages.Select(pair => $"{pair.Key}: {pair.Value:0.######}"));
    }
}