using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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
/// container for active edges list, utilize binary search function of list
/// specifically for when inserting
/// </summary>
public class ActiveEdgesList{
	/// <summary>
	/// internal edge list
	/// </summary>
	private List<Edge> _edgeList;
	public List<Edge> EdgeList{
		get{
			if (_edgeList == null)
				_edgeList = new List<Edge> ();
			return _edgeList;
		}
	}

	/// <summary>
	/// return edge count
	/// </summary>
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
	/// <summary>
	/// whether we want to subdivide or not
	/// </summary>
	[SerializeField]
	private bool _subdivide = true;
	public bool Subdivide{
		get{ return _subdivide;}
		set{ _subdivide = value;}
	}

	/// <summary>
	/// the result subdivisions
	/// </summary>
	private List<List<Vector2>> _subdivisions;
	public List<List<Vector2>> Subdivisions{
		get{
			if (_subdivisions == null)
				_subdivisions = new List<List<Vector2>> ();
			return _subdivisions;
		}
	}

	private void OnDrawGizmos(){
		if (Subdivide) {
			ComputeSubdivisions ();
			foreach (var division in Subdivisions)
				DrawDivision (division);
		} else {
			for (int i = 0; i < transform.childCount; i++) {
				Gizmos.color = Color.yellow;
				Gizmos.DrawLine (transform.GetChild (GetCircularIndex (i)).position, transform.GetChild (GetCircularIndex (i + 1)).position);
			}
		}
	}
		
	public void OnValidate(){
		// make sure there's no vertical line, if there's any, shift it a tiny bit
		List<float> occupiedX = new List<float> ();
		for (int i = 0; i < transform.childCount; i++) {
			Transform child = transform.GetChild (GetCircularIndex (i));
			while (occupiedX.Any(item => Mathf.Approximately(item,child.position.x))) {
				Vector3 temp = child.position;
				child.position = new Vector3 (temp.x + 0.00001f, temp.y, temp.z);
			}
			occupiedX.Add (child.position.x);
		}
	}

	/// <summary>
	/// Draws the division from a list of vertices
	/// </summary>
	private void DrawDivision(List<Vector2> division){
		Gizmos.color = Color.cyan;
		Gizmos.DrawLine (division[0], division[1]);
		Gizmos.DrawLine (division[1], division[2]);
		Gizmos.DrawLine (division[2], division[3]);
		Gizmos.DrawLine (division[3], division[1]);
		Gizmos.DrawLine (division[3], division[0]);
	}

	/// <summary>
	/// Computes the subdivisions of the polygon
	/// </summary>
	public void ComputeSubdivisions(){
		OnValidate ();

		// clear subdivisions, we'll start from scratch
		Subdivisions.Clear ();

		if (transform.childCount <= 2) // need at least 3 vertices
			return; 

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

		// scan the polygon
		float prevScanline = 0;
		for(int i = 0; i < childs.Count; i++){
			Transform child = childs[i];

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

				// check condition of the two edges and determine condition of current vertex
				if (!e1.IsBehind (scanline) && !e2.IsBehind (scanline)) { 
					if (i > 0)
						CreateSubdivisions (edgeList, scanline, prevScanline);
						//CreateTriangles (edgeList, scanline, prevScanline);

					edgeList.Insert (e1);
					edgeList.Insert (e2);
				} else if (e1.IsBehind (scanline) && e2.IsBehind (scanline)) {
					if(i > 0)
						CreateSubdivisions (edgeList, scanline, prevScanline);
						//CreateTriangles (edgeList, scanline, prevScanline);

					edgeList.Remove (e1);
					edgeList.Remove (e2);
				} else {
					if(i > 0)
						CreateSubdivisions (edgeList, scanline, prevScanline);
						//CreateTriangles (edgeList, scanline, prevScanline);

					if (e1.IsBehind (scanline)) {
						edgeList.Remove (e1);
						edgeList.Insert (e2);
					} else {
						edgeList.Remove (e2);
						edgeList.Insert (e1);
					}
				}
				prevScanline = scanline;
			}
		}
	}

	/// <summary>
	/// helper function to loop along index of child 
	/// </summary>
	public int GetCircularIndex(int index){
		while (index < 0)
			index += transform.childCount;
		return index % transform.childCount;
	}

	/// <summary>
	/// Creates subdivisions from a set of edge list and 2 scanline positions
	/// </summary>
	public void CreateSubdivisions(ActiveEdgesList edgeList, float currentScan, float prevScan){
		// prepare the list of intersections
		List<Vector2> prevIntersections = new List<Vector2> ();
		List<Vector2> currentIntersections = new List<Vector2> ();
		for (int i = 0; i < edgeList.Count; i++) {
			Edge edge = edgeList.GetEdgeAtIndex (i);
			prevIntersections.Add (edge.GetIntersectionWith (prevScan));
			currentIntersections.Add (edge.GetIntersectionWith (currentScan));
		}

		// add subdivisions to the list
		for (int i = 0; i < currentIntersections.Count; i = i + 2) {
				List<Vector2> newDivision = new List<Vector2> ();
				newDivision.Add (prevIntersections [i]);
				newDivision.Add (currentIntersections [i]);
				newDivision.Add (currentIntersections [i + 1]);
				newDivision.Add (prevIntersections [i + 1]);
				Subdivisions.Add (newDivision);
		}
	}
}


