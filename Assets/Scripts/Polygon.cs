using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

/// <summary>
/// an edge connects 2 vertices
/// </summary>
public class Edge:IComparable<Edge>{
	public Vector2 v1;
	public Vector2 v2;

	/// <summary>
	/// constructor. we make sure that v1 is always on the left of v2;
	/// </summary>
	public Edge(Vector2 v1,Vector2 v2){
		if (v1.x.CompareTo(v2.x) > 0) {
			this.v1 = v2;
			this.v2 = v1;
		} else {
			this.v1 = v1;
			this.v2 = v2;
		}
	}

	/// <summary>
	/// to be used with scanline, determines whether the edge is behind the scanline
	/// </summary>
	public bool IsBehind(float x){
		return v1.x < x;
	}

	/// <summary>
	/// get tangent of the edge
	/// </summary>
	public float GetTangent(){
		return (v2.y - v1.y) / (v2.x - v1.x);
	}

	/// <summary>
	/// Compares whether a line is above or below a vertex
	/// </summary>
	public int CompareTo(Vector2 v){
		// find equation of edge
		float tan = GetTangent();
		float b = v1.y - tan * v1.x;

		// input vertex' x end check for y
		float delta = v.y -(v.x * tan + b);

		if (delta < 0.0)
			return 1;
		else if (delta > 0)
			return -1;
		else
			return 0;
	}

	/// <summary>
	/// Gets the intersection with line X = x
	/// </summary>
	public Vector2 GetIntersectionWith (float x){
		// find equation of edge
		float tan = GetTangent();
		float b = v1.y - tan * v1.x;
		return new Vector2 (x, x * tan + b);
	}

	#region IComparable implementation

	/// <summary>
	/// compares an edge with another edge (which one is above)
	/// </summary>
	public int CompareTo (Edge e)
	{
		// find common x1 and x2
		float x1 = Mathf.Max(this.v1.x,e.v1.x);
		float x2 = Mathf.Min(this.v2.x,e.v2.x);
		if (x1 < x2) { // there are common area along x between e1 and e2
			// find equation of e1
			float tan1 = this.GetTangent();
			float b1 = this.v1.y - tan1 * this.v1.x;

			// find equation of e2
			float tan2 = e.GetTangent();
			float b2 = e.v1.y - tan2 * e.v1.x;

			// find y1 for both edge
			float y1e1 = tan1 * x1 + b1;
			float y1e2 = tan2 * x1 + b2;

			if (Mathf.Approximately (y1e1, y1e2)) { // left corner case, left of the edges are the same
				// compute for y2
				float y2e1 = tan1 * x2 + b1;
				float y2e2 = tan2 * x2 + b2;
				return y2e2.CompareTo (y2e1);
			} else { // left of the edges are separated, compare which one is above
				return y1e2.CompareTo (y1e1);
			}
		} else { // there is no common area along x between e1 and e2
			return 0;
		}
	}

	#endregion
}

/// <summary>
/// helper comparator static class
/// </summary>
public static class Comparator{
	public static int Compare2(Edge e1, Edge e2){
		// find common x1 and x2
		float x1 = Mathf.Max(e1.v1.x,e2.v1.x);
		float x2 = Mathf.Min(e1.v2.x,e2.v2.x);
		if (x1 < x2) { // there are common area along x between e1 and e2
			// find equation of e1
			float tan1 = e1.GetTangent();
			float b1 = e1.v1.y - tan1 * e1.v1.x;

			// find equation of e2
			float tan2 = e2.GetTangent();
			float b2 = e2.v1.y - tan2 * e2.v1.x;

			// find y1 for both edge
			float y1e1 = tan1 * x1 + b1;
			float y1e2 = tan2 * x1 + b2;

			if (Mathf.Approximately (y1e1, y1e2)) { // left corner case
				// compute for y2
				float y2e1 = tan1 * x2 + b1;
				float y2e2 = tan2 * x2 + b2;
				return y2e2.CompareTo (y2e1);
			} else {
				return y1e2.CompareTo (y1e1);
			}
		} else { // there are no common area along x between e1 and e2
			return 0;
		}
	}
}

/// <summary>
/// container for active edges list, utilize binary search function of list
/// specifically for when inserting
/// </summary>
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

	/// <summary>
	/// insert new edge to the list
	/// </summary>
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

	/// <summary>
	/// remove an edge from the list
	/// </summary>
	public void Remove(Edge e){
		int index = EdgeList.BinarySearch (e);
		if(index >= 0)
			EdgeList.RemoveAt (index);
	}

	/// <summary>
	/// gets the edge at a specific index
	/// </summary>
	public Edge GetEdgeAtIndex(int i){
		if (i < EdgeList.Count)
			return EdgeList [i];
		return null;
	}

	/// <summary>
	/// Gets the index of this edge
	/// </summary>
	public int GetIndexOf(Edge e){
		return EdgeList.BinarySearch (e);
	}
}

