using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Task5
{
    public partial class MainWindow : Window
    {
        const int RenderWidth = 900;
        const int RenderHeight = 650;
        const double NearPlane = 0.1;
        const double FocalLength = 700.0;

        const double CylinderRadius = 1.0;
        const double CylinderHeight = 2.5;

        Vec3 LightPosition = new(3.0, 3.0, -4.0);
        ColorVec LightColor = new(1.0, 1.0, 1.0);
        ColorVec AmbientLight = new(0.18, 0.18, 0.18);

        private readonly Material _material = new()
        {
            Ambient = new ColorVec(0.10, 0.20, 0.35),
            Diffuse = new ColorVec(0.20, 0.55, 0.95),
            Specular = new ColorVec(0.90, 0.90, 0.90),
            Shininess = 35.0
        };

        WriteableBitmap? bitmap;
        byte[] pixels = Array.Empty<byte>();
        List<Triangle> mesh = new();

        public MainWindow()
        {
            InitializeComponent();

            bitmap = new WriteableBitmap(RenderWidth, RenderHeight, 96, 96, PixelFormats.Bgra32, null);
            pixels = new byte[RenderWidth * RenderHeight * 4];
            RenderImage.Source = bitmap;

            GenerateMesh();
            UpdateLabels();
            RenderScene();
        }

        private void ControlChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (bitmap == null)
                return;

            UpdateLabels();
            RenderScene();
        }

        private void SegmentsChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (bitmap == null)
                return;

            GenerateMesh();
            UpdateLabels();
            RenderScene();
        }

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            AngleXSlider.Value = 20;
            AngleYSlider.Value = -25;
            DistanceSlider.Value = 5.2;
            SegmentsSlider.Value = 48;

            GenerateMesh();
            UpdateLabels();
            RenderScene();
        }

        private void UpdateLabels()
        {
            AngleXText.Text = $"Camera rotation around X: {AngleXSlider.Value:F0}°";
            AngleYText.Text = $"Camera rotation around Y: {AngleYSlider.Value:F0}°";
            DistanceText.Text = $"Distance from scene centre: {DistanceSlider.Value:F1}";
            SegmentsText.Text = $"Cylinder subdivisions: {(int)SegmentsSlider.Value}";
        }

        private void GenerateMesh()
        {
            int segments = Math.Max(8, (int)SegmentsSlider.Value);
            mesh = CylinderGenerator.Create(CylinderRadius, CylinderHeight, segments);
        }

        private void RenderScene()
        {
            ClearPixels(new ColorVec(0.06, 0.06, 0.07));

            double angleX = DegreesToRadians(AngleXSlider.Value);
            double angleY = DegreesToRadians(AngleYSlider.Value);
            double distance = DistanceSlider.Value;

            // Build view matrix
            Matrix4 view = Matrix4.Translation(0, 0, distance) * Matrix4.RotationX(angleX) * Matrix4.RotationY(angleY);
            Matrix4 inverseView = view.InverseRigid();
            Vec3 cameraPosition = inverseView.TransformPoint(Vec3.Zero);

            foreach (Triangle triangle in mesh)
            {
                DrawTriangle(triangle, view, cameraPosition);
            }

            bitmap!.WritePixels(new Int32Rect(0, 0, RenderWidth, RenderHeight), pixels, RenderWidth * 4, 0);
        }

        private void DrawTriangle(Triangle triangle, Matrix4 view, Vec3 cameraPosition)
        {
            ProjectedVertex v0 = ProjectVertex(triangle.A, view);
            ProjectedVertex v1 = ProjectVertex(triangle.B, view);
            ProjectedVertex v2 = ProjectVertex(triangle.C, view);

            if (v0.CameraPosition.Z <= NearPlane || v1.CameraPosition.Z <= NearPlane || v2.CameraPosition.Z <= NearPlane)
                return;

            // Skip triangles turned away from the camera 
            Vec3 edge1 = v1.CameraPosition - v0.CameraPosition;
            Vec3 edge2 = v2.CameraPosition - v0.CameraPosition;
            Vec3 faceNormal = Vec3.Cross(edge1, edge2).Normalized();
            Vec3 faceCenter = (v0.CameraPosition + v1.CameraPosition + v2.CameraPosition) / 3.0;
            Vec3 toCamera = (Vec3.Zero - faceCenter).Normalized();

            if (Vec3.Dot(faceNormal, toCamera) <= 0)
                return;

            RasterizeTriangle(v0, v1, v2, cameraPosition);
        }

        private ProjectedVertex ProjectVertex(Vertex vertex, Matrix4 view)
        {
            Vec3 cameraPosition = view.TransformPoint(vertex.Position);

            double x = RenderWidth / 2.0 + FocalLength * cameraPosition.X / cameraPosition.Z;
            double y = RenderHeight / 2.0 - FocalLength * cameraPosition.Y / cameraPosition.Z;

            return new ProjectedVertex(x, y, cameraPosition, vertex.Position, vertex.Normal);
        }

        private void RasterizeTriangle(ProjectedVertex a, ProjectedVertex b, ProjectedVertex c, Vec3 cameraPosition)
        {
            ProjectedVertex v0 = a, v1 = b, v2 = c;

            // Sort vertices by Y so the triangle can be filled from top to bottom
            if (v0.ScreenY > v1.ScreenY) (v0, v1) = (v1, v0);
            if (v0.ScreenY > v2.ScreenY) (v0, v2) = (v2, v0);
            if (v1.ScreenY > v2.ScreenY) (v1, v2) = (v2, v1);

            double totalHeight = v2.ScreenY - v0.ScreenY;
            if (totalHeight < 0.001) return;

            double iz0 = 1.0 / v0.CameraPosition.Z;
            double iz1 = 1.0 / v1.CameraPosition.Z;
            double iz2 = 1.0 / v2.CameraPosition.Z;

            Vec3 pz0 = v0.WorldPosition * iz0, pz1 = v1.WorldPosition * iz1, pz2 = v2.WorldPosition * iz2;
            Vec3 nz0 = v0.WorldNormal * iz0, nz1 = v1.WorldNormal * iz1, nz2 = v2.WorldNormal * iz2;

            int yStart = Math.Max(0, (int)Math.Ceiling(v0.ScreenY - 0.5));
            int yEnd = Math.Min(RenderHeight - 1, (int)Math.Floor(v2.ScreenY - 0.5));

            for (int y = yStart; y <= yEnd; y++)
            {
                double py = y + 0.5;

                double tLong = (py - v0.ScreenY) / totalHeight;
                double xLong = v0.ScreenX + tLong * (v2.ScreenX - v0.ScreenX);
                double izLong = iz0 + tLong * (iz2 - iz0);
                Vec3 pzLong = pz0 + (pz2 - pz0) * tLong;
                Vec3 nzLong = nz0 + (nz2 - nz0) * tLong;

                double xShort, izShort;
                Vec3 pzShort, nzShort;

                // Choose which short edge is active for this scanline
                if (py < v1.ScreenY)
                {
                    double topHeight = v1.ScreenY - v0.ScreenY;
                    if (topHeight < 0.001) continue;
                    double t = (py - v0.ScreenY) / topHeight;
                    xShort = v0.ScreenX + t * (v1.ScreenX - v0.ScreenX);
                    izShort = iz0 + t * (iz1 - iz0);
                    pzShort = pz0 + (pz1 - pz0) * t;
                    nzShort = nz0 + (nz1 - nz0) * t;
                }
                else
                {
                    double bottomHeight = v2.ScreenY - v1.ScreenY;
                    if (bottomHeight < 0.001) continue;
                    double t = (py - v1.ScreenY) / bottomHeight;
                    xShort = v1.ScreenX + t * (v2.ScreenX - v1.ScreenX);
                    izShort = iz1 + t * (iz2 - iz1);
                    pzShort = pz1 + (pz2 - pz1) * t;
                    nzShort = nz1 + (nz2 - nz1) * t;
                }

                double xLeft = xLong, xRight = xShort;
                double izLeft = izLong, izRight = izShort;
                Vec3 pzLeft = pzLong, pzRight = pzShort;
                Vec3 nzLeft = nzLong, nzRight = nzShort;

                if (xLeft > xRight)
                {
                    (xLeft, xRight) = (xRight, xLeft);
                    (izLeft, izRight) = (izRight, izLeft);
                    (pzLeft, pzRight) = (pzRight, pzLeft);
                    (nzLeft, nzRight) = (nzRight, nzLeft);
                }

                int xStart = Math.Max(0, (int)Math.Ceiling(xLeft - 0.5));
                int xEnd = Math.Min(RenderWidth - 1, (int)Math.Floor(xRight - 0.5));
                double spanWidth = xRight - xLeft;

                if (spanWidth < 0.001)
                    continue;

                for (int x = xStart; x <= xEnd; x++)
                {
                    double t = (x + 0.5 - xLeft) / spanWidth;

                    // Interpolate 3D data for this pixel
                    double iz = izLeft + t * (izRight - izLeft);
                    Vec3 worldPosition = (pzLeft + (pzRight - pzLeft) * t) / iz;
                    Vec3 normal = ((nzLeft + (nzRight - nzLeft) * t) / iz).Normalized();

                    ColorVec color = CalculatePhongColor(worldPosition, normal, cameraPosition);
                    PutPixel(x, y, color);
                }
            }
        }

        private ColorVec CalculatePhongColor(Vec3 point, Vec3 normal, Vec3 cameraPosition)
        {
            Vec3 n = normal.Normalized();
            Vec3 l = (LightPosition - point).Normalized();
            Vec3 v = (cameraPosition - point).Normalized();

            ColorVec ambient = _material.Ambient * AmbientLight;

            double diffuseFactor = Math.Max(0.0, Vec3.Dot(n, l));
            ColorVec diffuse = (_material.Diffuse * LightColor) * diffuseFactor;

            ColorVec specular = ColorVec.Black;
            if (diffuseFactor > 0.0)
            {
                Vec3 r = (n * (2.0 * Vec3.Dot(n, l)) - l).Normalized();
                double specularFactor = Math.Pow(Math.Max(0.0, Vec3.Dot(r, v)), _material.Shininess);
                specular = (_material.Specular * LightColor) * specularFactor;
            }

            return (ambient + diffuse + specular).Clamped();
        }
        private void ClearPixels(ColorVec color)
        {
            byte r = ToByte(color.R);
            byte g = ToByte(color.G);
            byte b = ToByte(color.B);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = b;
                pixels[i + 1] = g;
                pixels[i + 2] = r;
                pixels[i + 3] = 255;
            }
        }

        private void PutPixel(int x, int y, ColorVec color)
        {
            if (x < 0 || x >= RenderWidth || y < 0 || y >= RenderHeight)
                return;

            color = color.Clamped();
            int index = (y * RenderWidth + x) * 4;
            pixels[index] = ToByte(color.B);
            pixels[index + 1] = ToByte(color.G);
            pixels[index + 2] = ToByte(color.R);
            pixels[index + 3] = 255;
        }

        private byte ToByte(double value)
        {
            value = Math.Clamp(value, 0.0, 1.0);
            return (byte)(value * 255.0);
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}

