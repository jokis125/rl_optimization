using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Threading;

public class RouteML : Agent
{

	public int stops = 5;
	public float maxDimension = 50;
	public GameObject mark;
	public GameObject home;
	public bool debugInConsole = false;
	public Vector2[] board;
	public Camera cam;
	Vector2[] bestBoard;
	Vector2[] bestBoardtemp;
	float maxDistance;
	LineRenderer lRen;
	int maxSwapCount = 150;
	int swapCount = 0;
	float newDist;
	float tempResult = 0f;
	float bestResult = Mathf.Infinity;


    public override void OnEpisodeBegin()
	{
		//CleanBoard();
		//Cleans gameobjects from the hierarchy that were generated with DrawBoard() that were left over from the last episode of training
		GenerateBoard(); //Generates data (Vector2[])
		//DrawBoard(); //Generates Gameobjects in the hierarchy for visual representation
		bestBoard = new Vector2[stops]; 
		//bestBoardtemp = new Vector2[stops]; 
		board.CopyTo(bestBoard, 0); //Shallow copy (should be deep, but for testing purposes it's fine)
		//board.CopyTo(bestBoardtemp, 0); //Shallow copy (should be deep, but for testing purposes it's fine)
		//BruteForceRoute(0, bestBoardtemp); //Brute force way of solving the problem (for training purposes)

		//This is probably wrong
		/*for(var i = 0; i < maxSwapCount; i++)
        {
			RequestDecision();
		}*/
		
	}

    public override void CollectObservations(VectorSensor sensor)
    {
		for(var i = 0; i < board.Length; i++)
        {
			sensor.AddObservation(board[i]);
		}
		
    }

	public override void OnActionReceived(ActionBuffers actionBuffers)
    {
		// Swap x with y elements. Stop episode if agent thinks it's done
		int x = actionBuffers.DiscreteActions[0]; 
		int y = actionBuffers.DiscreteActions[1];


		(bestBoard[x], bestBoard[y]) = (bestBoard[y], bestBoard[x]);
		swapCount++;
		//DrawSolution();
		
		if (swapCount >= maxSwapCount)
        {
			
			swapCount = 0;
			newDist = MeasureRoute(bestBoard); // measures the route distance
			//assuming that 1000 is longest possible path ALSO DOESNT WORK PROPERLY BECAUSE COPYTO = SHALLOWCOPY. Will fix later
			SetReward(Mathf.InverseLerp(600, 400, newDist));
			//Debug.Log(GetCumulativeReward());

			EndEpisode();
		}
	}

	IEnumerator waiter()
	{
		yield return new WaitForSeconds(4);
	}


		void GenerateBoard()
	{
		board = new Vector2[stops];
		maxDistance = 0f;
		for (int i = 0; i < stops; i++)
		{
			board[i] = new Vector2(Random.Range(-maxDimension, maxDimension), Random.Range(-maxDimension, maxDimension));
			maxDistance = Mathf.Max(maxDistance, Mathf.Abs(board[i].x), Mathf.Abs(board[i].y));
		}
	}


	void DrawBoard()
	{
		//Method to draw a route on screen
		// INITIALIZE VALUES
		GameObject[] design = new GameObject[stops + 1];

		// COUNT MIN DISTANCES
		float minDistance = maxDistance;
		for (int i = 0; i < stops; i++)
		{
			minDistance = Mathf.Min(minDistance, Vector2.Distance(Vector2.zero, board[i]));
		}
		for (int i = 0; i < stops - 1; i++)
		{
			for (int d = i + 1; d < stops; d++)
			{
				minDistance = Mathf.Min(minDistance, Vector2.Distance(board[i], board[d]));
			}
		}
		minDistance = minDistance / 1.25f;

		// CREATE OBJECT & SET SIZES
		design[0] = Instantiate(home, new Vector3(0f, 0f, 0f), Quaternion.identity) as GameObject;
		lRen = design[0].GetComponent<LineRenderer>();
		for (int i = 1; i < stops + 1; i++)
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
		for (int i = 0; i < stops - 1; i++)
		{
			totalDistance += Vector2.Distance(boardToMeasure[i], boardToMeasure[i + 1]);
		}
		totalDistance += Vector2.Distance(boardToMeasure[stops - 1], Vector2.zero);
		return totalDistance;
	}

	void DrawSolution()
	{
		lRen.positionCount = stops + 2;
		lRen.SetPosition(0, Vector3.zero);
		for (int i = 1; i < stops + 1; i++)
		{
			lRen.SetPosition(i, (Vector3)bestBoard[i - 1]);
		}
	}

	void BruteForceRoute(int counter, Vector2[] boardToTry)
	{
		Vector2[] boardToExit = new Vector2[stops];
		boardToTry.CopyTo(boardToExit, 0);
		bool keepRunning = true;

		do
		{
			for (int i = counter; i < stops - 1 - counter; i++)
			{

				Vector2 tempVector2 = boardToTry[i];
				boardToTry[i] = boardToTry[i + 1];
				boardToTry[i + 1] = tempVector2;

				tempResult = MeasureRoute(boardToTry);
				if (tempResult < bestResult)
				{
					//Debug.Log ("New Best: " + tempResult);
					bestResult = tempResult;
					boardToTry.CopyTo(bestBoard, 0);
				}


				if (counter < stops - 3)
					BruteForceRoute(counter + 1, boardToTry);

			}

			//CHECK FOR EXIT
			keepRunning = (boardToTry.SequenceEqual(boardToExit)) ? false : true;


		} while (keepRunning);
	}
}