﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public struct VertexPath
{
    Vector3[] vertices;
    Vector3[] tangents;
    Vector3[] normals;
    float[] distances;

    public bool valid { get; private set; }

    public float length { get; private set; }

    public int VertexCount => vertices.Length;

    public Vector3 this[int index] => vertices[LoopIndex(index)];

    public Vector3 up { get; private set; }

    public Vector3 GetPositionAtDistance(float distance)
    {
        distance = LoopDistance(distance);

        for (int i = 0; i < vertices.Length - 1; i++)
        {
            if (distances[i] <= distance && distances[i + 1] >= distance)
            {
                float distanceBetweenPoints = distances[i + 1] - distances[i];
                float distanceSinceLastPoint = distance - distances[i];
                return Vector3.Lerp(vertices[i], vertices[i + 1], distanceSinceLastPoint / distanceBetweenPoints);
            }
        }

        return Vector3.zero;
    }

    public Vector3 GetPositionAtTime(float time)
    {
        return GetPositionAtDistance(time * length);
    }

    public Quaternion GetRotationAtDistance(float distance)
    {
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        distance = LoopDistance(distance);

        for (int i = 0; i < vertices.Length - 1; i++)
        {
            if (distances[i] <= distance && distances[i + 1] >= distance)
            {
                float distanceBetweenPoints = distances[i + 1] - distances[i];
                float distanceSinceLastPoint = distance - distances[i];

                forward = Vector3.Slerp(tangents[i], tangents[i + 1], distanceSinceLastPoint / distanceBetweenPoints);
                right = Vector3.Slerp(normals[i], normals[i + 1], distanceSinceLastPoint / distanceBetweenPoints);
            }
        }


        Vector3 up = Vector3.Cross(forward, right);
        return Quaternion.LookRotation(forward, up);
    }

    public void UpdatePath(Spline spline)
    {
        if (length == 0f)
            length = 1f;

        valid = true;
        up = spline.transform.up;

        float resolution = spline.resolution;
        if (resolution % 1 == 0)
            resolution += 0.001f;

        SplineVertexData vertexData = spline.CalculateEvenlySpacedPoints(length / resolution);

        vertices = vertexData.vertices;
        normals = vertexData.normals;
        tangents = vertexData.tangents;
        distances = vertexData.distances;
        length = vertexData.length;
    }

    public Vector3 GetTangent(int index)
    {
        return tangents[LoopIndex(index)];
    }

    public Vector3 GetNormal(int index)
    {
        return normals[LoopIndex(index)];
    }

    public float GetTime(int index)
    {
        return distances[LoopIndex(index)] / length;
    }

    public float GetDistance(int index)
    {
        return distances[LoopIndex(index)];
    }


    public int LoopIndex(int i)
    {
        int count = vertices.Length;
        return (((Mathf.Abs(i) % count) * (int)Mathf.Sign(i)) + count) % count;
    }

    public float LoopDistance(float distance)
    {
        return (((Mathf.Abs(distance) % length) * Mathf.Sign(distance)) + length) % length;
    }
}

