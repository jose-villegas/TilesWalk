﻿using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;

namespace TilesWalk.Tile.Editor
{
	[CustomEditor(typeof(TileView))]
	public class TileViewEditor : NaughtyInspector
	{
		[DrawGizmo(GizmoType.Selected)]
		private static void RenderCustomGizmoSelected(Transform objectTransform, GizmoType gizmoType)
		{
			DrawHandles(objectTransform);
		}

		[DrawGizmo(GizmoType.NonSelected)]
		private static void RenderCustomGizmoNonSelected(Transform objectTransform, GizmoType gizmoType)
		{
			DrawHandles(objectTransform);
		}

		private static void DrawHandles(Transform objectTransform)
		{
			var t = objectTransform.GetComponent<TileView>();

			if (t == null) return;

			var tile = t.Controller.Tile;

			Handles.color = Handles.xAxisColor;
			var rotation = Quaternion.LookRotation(tile.Right, tile.Forward);
			Handles.ArrowHandleCap(0, t.transform.position, rotation, 1, EventType.Repaint);

			Handles.color = Handles.yAxisColor;
			rotation = Quaternion.LookRotation(tile.Up, tile.Right);
			Handles.ArrowHandleCap(0, t.transform.position, rotation, 1, EventType.Repaint);

			Handles.color = Handles.zAxisColor;
			rotation = Quaternion.LookRotation(tile.Forward, tile.Up);
			Handles.ArrowHandleCap(0, t.transform.position, rotation, 1, EventType.Repaint);
		}
	}
}
