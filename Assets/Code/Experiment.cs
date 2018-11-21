using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class Experiment : MonoBehaviour
{
    public ParameterSet Parameters;

    public bool LeaveResultOnScreen;
    public bool DoNextExperiment;
    public int StepsPerFrame = 10;

    public ExperimentResults Results;
    public Text FrameText;

    int _numberOfBoxes = 20;
    GameObject _boxPrefab;
    float _stackHeightMultiplier = 1.001f;

    readonly List<Rigidbody> _boxes = new List<Rigidbody>();
    bool _experimentRunning;
    bool _runInFixedUpdate;
    int _repeatsRemaining;
    int _maximumFrames;
    int _runningFrames;
    float _density;
    float _friction;
    float _positionJitter;
    bool _randomizeAngle;
    float _boxHeight;
    float _currentStackHeight;
    bool _stackOneByOne;
    int _boxesPlaced;


    // Use this for initialization
    void OnEnable()
    {
        DoExperiments();
    }

    string GetExperimentBasePath()
    {
        return Path.Combine(Application.dataPath, "Results", $"{Parameters.Filename}");
    }

    void DoExperiments()
    {
        Parameters.Reset();
        var basepath = GetExperimentBasePath();
        var settingsPath = basepath + ".settings.txt";
        var meta = $"Date: {DateTime.Now:yyyy-MM-dd H:mm:ss}\n{Parameters.FixedParameters()}";
        File.WriteAllText(settingsPath, meta);

        Results = new ExperimentResults();

        var resultsPath = basepath + ".results.csv";
        var header = $"{Parameters.VariableParametersCSVHeader()},{ExperimentResults.CSVHeader()}\n";

        File.WriteAllText(resultsPath, header);

        DoNextExperiment = LeaveResultOnScreen == false;

        StartNextExperiment();
    }

    void WriteResults()
    {
        var resultsPath = GetExperimentBasePath() + ".results.csv";
        File.AppendAllText(resultsPath, $"{Parameters.VariableParametersCSVValues()},{Results.CSVValues()}\n");
    }

    void SetupFromParameters(ParameterSet parameters)
    {
        Physics.autoSimulation = false;
        Physics.sleepThreshold = parameters.SleepThreshold.Current;
        _numberOfBoxes = parameters.NumberOfBoxes.Current;
        _boxPrefab = Resources.Load<GameObject>("Boxes/" + parameters.BoxType.Current);
        _stackHeightMultiplier = parameters.StackHeightMultiplier.Current;
        Physics.defaultSolverIterations = parameters.SolverIterations.Current;
        Time.fixedDeltaTime = parameters.FixedTimestep.Current;
        _runInFixedUpdate = parameters.RunExperimentInFixedUpdate.Current;
        _repeatsRemaining = parameters.Repeats.Current;
        _maximumFrames = parameters.MaximumSimulationFrames.Current;
        _density = parameters.BoxDensity.Current;
        _friction = parameters.Friction.Current;
        _positionJitter = parameters.PositionJitter.Current;
        _randomizeAngle = parameters.RandomizeAngle.Current;
        Physics.gravity = new Vector3(0, -parameters.Gravity.Current, 0);
        _stackOneByOne = parameters.StackOneByOne.Current;
    }

    void StartNextExperiment()
    {
        /*
         * Start Next if necessary
         */
        if (_repeatsRemaining == 0)
        {
            Results = new ExperimentResults();
            if (Parameters.MoveNext())
            {
                SetupFromParameters(Parameters);
                StartSingleExperiment();
            }
            else
            {
                Debug.LogFormat("All experiments finished");
            }
        }
        else
        {
            StartSingleExperiment();
        }
    }

    void StartSingleExperiment()
    {
        Debug.Log($"Starting experiment ({_repeatsRemaining} remaining) \n {Parameters.CurrentValuesString()}");
        _repeatsRemaining--;

        _boxHeight = 0;
        _currentStackHeight = 1f;
        

        int placeNow = _stackOneByOne ? 1 : _numberOfBoxes;
        _boxesPlaced = 0;
        while (_boxesPlaced < placeNow)
        {
            var box = AddBox();
            // Do some stuff after adding the first box
            if (_boxesPlaced == 1)
            {
                var physicMaterial = box.GetComponent<Collider>().sharedMaterial;
                physicMaterial.dynamicFriction = _friction;
                physicMaterial.staticFriction = _friction;
                _boxHeight = GetTotalColliderSize(box).y;
                box.isKinematic = true;
            }
        }


        _experimentRunning = true;
        _runningFrames = 0;

        /*
         * Position camera
         */
        var total = _boxHeight * _numberOfBoxes;
        var camPos = new Vector3(total * 0.75f, total, total * 1.25f);
        Camera.main.transform.position = camPos;
        Camera.main.transform.LookAt(new Vector3(0, total * 0.3f, 0));
    }

    Rigidbody AddBox()
    {
        var box = Instantiate(_boxPrefab).GetComponent<Rigidbody>();

        box.SetDensity(_density);
        box.mass = box.mass; // workaround for bug where mass doesn't show
        _boxes.Add(box);

        var angle = Random.Range(0, Mathf.PI * 2);
        var x = Mathf.Sin(angle) * _positionJitter;
        var z = Mathf.Cos(angle) * _positionJitter;


        box.transform.position = new Vector3(x, _currentStackHeight + _boxHeight * _stackHeightMultiplier, z);
        _currentStackHeight = box.transform.position.y;
        if (_randomizeAngle)
        {
            box.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        }

        _boxesPlaced++;
        return box;
    }

    void Update()
    {
        if (_runInFixedUpdate)
        {
            return;
        }

        for (int i = 0; i < StepsPerFrame; i++)
        {
            if (_experimentRunning)
            {
                RunStep();
            }
        }
    }

    void FixedUpdate()
    {
        if (_experimentRunning && _runInFixedUpdate)
        {
            RunStep();
        }
    }

    void RunStep()
    {
        var start = Time.realtimeSinceStartup;
        Physics.Simulate(Time.fixedDeltaTime);
        var elapsed = Time.realtimeSinceStartup - start;

        Results.Add(Measure.SimulationTime, elapsed);

        _runningFrames++;
        FrameText.text = $"{_runningFrames} frames";

        // Reset the stack height and measure it later when looping over all boxes
        _currentStackHeight = 0;

        bool allSleeping = true;
        foreach (var rb in _boxes)
        {
            if (rb.IsSleeping())
            {
                rb.GetComponent<Renderer>().material.color = Color.blue;
            }
            else
            {
                rb.GetComponent<Renderer>().material.color = Color.white;
                allSleeping = false;
            }

            allSleeping &= rb.IsSleeping();

            var yPos = rb.position.y;
            if (yPos < 0)
            {
                rb.GetComponent<Renderer>().material.color = Color.red;
                Results.Add(Measure.FramesToCollapse, _runningFrames);
                FinishExperiment(false, false, true);
                return;
            }

            _currentStackHeight = Mathf.Max(_currentStackHeight, yPos);
        }

        if (allSleeping)
        {
            if (_boxesPlaced < _numberOfBoxes)
            {
                AddBox();
            }
            else
            {
                Results.Add(Measure.FramesToSleep, _runningFrames);
                FinishExperiment(true, false, false);
                return;
            }
        }

        if (_runningFrames >= _maximumFrames)
        {
            Results.Add(Measure.FramesToSleep, _runningFrames);
            FinishExperiment(false, true, false);
            return;
        }
    }

    void FinishExperiment(bool didSleep, bool didTimeout, bool didCollapse)
    {
        Results.Add(Measure.AmountThatSlept, didSleep ? 1 : 0);
        Results.Add(Measure.AmountTimeout, didTimeout ? 1 : 0);
        Results.Add(Measure.AmountCollapsed, didCollapse? 1 : 0);
        _experimentRunning = false;

        /*
         * Process Results
         */
        if (_repeatsRemaining == 0)
        {
            WriteResults();
        }
//        Debug.Log(Results.Report());

        StartCoroutine(CleanAndStartNextExperiment());
    }

    IEnumerator CleanAndStartNextExperiment()
    {
        while (DoNextExperiment == false)
        {
            yield return null;
        }

        if (LeaveResultOnScreen)
        {
            DoNextExperiment = false;
        }

        yield return null;
        /*
         * Cleanup
         */
        foreach (var box in _boxes)
        {
            Destroy(box.gameObject);
        }

        _boxes.Clear();

        /*
         * Start Next Experiment
         */
        StartNextExperiment();
    }


    static Vector3 GetTotalColliderSize(Rigidbody box)
    {
        var bounds = new Bounds();
        bool first = true;
        foreach (var collider in box.GetComponentsInChildren<Collider>())
        {
            if (first)
            {
                bounds = collider.bounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return bounds.size;
    }
}