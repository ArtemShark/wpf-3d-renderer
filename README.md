# WPF 3D Cylinder Renderer

A software 3D renderer built from scratch in WPF — no OpenGL, no DirectX, just raw pixel manipulation. Renders a shaded cylinder with interactive camera controls. Part of the "Computer Graphics 1" course at Warsaw University of Technology.

## What's implemented

- **Procedural mesh generation** — cylinder built from triangles with configurable subdivision count
- **Perspective projection** — 3D to 2D with focal length and near plane clipping
- **Scanline triangle rasterization** — fills triangles pixel by pixel with perspective-correct interpolation
- **Phong shading** — ambient + diffuse + specular lighting computed per pixel using interpolated normals
- **Backface culling** — skips triangles facing away from the camera
- **Custom math library** — Vec3, ColorVec, Matrix4 with rotation, translation, and inverse transforms
- **Interactive camera** — rotate around X/Y axes and adjust distance via sliders

## Controls

- **Camera X/Y rotation** — orbit around the cylinder
- **Distance** — zoom in/out
- **Subdivisions** — increase/decrease mesh detail (8–128 segments)
- **Reset** — return to default view

## Tech

C#, WPF, .NET 8, software rasterization, Phong shading model
