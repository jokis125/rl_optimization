using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Linq;
using Unity.Barracuda;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

using UnityEditor;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class RouteML : Agent
{

	public int minStops = 5;
	public int maxStops = 5;
	public float maxDimension = 50;
	public GameObject mark;
	public GameObject home;
	public Vector2[] board;
	public Camera cam;
	public bool pause = false;
	public bool logResults = false;
	public bool testGraphs = false;
	
	public bool drawResult = false;
	private int graphEps = 0;

	public int graphSkip = 1; //test every other graph

	private Dictionary<int, float> random = new Dictionary<int, float>();
	private Dictionary<int, List<double>> results = new Dictionary<int, List<double>>();
	private List<double> timeResults = new List<double>();

	Vector2[] bestBoard;
	Vector2[] bestBoardtemp;
	float maxDistance;
	LineRenderer lRen;
	float newDist;
	private float newdistsum = 0;
	public int maxTestEps = 1000;
	private float randomsum = 0;
	private float randomDist = 0;

	private int graphNodeCount = 5;
	private double episodeTimeSum = 0;
	private double totalTimeSum = 0;
	
	float bestResult = Mathf.Infinity;

	private float distanceSum;
	public bool logTime = false;
	private Stopwatch watch = new System.Diagnostics.Stopwatch();

	public string txtNameIdentifier;
	
	
	

	public void Start()
	{
		graphNodeCount = minStops;
		if (logResults || testGraphs)
		{
			Time.timeScale = 100;
		}
		
		if (drawResult)
		{
			Time.timeScale = 0.5f;
		}
		
		cam = Camera.main;
	}

	public override void OnEpisodeBegin()
	{
		
		bestResult = Mathf.Infinity;

		if (testGraphs && graphEps == maxTestEps)
		{
			if (!logTime)
			{
				random.Add(graphNodeCount, randomsum / graphEps);
				results.Add(graphNodeCount, new List<double>{newdistsum / graphEps, totalTimeSum / graphEps});
			}

			if (logTime)
			{
				timeResults.Add(totalTimeSum / graphEps);
				totalTimeSum = 0;
			}
			graphNodeCount += graphSkip;
			newdistsum = 0;
			randomsum = 0;
			graphEps = 0;
		}
#if UNITY_EDITOR
		if ((graphNodeCount >= maxStops) && testGraphs)
		{
			EditorApplication.ExitPlaymode();
		}
#endif
		
#if UNITY_STANDALONE_WIN
		if ((graphNodeCount >= maxStops) && testGraphs)
		{
			Application.Quit();
		}
#endif


		GenerateBoard(!testGraphs ? Random.Range(minStops, maxStops) : graphNodeCount);
		MaxStep = board.Length * 2;
		
		randomDist = MeasureRoute(board);
		if (!logTime)
		{
			if (logResults || testGraphs)
			{
				randomsum += randomDist;
			}
		
		
			if (drawResult)
			{
				DrawBoard();
				//Debug.Break();
			}
		}
		

		if (logTime)
		{
			watch.Reset();
			watch.Start();
		}
	}

	public override void CollectObservations(VectorSensor sensor)
	{
		foreach (var t in board)
	    {
		    var x = Normalize(t.x, -maxDimension, maxDimension);
		    var y = Normalize(t.y, -maxDimension, maxDimension);
		    sensor.AddObservation(new Vector2(x, y));
	    }

	    for (var i = board.Length; i < maxStops; i++)
	    {
		    sensor.AddObservation(new Vector2(2, 2));
	    }
    }

	public override void OnActionReceived(ActionBuffers actionBuffers)
	{
		// Swap x with y elements.
		int x = actionBuffers.DiscreteActions[0]; 
		int y = actionBuffers.DiscreteActions[1];

		//skip move if out of bounds
		if (x < board.Length && y < board.Length)
		{
			(board[x], board[y]) = (board[y], board[x]);
		}
		else
		{
			AddReward(-0.05f);
		}
		
		if (drawResult)
		{
			DrawSolution(board);
			if ((StepCount >= MaxStep-1 || StepCount == 1) && pause)
			{
				Debug.Break();
			}
		}
		
		if (StepCount >= MaxStep)
		{
			newDist = MeasureRoute(board);

			if (testGraphs)
			{
				graphEps++;
				newdistsum += newDist;
				if(logResults)
					Debug.Log($"Random:{randomDist} solved:{newDist} Best:{bestResult} DistSum/2:{distanceSum}");
			}

			if (drawResult)
			{
				CleanBoard();
			}
			//var reward = (randomDist == bestResult) && (bestResult == newDist) ?  1 : Mathf.InverseLerp(randomDist, bestResult, newDist);
			var reward = Mathf.InverseLerp(randomDist, distanceSum, newDist);
			SetReward(reward);

			if (logResults)
			{
				Debug.Log(GetCumulativeReward());
			}

			if (logTime)
			{
				watch.Stop();
				totalTimeSum += watch.ElapsedMilliseconds;
			}
			EndEpisode();
		}

    }

	private void OnApplicationQuit()
	{

		if (logResults)
		{
			print($"Total Graphs: {graphEps}");
			print($"Agent Result = {newdistsum / graphEps}");
			print($"Random Result = {randomsum / graphEps}");
		}

		/*if (logTime)
		{
			if (!File.Exists("C:\\Users\\Jokubas.DESKTOP-AHK8CJE\\Documents\\rl_optimization\\BIGTEST\\test.txt"))
			{
				using (var sw = File.CreateText("C:\\Users\\Jokubas.DESKTOP-AHK8CJE\\Documents\\rl_optimization\\BIGTEST\\test.txt"))
				{
					foreach (var timeResult in timeResults)
					{
						sw.WriteLine(timeResult);
					}
				}
			}
			foreach (var timeResult in timeResults)
			{
				print(timeResult);
			}
		}*/

		if (testGraphs && !logTime)
		{
			for (var i = minStops; i < maxStops; i += graphSkip)
			{
				string resultString = $"Solved: {i} : ";
				print($"Random: Nodes: {i}: {random[i]}");
				foreach (var res in results[i])
				{

					resultString += $"{res}, ";
				}
				print(resultString);
			}
		}
		string path = $@"C:\results\{minStops}-{maxStops}Results{txtNameIdentifier}.txt";
		if (!File.Exists(path))
		{
			using (var sw = File.CreateText(path))
			{
				sw.WriteLine($"Total Graphs: {graphEps}");
				sw.WriteLine($"Agent Result = {newdistsum / graphEps}");
				sw.WriteLine($"Random Result = {randomsum/ graphEps}");
			}
		}
		
		using (var sw = File.AppendText(path))
		{
			var i = minStops;
			foreach (var result in results)
			{
				sw.WriteLine($"Random: Nodes: {i}: {random[i]}");
				sw.WriteLine($"Solved Nodes: {result.Value[0]}");
				i += graphSkip;
			}
		}	
		
	}


	private void GenerateBoard(int nodes)
	{
		board = new Vector2[nodes];
		maxDistance = 0f;
		distanceSum = 0f;
		for (var i = 0; i < nodes; i++)
		{

			board[i] = new Vector2(Random.Range(-maxDimension, maxDimension),
				Random.Range(-maxDimension, maxDimension));
			maxDistance = Mathf.Max(maxDistance, Vector2.Distance(Vector2.zero, board[i]));//,// Mathf.Abs(board[i].x), Mathf.Abs(board[i].y));
			distanceSum += Vector2.Distance(Vector2.zero, board[i]);
		}
		distanceSum /= 2;
	}


	void DrawBoard()
	{
		//Method to draw a route on screen
		// INITIALIZE VALUES
		var design = new GameObject[board.Length + 1];

		// COUNT MIN DISTANCES
		var minDistance = maxDistance;
		for (int i = 0; i < board.Length; i++)
		{
			minDistance = Mathf.Min(minDistance, Vector2.Distance(Vector2.zero, board[i]));
		}
		for (int i = 0; i < board.Length - 1; i++)
		{
			for (int d = i + 1; d < board.Length; d++)
			{
				minDistance = Mathf.Min(minDistance, Vector2.Distance(board[i], board[d]));
			}
		}
		minDistance = minDistance / 1.25f;

		// CREATE OBJECT & SET SIZES
		design[0] = Instantiate(home, new Vector3(0f, 0f, 0f), Quaternion.identity) as GameObject;
		lRen = design[0].GetComponent<LineRenderer>();
		for (int i = 1; i < board.Length + 1; i++)
		{
			design[i] = Instantiate(mark, new Vector3(board[i - 1].x, board[i - 1].y, 0), Quaternion.identity) as GameObject;
			design[i].transform.localScale = new Vector3(minDistance, minDistance, minDistance);
			//yield return null;
		}
		design[0].transform.localScale = new Vector3(minDistance, minDistance, minDistance);
		cam.orthographicSize = maxDistance + minDistance;
	}

	void CleanBoard()
    {
		foreach (GameObject o in Object.FindObjectsOfType<GameObject>())
		{
			if(o.layer.Equals(7))
				Destroy(o);
		}
	}

	float MeasureRoute(Vector2[] boardToMeasure)
	{
		float totalDistance = Vector2.Distance(Vector2.zero, boardToMeasure[0]);
		for (int i = 0; i < boardToMeasure.Length - 1; i++)
		{
			totalDistance += Vector2.Distance(boardToMeasure[i], boardToMeasure[i + 1]);
		}
		totalDistance += Vector2.Distance(boardToMeasure[boardToMeasure.Length - 1], Vector2.zero);
		return totalDistance;
	}

	void DrawSolution(Vector2[] boardToDrwa)
	{
		lRen.positionCount = boardToDrwa.Length + 2;
		lRen.SetPosition(0, Vector3.zero);
		for (int i = 1; i < boardToDrwa.Length + 1; i++)
		{
			lRen.SetPosition(i, (Vector3)boardToDrwa[i - 1]);
		}
	}

	//-------------------------------------
	private float Normalize(float value, float min, float max)
	{
		return (value - min) / (max - min);
	}
}