using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


[CreateAssetMenu(menuName = "ParameterSet")]
public class ParameterSet : ScriptableObject
{
    public IntParameterRange NumberOfBoxes = 10;
    public FloatParameterRange FixedTimestep = 1f / 20;
    public IntParameterRange SolverIterations = 6;
    public StringParameterSet BoxType;
    public FloatParameterRange StackHeightMultiplier = 1.001f;
    public BoolParameterSet RunExperimentInFixedUpdate;
    public IntParameterRange Repeats = 5;
    public IntParameterRange MaximumSimulationFrames = 5000;
    public FloatParameterRange Gravity = 9.81f;
    public FloatParameterRange BoxDensity = 1;
    public FloatParameterRange SleepThreshold = 0.005f;
    public FloatParameterRange Friction = 1;
    public FloatParameterRange PositionJitter = 0;

    public BoolParameterSet RandomizeAngle;

    public BoolParameterSet StackOneByOne;
//    public BoolParameterSet AdaptiveForce;

    List<IParameterRange> _parameters;

    readonly List<IEnumerator> _enumerators = new List<IEnumerator>();
    bool _isFirstIteration = false;

    public string Filename => name.ToLower().Replace(" ", "-");

    void OnEnable()
    {
        _parameters = new List<IParameterRange>
        {
            NumberOfBoxes,
            FixedTimestep,
            SolverIterations,
            BoxType,
            StackHeightMultiplier,
            RunExperimentInFixedUpdate,
            Repeats,
            MaximumSimulationFrames,
            Gravity,
            BoxDensity,
            SleepThreshold,
            Friction,
            PositionJitter,
            RandomizeAngle,
            StackOneByOne
//            AdaptiveForce,
        };
        NumberOfBoxes.Name = "NumberOfBoxes";
        FixedTimestep.Name = "FixedTimestep";
        SolverIterations.Name = "SolverIterations";
        BoxType.Name = "BoxType";
        StackHeightMultiplier.Name = "StackHeightMultiplier";
        RunExperimentInFixedUpdate.Name = "RunExperimentInFixedUpdate";
        Repeats.Name = "Repeats";
        MaximumSimulationFrames.Name = "MaximumSimulationFrames";
        Gravity.Name = "Gravity";
        BoxDensity.Name = "BoxDensity";
        SleepThreshold.Name = "SleepThreshold";
        Friction.Name = "Friction";
        PositionJitter.Name = "PositionJitter";
        RandomizeAngle.Name = "RandomizeAngle";
        StackOneByOne.Name = "StackOneByOne";
//        AdaptiveForce.Name = "AdaptiveForce";
    }


    public void Reset()
    {
        _isFirstIteration = true;
        _enumerators.Clear();
        foreach (var param in _parameters)
        {
            var enumerator = param.GetEnumerator();
            _enumerators.Add(enumerator);
        }
    }

    // Update is called once per frame
    public bool MoveNext()
    {
        bool hasNext = false;
        for (var i = 0; i < _enumerators.Count; i++)
        {
            var enumerator = _enumerators[i];
            if (enumerator.MoveNext())
            {
                hasNext = true;
                // MoveNext() ALL iterators on the first round. So no break.
                if (_isFirstIteration == false)
                {
                    break;
                }
            }
            else
            {
                // Reset the enumerator and advance to first position
                _enumerators[i] = _parameters[i].GetEnumerator();
                _enumerators[i].MoveNext();
            }
        }

        _isFirstIteration = false;
        return hasNext;

        /*
         anyIncremented = false;
         foreach(param)
         {
             if (param.Next())
             {
                break;
             }
         }
         increment First
         if (false)
         
         
         */
    }

    public string VariableParametersCSVHeader()
    {
        return string.Join(",", _parameters.Where(p => p.IsRange).Select(p => p.Name));
    }

    public string VariableParametersCSVValues()
    {
        return string.Join(",", _parameters.Where(p => p.IsRange).Select(p => p.CurrentValue));
    }

    public string FixedParameters()
    {
        return string.Join("\n", _parameters
            .Where(p => p.IsRange == false)
            .Select(p => $"{p.Name}: {p.CurrentValue}"));
    }

    public string CurrentValuesString()
    {
        return string.Join("\n", _parameters.Select(p => p.ToString()));
    }
}