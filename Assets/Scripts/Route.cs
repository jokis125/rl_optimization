using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Route : MonoBehaviour {

	public int stops = 10;
	public float maxDimension = 100;
	public GameObject mark;
	public GameObject home;
	public bool debugInConsole = false;
	public Text textOutput; 
	public Vector2[] board;
	Vector2[] bestBoard;
	float maxDistance;
	LineRenderer lRen;
	double totalCombos = 1;
	float tempResult = 0f;
	float bestResult = Mathf.Infinity;
	private int comboCheck = 0;

	void Start () 
	{
		Debug.Log(Mathf.InverseLerp(350, 750, 0.75f));

		GenerateBoard ();

		//DrawBoard();

		bestBoard = new Vector2[stops];
		board.CopyTo (bestBoard, 0);
	    float timeTook = Time.realtimeSinceStartup;
		BruteForceRoute (0, (Vector2[])board.Clone());
		//timeTook = Time.realtimeSinceStartup - timeTook;
		//textOutput.text = "It took: " + timeTook.ToString() + " for " + comboCheck + " checks";
		//DrawSolution ();
		Debug.Log(Mathf.InverseLerp(750, 350, 350));
		Debug.Log(MeasureRoute(bestBoard));
	}
	
	void GenerateBoard()
	{
		board = new Vector2[stops];
		maxDistance = 0f;
		for (int i = 0; i < stops; i++)
		{
			board [i] = new Vector2 (Random.Range (-maxDimension, maxDimension), Random.Range (-maxDimension, maxDimension));
			maxDistance = Mathf.Max (maxDistance, Mathf.Abs(board [i].x), Mathf.Abs(board [i].y));
		
		}
		//Debug.Log ("Destinations: " + stops + " Possible combinations: " + (CountCombos(stops)/2) );
	}


	/*void DrawBoard()
	{
		//Method to draw a route on screen
		// INITIALIZE VALUES
		GameObject[] design = new GameObject[stops + 1];

		// COUNT MIN DISTANCES
		float minDistance = maxDistance;
		for (int i = 0; i < stops; i++) 
		{
			minDistance = Mathf.Min (minDistance, Vector2.Distance (Vector2.zero, board [i]));
		}
		for (int i = 0; i < stops-1; i++) 
		{
			for (int d = i + 1; d < stops; d++) 
			{
				minDistance = Mathf.Min (minDistance, Vector2.Distance (board [i], board [d]));
			}
		}
		minDistance = minDistance / 1.25f;

		// CREATE OBJECT & SET SIZES
		design[0] = Instantiate (home, new Vector3 (0f, 0f, 0f), Quaternion.identity) as GameObject;
		lRen = design [0].GetComponent<LineRenderer> ();
		for (int i = 1; i < stops + 1; i++) 
		{
			design[i] = Instantiate (mark, new Vector3 (board [i - 1].x, board [i - 1].y, 0), Quaternion.identity) as GameObject;
			design[i].transform.localScale = new Vector3 (minDistance, minDistance, minDistance);
			//yield return null;
		}
			design[0].transform.localScale = new Vector3 (minDistance, minDistance, minDistance);
		Camera.main.orthographicSize = maxDistance + minDistance;
	}*/

	void BruteForceRoute (int counter, Vector2[] boardToTry) 
	{
		Vector2[] boardToExit = new Vector2[stops];
		boardToTry.CopyTo (boardToExit, 0);
		bool keepRunning = true;

		do 
		{
			for (int i = counter; i < stops - 1 - counter; i++) 
			{
				
				Vector2 tempVector2 = boardToTry [i];
				boardToTry [i] = boardToTry [i + 1];
				boardToTry [i + 1] = tempVector2;

				tempResult = MeasureRoute (boardToTry);
				if (tempResult < bestResult)
				{
					//Debug.Log ("New Best: " + tempResult);
					bestResult = tempResult;
					boardToTry.CopyTo(bestBoard, 0);
				}


				if (counter < stops - 3)
					BruteForceRoute (counter+1, boardToTry);
					
			}

			//CHECK FOR EXIT
			keepRunning = (boardToTry.SequenceEqual (boardToExit)) ? false : true; 

		
		} while (keepRunning);
	}

	float MeasureRoute (Vector2[] boardToMeasure)
	{
		comboCheck++;
		float totalDistance = Vector2.Distance (Vector2.zero, boardToMeasure[0]);
		if (debugInConsole)
			Debug.Log ("Stop: " + (0) + " = " + boardToMeasure [0].ToString ());
		for (int i = 0; i < stops - 1; i++) 
		{
			if (debugInConsole)
				Debug.Log ("Stop: " + (i + 1) + " = " + boardToMeasure [i + 1].ToString ());
			totalDistance += Vector2.Distance (boardToMeasure [i], boardToMeasure [i + 1]);
		}
		totalDistance += Vector2.Distance (boardToMeasure [stops - 1], Vector2.zero);
		return totalDistance;
	}

	void DrawSolution()
	{
		lRen.positionCount = stops + 2;
		lRen.SetPosition (0, Vector3.zero);
		for (int i = 1; i < stops + 1; i++) 
		{
			lRen.SetPosition (i, (Vector3)bestBoard[i - 1]);
		}
	}
}