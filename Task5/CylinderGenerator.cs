using System;
using System.Collections.Generic;

namespace Task5
{
    public static class CylinderGenerator
    {
        public static List<Triangle> Create(double radius, double height, int segments)
        {
            List<Triangle> triangles = new();
            double bottomY = -height / 2.0;
            double topY = height / 2.0;

            Vec3 bottomCenter = new(0, bottomY, 0);
            Vec3 topCenter = new(0, topY, 0);
            Vec3 bottomNormal = new(0, -1, 0);
            Vec3 topNormal = new(0, 1, 0);

            for (int i = 0; i < segments; i++)
            {
                double angle0 = 2.0 * Math.PI * i / segments;
                double angle1 = 2.0 * Math.PI * (i + 1) / segments;

                Vec3 bottom0 = new(radius * Math.Cos(angle0), bottomY, radius * Math.Sin(angle0));
                Vec3 bottom1 = new(radius * Math.Cos(angle1), bottomY, radius * Math.Sin(angle1));
                Vec3 top0 = new(radius * Math.Cos(angle0), topY, radius * Math.Sin(angle0));
                Vec3 top1 = new(radius * Math.Cos(angle1), topY, radius * Math.Sin(angle1));

                Vec3 normal0 = new(Math.Cos(angle0), 0, Math.Sin(angle0));
                Vec3 normal1 = new(Math.Cos(angle1), 0, Math.Sin(angle1));

                Vertex b0Side = new(bottom0, normal0);
                Vertex b1Side = new(bottom1, normal1);
                Vertex t0Side = new(top0, normal0);
                Vertex t1Side = new(top1, normal1);

                triangles.Add(new Triangle(t0Side, b1Side, b0Side));
                triangles.Add(new Triangle(b1Side, t0Side, t1Side));

                triangles.Add(new Triangle(new Vertex(bottomCenter, bottomNormal), new Vertex(bottom0, bottomNormal), new Vertex(bottom1, bottomNormal)));

                triangles.Add(new Triangle(new Vertex(topCenter, topNormal), new Vertex(top1, topNormal), new Vertex(top0, topNormal)));
            }

            return triangles;
        }
    }

    public struct Vertex
    {
        public Vec3 Position { get; }
        public Vec3 Normal { get; }

        public Vertex(Vec3 position, Vec3 normal)
        {
            Position = position;
            Normal = normal;
        }
    }

    public struct Triangle
    {
        public Vertex A { get; }
        public Vertex B { get; }
        public Vertex C { get; }

        public Triangle(Vertex a, Vertex b, Vertex c)
        {
            A = a;
            B = b;
            C = c;
        }
    }

    public struct ProjectedVertex
    {
        public double ScreenX { get; }
        public double ScreenY { get; }
        public Vec3 CameraPosition { get; }
        public Vec3 WorldPosition { get; }
        public Vec3 WorldNormal { get; }

        public ProjectedVertex(double screenX, double screenY, Vec3 cameraPosition, Vec3 worldPosition, Vec3 worldNormal)
        {
            ScreenX = screenX;
            ScreenY = screenY;
            CameraPosition = cameraPosition;
            WorldPosition = worldPosition;
            WorldNormal = worldNormal;
        }
    }

    public struct Material
    {
        public ColorVec Ambient { get; set; }
        public ColorVec Diffuse { get; set; }
        public ColorVec Specular { get; set; }
        public double Shininess { get; set; }
    }

}
