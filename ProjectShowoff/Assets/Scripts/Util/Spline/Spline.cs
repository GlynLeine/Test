﻿using System.Collections.Generic;
using UnityEngine;

public class SplineVertexData
{
    public Vector3[] vertices;
    public Vector3[] normals;
    public Vector3[] tangents;
    public float[] distances;
    public float length;
}

public class Spline : MonoBehaviour
{
    [SerializeField, HideInInspector]
    List<Vector3> points;
    [SerializeField, HideInInspector]
    List<Vector3> normals;

    [SerializeField, HideInInspector]
    bool automaticTangents;

    [Range(0.01f, 10f)]
    public float resolution = 1;

    VertexPath vertexPath;
    public float length;

    public VertexPath VertexPath
    {
        get
        {
            vertexPath.UpdatePath(this);
            length = vertexPath.length;
            return vertexPath;
        }
    }

    public bool AutoTangents
    {
        get
        {
            return automaticTangents;
        }

        set
        {
            if (automaticTangents != value)
            {
                automaticTangents = value;
                if (value)
                {
                    SetTangentsAuto();
                }
            }
        }
    }

    public void Reset()
    {
        points = new List<Vector3>
        {
            Vector2.left,
            (Vector2.left + Vector2.up) * 0.5f,
            (Vector2.right + Vector2.down) * 0.5f,
            Vector2.right
        };
        normals = new List<Vector3>
        {
            transform.up,
            transform.up
        };
    }

    public Vector3 this[int index]
    {
        get
        {
            return points[index];
        }
    }

    public int PointCount
    {
        get
        {
            return points.Count;
        }
    }

    public int SegmentCount
    {
        get
        {
            return points.Count / 3;
        }
    }

    public Vector3 GetWorldPoint(int index)
    {
        return transform.TransformPoint(points[index]);
    }

    public Vector3 GetWorldForward(int index)
    {
        return transform.TransformDirection(GetForward(index));
    }

    public Vector3 GetForward(int index)
    {
        Vector3 forward = Vector3.zero;
        if (index - 3 >= 0)
            forward += (points[index - 3] - points[index]).normalized;
        if (index + 3 < points.Count)
            forward -= (points[index + 3] - points[index]).normalized;

        forward.Normalize();
        return forward;
    }

    public Vector3 GetWorldNormal(int index)
    {
        return transform.TransformDirection(normals[index]);
    }

    public Vector3 GetNormal(int index)
    {
        return normals[index];
    }

    public void SetWorldNormal(int index, Vector3 normal)
    {
        normals[index] = transform.InverseTransformDirection(normal);
    }

    public void SetNormal(int index, Vector3 normal)
    {
        normals[index] = normal;
    }

