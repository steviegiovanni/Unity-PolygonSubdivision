using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Edge:IComparable<Edge>{
	public Vector2 v1;
	public Vector2 v2;
	public Edge(Vector2 v1,Vector2 v2){
		if (Comparator.CompareX(v1,v2) > 0) {
			this.v1 = v2;
			this.v2 = v1;
		} else {
			this.v1 = v1;
			this.v2 = v2;
		}
	}

	public bool IsBehind(float x){
		return v1.x < x;
	}

	public float GetTangent(){
		return (v2.y - v1.y) / (v2.x - v1.x);
	}

	public int CompareTo(Vector2 v){
		// find equation of edge
		float tan = GetTangent();
		float b = v1.y - tan * v1.x;
		float delta = v.y -(v.x * tan + b);
		Debug.Log ("Y = "+v.y+", ComputedY = "+(v.x * tan + b));
		if (delta < 0.0)
			return -1;
		else if (delta > 0)
			return 1;
		else
			return 0;
	}

	public Vector2 GetIntersectionWith (float x){
		// find equation of edge
		float tan = GetTangent();
		float b = v1.y - tan * v1.x;
		return new Vector2 (x, x * tan + b);
	}

	#region IComparable implementation

	public int CompareTo (Edge e)
	{
		if ((this.v1 == e.v1) && (this.v2 == e.v2)) {
			return 0;
		} else {
			return Comparator.Compare (this, e);
		}
	}

	#endregion
}

public static class Comparator{
	public static int CompareX(Vector2 v1, Vector2 v2){
		return v1.x.CompareTo(v2.x);
	}

	public static int CompareY(Vector2 v1, Vector2 v2){
		return v1.y.CompareTo(v2.y);
	}

	public static int Compare(Edge e1, Edge e2){	
		// find equation of e2
		float tan = e2.GetTangent();
		float b = e2.v1.y - tan * e2.v1.x;
		float deltav1y = e1.v1.y -(e1.v1.x * tan + b);
		if (deltav1y > 0)
			return -1;
		else if (deltav1y < 0)
			return 1;
		else {
			float deltav2y = e1.v2.y - (e1.v2.x * tan + b);
			if (deltav2y > 0)
				return -1;
			else if (deltav2y < 0)
				return 1;
			else
				return 0;
		}
	}

	public static int Equal(Edge e1, Edge e2){
		if ((e1.v1 == e2.v1) && (e1.v2 == e2.v2))
			return 0;
		else
			return -1;
	}
}

public class ActiveEdgesList{
	private List<Edge> _edgeList;
	public List<Edge> EdgeList{
		get{
			if (_edgeList == null)
				_edgeList = new List<Edge> ();
			return _edgeList;
		}
	}

	public int Count{
		get{
			return EdgeList.Count;
		}
	}

	public void Insert (Edge e){
		if (EdgeList.Count == 0)
			EdgeList.Add (e);
		else if (EdgeList [EdgeList.Count - 1].CompareTo (e) <= 0)
			EdgeList.Add (e);
		else if (EdgeList [0].CompareTo (e) >= 0)
			EdgeList.Insert (0, e);
		else {
			int index = EdgeList.BinarySearch (e);
			if (index < 0)
				index = ~index;
			EdgeList.Insert (index, e);
		}
	}

	public void Remove(Edge e){
		int index = EdgeList.BinarySearch (e);
		if(index >= 0)
			EdgeList.RemoveAt (index);
	}

	public Edge GetEdgeAtIndex(int i){
		if (i < EdgeList.Count)
			return EdgeList [i];
		return null;
	}

	public int GetIndexOf(Edge e){
		return EdgeList.BinarySearch (e);
	}
}