public class Polygon : MonoBehaviour {
	private void CreateTriangles(ActiveEdgesList edgeList, float currentScan, float prevScan){
		List<Vector2> prevIntersections = new List<Vector2> ();
		List<Vector2> currentIntersections = new List<Vector2> ();

		for (int i = 0; i < edgeList.Count; i++) {
			Edge edge = edgeList.GetEdgeAtIndex (i);
			prevIntersections.Add (edge.GetIntersectionWith (prevScan));
			currentIntersections.Add (edge.GetIntersectionWith (currentScan));
		}

		for (int i = 0; i < currentIntersections.Count; i = i + 2) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine (prevIntersections[i], currentIntersections[i]);
			Gizmos.DrawLine (currentIntersections[i], currentIntersections[i+1]);
			Gizmos.DrawLine (currentIntersections[i+1], prevIntersections[i+1]);
			Gizmos.DrawLine (prevIntersections[i+1], currentIntersections[i]);
			Gizmos.DrawLine (prevIntersections[i+1], prevIntersections[i]);
		}
	}

	private void OnDrawGizmos(){			
		if (transform.childCount <= 2) // need at least 3 vertices
			return; 

		// default draw
		for (int i = 0; i < transform.childCount; i++) {
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(transform.GetChild(GetCircularIndex (i)).position, transform.GetChild(GetCircularIndex(i+1)).position);
		}

		// form dictionary transform -> index (for easy access of neighboring edges)
		Dictionary<Transform,int> transformIndexMap = new Dictionary<Transform, int>();
		for (int i = 0; i < transform.childCount; i++) 
			transformIndexMap.Add (transform.GetChild(i),i);

		// get the list of child transform and sort it on x
		List<Transform> childs = new List<Transform>();
		for (int i = 0; i < transform.childCount; i++)
			childs.Add (transform.GetChild (i));
		childs.Sort ((a, b) => a.position.x.CompareTo(b.position.x));

		// construct active list of scanned edges
		ActiveEdgesList edgeList = new ActiveEdgesList ();
		List<Edge> activeEdges = new List<Edge> ();

		int test = 0;
		// scan the polygon
		float prevScanline = 0;
		foreach (var child in childs) {
			// get index of transform in the list
			int index;
			if(transformIndexMap.TryGetValue(child,out index)){
				// define scanline x
				float scanline = child.position.x;

				// get the next and previous transform
				Transform next = transform.GetChild(GetCircularIndex(index+1));
				Transform previous = transform.GetChild(GetCircularIndex(index-1));

				// construct the edges that intersects with current vertex
				Edge e1 = new Edge(child.position,next.position);
				Edge e2 = new Edge (child.position, previous.position);

				// swap e2 and e1 if e2 is above e1 for easier processing later
				if (e1.CompareTo (e2) > 0) {
					Vector3 temp = e2.v1;
					e2.v1 = e1.v1;
					e1.v1 = temp;

					temp = e2.v2;
					e2.v2 = e1.v2;
					e1.v2 = temp;
				}
					

				// check condition of the two edges and determine condition of current vertex
				if (!e1.IsBehind (scanline) && !e2.IsBehind (scanline)) { 
					Gizmos.color = Color.green;
					Gizmos.DrawSphere (child.position, 0.3f);

					if(test > 0){
						CreateTriangles (edgeList, scanline, prevScanline);
					}

					edgeList.Insert (e1);
					edgeList.Insert (e2);

					int ie1 = edgeList.GetIndexOf (e1);
					int ie2 = edgeList.GetIndexOf (e2);

					// we know that ie1 is > ie2, we'll go from ie1 looking up and from ie2 loking down
					if (ie1 % 2 > 0) { // only draw line if there's at least an odd number of active edge above this vertex
						Edge closestAbove = edgeList.GetEdgeAtIndex(ie1 - 1);
						Vector2 intersection = closestAbove.GetIntersectionWith (scanline);
						Gizmos.color = Color.blue;
						Gizmos.DrawSphere (intersection, 0.3f);
						Gizmos.DrawLine (intersection, child.position);
					}

					if ((edgeList.Count - 1 - ie2) % 2 > 0) { // only draw line if there's at least an odd number of active edge below this vertex
						Edge closestBelow = edgeList.GetEdgeAtIndex(ie2 + 1);
						Vector2 intersection = closestBelow.GetIntersectionWith (scanline);
						Gizmos.color = Color.blue;
						Gizmos.DrawSphere (intersection, 0.3f);
						Gizmos.DrawLine (intersection, child.position);
					}
				
					activeEdges.Add (e1);
					activeEdges.Add (e2);
				} else if (e1.IsBehind (scanline) && e2.IsBehind (scanline)) {
					Gizmos.color = Color.red;
					Gizmos.DrawSphere (child.position, 0.3f);

					if(test > 0){
						CreateTriangles (edgeList, scanline, prevScanline);
					}

					// draw first as we're going to remove these edges from the active list
					int ie1 = edgeList.GetIndexOf (e1);
					int ie2 = edgeList.GetIndexOf (e2);

					// we know that ie1 is > ie2, we'll go from ie1 looking up and from ie2 loking down
					if (ie1 % 2 > 0) { // only draw line if there's at least an odd number of active edge above this vertex
						Edge closestAbove = edgeList.GetEdgeAtIndex(ie1 - 1);
						Vector2 intersection = closestAbove.GetIntersectionWith (scanline);
						Gizmos.color = Color.blue;
						Gizmos.DrawSphere (intersection, 0.3f);
						Gizmos.DrawLine (intersection, child.position);
					}

					if ((edgeList.Count - 1 - ie2) % 2 > 0) { // only draw line if there's at least an odd number of active edge below this vertex
						Edge closestBelow = edgeList.GetEdgeAtIndex(ie2 + 1);
						Vector2 intersection = closestBelow.GetIntersectionWith (scanline);
						Gizmos.color = Color.blue;
						Gizmos.DrawSphere (intersection, 0.3f);
						Gizmos.DrawLine (intersection, child.position);
					}

					edgeList.Remove (e1);
					edgeList.Remove (e2);

					activeEdges.RemoveAt (activeEdges.FindIndex (e => e.v1 == e1.v1 && e.v2 == e1.v2));
					activeEdges.RemoveAt (activeEdges.FindIndex (e => e.v1 == e2.v1 && e.v2 == e2.v2));
				} else {
					Gizmos.color = Color.white;
					Gizmos.DrawSphere (child.position, 0.3f);

					if(test > 0){
						CreateTriangles (edgeList, scanline, prevScanline);
					}

					if (e1.IsBehind (scanline)) {
						edgeList.Remove (e1);
						edgeList.Insert (e2);

						// we only have 1 edge to look above and below now
						int ie2 = edgeList.GetIndexOf (e2);

						if (ie2 % 2 > 0) { // only draw line if there's at least an odd number of active edge above this vertex
							Edge closestAbove = edgeList.GetEdgeAtIndex(ie2 - 1);
							Vector2 intersection = closestAbove.GetIntersectionWith (scanline);
							Gizmos.color = Color.blue;
							Gizmos.DrawSphere (intersection, 0.3f);
							Gizmos.DrawLine (intersection, child.position);
						}

						if ((edgeList.Count - 1 - ie2) % 2 > 0) { // only draw line if there's at least an odd number of active edge below this vertex
							Edge closestBelow = edgeList.GetEdgeAtIndex(ie2 + 1);
							Vector2 intersection = closestBelow.GetIntersectionWith (scanline);
							Gizmos.color = Color.blue;
							Gizmos.DrawSphere (intersection, 0.3f);
							Gizmos.DrawLine (intersection, child.position);
						}

						activeEdges.RemoveAt (activeEdges.FindIndex (e => e.v1 == e1.v1 && e.v2 == e1.v2));
						activeEdges.Add (e2);
					} else {
						edgeList.Remove (e2);
						edgeList.Insert (e1);

						// we only have 1 edge to look above and below now
						int ie1 = edgeList.GetIndexOf (e1);

						if (ie1 % 2 > 0) { // only draw line if there's at least an odd number of active edge above this vertex
							Edge closestAbove = edgeList.GetEdgeAtIndex(ie1 - 1);
							Vector2 intersection = closestAbove.GetIntersectionWith (scanline);
							Gizmos.color = Color.blue;
							Gizmos.DrawSphere (intersection, 0.3f);
							Gizmos.DrawLine (intersection, child.position);
						}

						if ((edgeList.Count - 1 - ie1) % 2 > 0) { // only draw line if there's at least an odd number of active edge below this vertex
							Edge closestBelow = edgeList.GetEdgeAtIndex(ie1 + 1);
							Vector2 intersection = closestBelow.GetIntersectionWith (scanline);
							Gizmos.color = Color.blue;
							Gizmos.DrawSphere (intersection, 0.3f);
							Gizmos.DrawLine (intersection, child.position);
						}

						activeEdges.RemoveAt (activeEdges.FindIndex (e => e.v1 == e2.v1 && e.v2 == e2.v2));
						activeEdges.Add (e1);
					}
				}

				activeEdges.Sort ((a,b)=>Comparator.Compare2 (a,b));

				// debug
				if (test == 1) {
					
				}
				test++;
				prevScanline = scanline;
				// end debug
			}
		}
	}

	/// <summary>
	/// helper function to sweep along index of child as a circular loop
	/// </summary>
	public int GetCircularIndex(int index){
		while (index < 0)
			index += transform.childCount;
		return index % transform.childCount;
	}
}