    public void SplitSegment(Vector3 anchor, int segmentIndex)
    {
        points.InsertRange(segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, anchor, Vector3.zero });
        normals.Insert(segmentIndex, transform.up);
        if (automaticTangents)
        {
            SetTangentsAuto(segmentIndex * 3 + 3);
            ReportAutoTangentModification(segmentIndex * 3 + 3);
        }
        else
            SetTangentsAuto(segmentIndex * 3 + 3);
    }

    public void RemoveSegment(int anchorIndex)
    {
        if (SegmentCount < 2)
            return;

        if (anchorIndex == 0)
            points.RemoveRange(0, 3);
        else if (anchorIndex == points.Count - 1)
            points.RemoveRange(anchorIndex - 2, 3);
        else
            points.RemoveRange(anchorIndex - 1, 3);

        normals.RemoveAt(anchorIndex/3);
    }

    public void AddSegment(Vector3 anchor)
    {
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        points.Add((points[points.Count - 1] + anchor) * 2f);
        points.Add(anchor);
        normals.Add(transform.up);

        if (automaticTangents)
            ReportAutoTangentModification(points.Count - 1);
    }

    public Vector3[] GetPointsInSegment(int i)
    {
        return new Vector3[] { points[i * 3], points[i * 3 + 1], points[i * 3 + 2], points[i * 3 + 3] };
    }

    public Vector3[] GetWorldPointsInSegment(int i)
    {
        return new Vector3[] { transform.TransformPoint(points[i * 3]), transform.TransformPoint(points[i * 3 + 1]), transform.TransformPoint(points[i * 3 + 2]), transform.TransformPoint(points[i * 3 + 3]) };
    }

    public void MovePoint(int index, Vector3 position)
    {
        if (automaticTangents && index % 3 != 0)
            return;

        Vector3 deltaPos = position - points[index];

        points[index] = position;

        if (automaticTangents)
        {
            ReportAutoTangentModification(index);
        }
        else
        {
            if (index % 3 == 0)
            {
                if (index + 1 < points.Count)
                    points[index + 1] += deltaPos;
                if (index - 1 >= 0)
                    points[index - 1] += deltaPos;
            }
            else
            {
                bool nextIsAnchor = (index + 1) % 3 == 0;
                int anchorIndex = nextIsAnchor ? index + 1 : index - 1;
                int siblingIndex = nextIsAnchor ? index + 2 : index - 2;

                if (siblingIndex >= 0 && siblingIndex < points.Count)
                {
                    float distance = (points[anchorIndex] - points[siblingIndex]).magnitude;
                    Vector3 direction = (points[anchorIndex] - position).normalized;
                    points[siblingIndex] = points[anchorIndex] + direction * distance;
                }
            }
        }
    }

    public SplineVertexData CalculateEvenlySpacedPoints(float spacing, float resolution = 1)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> vertNormals = new List<Vector3>();
        List<Vector3> tangents = new List<Vector3>();
        List<float> distances = new List<float>();

        vertices.Add(points[0]);
        vertNormals.Add(normals[0]);
        distances.Add(0);
        Vector3 previousPoint = points[0];
        float dstSinceLastEvenPoint = 0;
        float length = 0;

        for (int segmentIndex = 0; segmentIndex < SegmentCount; segmentIndex++)
        {
            Vector3[] p = GetPointsInSegment(segmentIndex);
            float controlNetLength = Vector3.Distance(p[0], p[1]) + Vector3.Distance(p[1], p[2]) + Vector3.Distance(p[2], p[3]);
            float estimatedCurveLength = Vector3.Distance(p[0], p[3]) + controlNetLength / 2f;
            int divisions = Mathf.CeilToInt(estimatedCurveLength * resolution * 10);
            float t = 0;
            Vector3 currentNormal = normals[segmentIndex];
            Vector3 nextNormal = normals[segmentIndex + 1];
            while (t <= 1)
            {
                t += 1f / divisions;
                Vector3 pointOnCurve = Bezier.EvaluateCubic(p[0], p[1], p[2], p[3], t);
                dstSinceLastEvenPoint += Vector3.Distance(previousPoint, pointOnCurve);

                while (dstSinceLastEvenPoint >= spacing)
                {
                    float overshootDst = dstSinceLastEvenPoint - spacing;

                    Vector3 newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshootDst;
                    vertices.Add(newEvenlySpacedPoint);

                    vertNormals.Add(Vector3.Lerp(currentNormal, nextNormal, t).normalized);

                    length += dstSinceLastEvenPoint - overshootDst;
                    distances.Add(length);

                    dstSinceLastEvenPoint = overshootDst;
                    previousPoint = newEvenlySpacedPoint;
                }

                previousPoint = pointOnCurve;
            }
        }

        vertices.Add(points[points.Count - 1]);
        vertNormals.Add(normals[normals.Count-1]);

        length += Vector3.Distance(vertices[vertices.Count - 2], vertices[vertices.Count - 1]);
        distances.Add(length);

        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 forward = Vector3.zero;

            if (i - 1 >= 0)
                forward += (vertices[i - 1] - vertices[i]).normalized;
            if (i + 1 < vertices.Count)
                forward -= (vertices[i + 1] - vertices[i]).normalized;

            forward.Normalize();

            Vector3 tangent = Vector3.Cross(vertNormals[i], forward);
            tangents.Add(-forward);
        }

        SplineVertexData vertexData = new SplineVertexData();
        vertexData.vertices = vertices.ToArray();
        vertexData.normals = vertNormals.ToArray();
        vertexData.tangents = tangents.ToArray();
        vertexData.length = length;
        vertexData.distances = distances.ToArray();
        return vertexData;
    }

    void ReportAutoTangentModification(int modifiedAnchorIndex)
    {
        for (int i = modifiedAnchorIndex - 3; i <= modifiedAnchorIndex + 3; i += 3)
        {
            if (i >= 0 && i < points.Count)
                SetTangentsAuto(i);
        }
    }

    void SetTangentsAuto()
    {
        for (int i = 0; i < points.Count; i += 3)
        {
            SetTangentsAuto(i);
        }
    }

    void SetTangentsAuto(int anchorIndex)
    {
        if (anchorIndex == 0)
        {
            points[1] = (points[0] + points[2]) * 0.5f;
            return;
        }
        else if (anchorIndex == points.Count - 1)
        {
            points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * 0.5f;
            return;
        }

        Vector3 anchor = points[anchorIndex];
        Vector3 direction = Vector3.zero;
        float[] neighbourDistances = new float[2];

        if (anchorIndex - 3 >= 0)
        {
            Vector3 offset = points[anchorIndex - 3] - anchor;
            direction += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }

        if (anchorIndex + 3 < points.Count)
        {
            Vector3 offset = points[anchorIndex + 3] - anchor;
            direction -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }

        direction.Normalize();

        for (int i = 0; i < 2; i++)
        {
            int tangentIndex = anchorIndex + i * 2 - 1;
            if (tangentIndex >= 0 || tangentIndex < points.Count)
            {
                points[tangentIndex] = anchor + direction * neighbourDistances[i] * 0.5f;
            }
        }
    }
}