public class Polygon : MonoBehaviour {
	private void OnDrawGizmos(){			
		if (transform.childCount <= 2) // need at least 3 vertices
			return; 

		// form dictionary transform -> index
		Dictionary<Transform,int> transformIndexMap = new Dictionary<Transform, int>();
		for (int i = 0; i < transform.childCount; i++) 
			transformIndexMap.Add (transform.GetChild(i),i);

		// get the list of child transform and sort it on x
		List<Transform> childs = new List<Transform>();
		for (int i = 0; i < transform.childCount; i++)
			childs.Add (transform.GetChild (i));
		childs.Sort ((a, b) => Comparator.CompareX (a.position, b.position));

		// construct active list of scanned edges
		ActiveEdgesList edgeList = new ActiveEdgesList ();

		int test = 0;
		// scan the polygon
		foreach (var child in childs) {
			// get index of transform in the list
			int index;
			if(transformIndexMap.TryGetValue(child,out index)){
				// scanline x
				float scanline = child.position.x;


				// get the next and previous transform
				Transform next = transform.GetChild(GetCircularIndex(index+1));
				Transform previous = transform.GetChild(GetCircularIndex(index-1));

				// construct the edges that intersects with current vertex
				Edge e1 = new Edge(child.position,next.position);
				Edge e2 = new Edge (child.position, previous.position);

				// check condition of the two edges and determine condition of current vertex
				if (!e1.IsBehind (scanline) && !e2.IsBehind (scanline)) { 
					//Gizmos.color = Color.green;
					//Gizmos.DrawSphere (child.position, 0.2f);

					edgeList.Insert (e1);
					edgeList.Insert (e2);
				

				} else if (e1.IsBehind (scanline) && e2.IsBehind (scanline)) {
					//Gizmos.color = Color.red;
					//Gizmos.DrawSphere (child.position, 0.2f);

					edgeList.Remove (e1);
					edgeList.Remove (e2);
				} else {
					//Gizmos.color = Color.white;
					//Gizmos.DrawSphere (child.position, 0.2f);

					if (e1.IsBehind (scanline)) {
						edgeList.Remove (e1);
						edgeList.Insert (e2);
					} else {
						edgeList.Remove (e2);
						edgeList.Insert (e1);
					}
				}



				// debug
				/*test++;
				if (test == 2) {
					Debug.Log ("[Iteration " + test+"] Active Edge = "+edgeList.Count);
					Debug.Log("compare to value: "+edgeList.GetEdgeAtIndex (1).CompareTo(child.position));
					Edge test1 = edgeList.GetEdgeAtIndex(0);
					Gizmos.color = Color.magenta;
					Gizmos.DrawLine (test1.v1, test1.v2);
					Edge test2 = edgeList.GetEdgeAtIndex(1);
					Gizmos.color = Color.white;
					Gizmos.DrawLine (test2.v1, test2.v2);
				}*/

				// end debug

				// draw vertical line to above
				int activeEdgeIndex = 0;
				Edge closestAbove = null;
				while ((activeEdgeIndex < edgeList.Count) && (edgeList.GetEdgeAtIndex (activeEdgeIndex).CompareTo(child.position) == -1)) {
					closestAbove = edgeList.GetEdgeAtIndex (activeEdgeIndex);
					activeEdgeIndex++;
				}
				if (closestAbove != null) {
					Vector2 intersection = closestAbove.GetIntersectionWith (scanline);

					Gizmos.color = Color.blue;
					Gizmos.DrawSphere (intersection, 0.2f);
					Gizmos.DrawLine (intersection, child.position);
				}

				// draw vertical line to below
				/*activeEdgeIndex = edgeList.Count - 1;
				Edge closestBelow = null;
				while ((activeEdgeIndex >= 0) && edgeList.GetEdgeAtIndex (activeEdgeIndex).CompareTo (child.position) == 1) {
					closestBelow = edgeList.GetEdgeAtIndex (activeEdgeIndex);
					activeEdgeIndex--;
				}
				if (closestBelow != null) {
					Vector2 intersection = closestBelow.GetIntersectionWith (scanline);
					Gizmos.color = Color.blue;

					Gizmos.DrawSphere (intersection, 0.2f);
					Gizmos.DrawLine (intersection, child.position);
				}*/

			}
		}

		/*foreach (var child in childs) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine (child.position,child.position + Vector3.up * 10);
			Gizmos.DrawLine (child.position,child.position - Vector3.up * 10);
		}*/

		// default draw
		for (int i = 0; i < transform.childCount; i++) {
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(transform.GetChild(GetCircularIndex (i)).position, transform.GetChild(GetCircularIndex(i+1)).position);
		}
	}

	public int GetCircularIndex(int index){
		while (index < 0)
			index += transform.childCount;
		return index % transform.childCount;
	}
}


