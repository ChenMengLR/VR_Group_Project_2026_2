#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CourseSetup
{
    // Keep physics meshes intact; author metre-scaled texture coordinates on a render-only copy.
    internal static class CabinMeshAssets
    {
        internal const string Folder = "Assets/05_Models/ShipCabin";

        internal static void Surface(Transform part, Material material, float repeatMetres)
        {
            var filter = part.GetComponent<MeshFilter>();
            string key = part.name + "_Surface";
            var source = filter.sharedMesh;
            if (source.name == key)
                source = part.TryGetComponent<MeshCollider>(out var collider) ? collider.sharedMesh
                    : Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var vertices = source.vertices;
            var indices = source.triangles;
            var positions = new Vector3[indices.Length];
            var normals = new Vector3[indices.Length];
            var uv = new Vector2[indices.Length];
            var triangles = new int[indices.Length];
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 a = vertices[indices[i]], b = vertices[indices[i+1]], c = vertices[indices[i+2]];
                var normal = Vector3.Cross(b-a,c-a).normalized;
                // Inside a hull face, text should read left to right when viewed from the cabin.
                bool horizontal = Mathf.Abs(normal.y) > .6f;
                Vector3 right = horizontal ? Vector3.right : Vector3.Cross(Vector3.up,-normal).normalized;
                Vector3 up = horizontal ? Vector3.forward : Vector3.up;
                for (int j = i; j < i+3; j++)
                {
                    positions[j] = vertices[indices[j]];
                    normals[j] = normal;
                    var p = Vector3.Scale(positions[j],part.lossyScale);
                    uv[j] = new Vector2(Vector3.Dot(p,right),Vector3.Dot(p,up))/repeatMetres + Vector2.one*.5f;
                    triangles[j] = j;
                }
            }
            var mesh = new Mesh {name=key, vertices=positions, normals=normals, uv=uv, triangles=triangles};
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(mesh);
            filter.sharedMesh = Save(key, mesh);
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveGI = ReceiveGI.Lightmaps;
            GameObjectUtility.SetStaticEditorFlags(part.gameObject,
                GameObjectUtility.GetStaticEditorFlags(part.gameObject) | StaticEditorFlags.ContributeGI);
        }

        private static Mesh Save(string key, Mesh mesh)
        {
            string path = Folder+"/"+key+".asset";
            mesh.name=key;
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh,path); return mesh; }
            EditorUtility.CopySerialized(mesh,existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        internal static Mesh LandingMark()
        {
            var points = new List<Vector3>();
            var triangles = new List<int>();
            Vector3 Point(float degrees, float r)
            {
                float a=degrees*Mathf.Deg2Rad;
                return new Vector3(Mathf.Sin(a)*r,1f,Mathf.Cos(a)*r);
            }
            void Arc(float start,float end,float outer,float inner)
            {
                const int segments=10;
                for(int i=0;i<segments;i++)
                {
                    int first=points.Count;
                    float a=Mathf.Lerp(start,end,i/(float)segments);
                    float b=Mathf.Lerp(start,end,(i+1f)/segments);
                    points.Add(Point(a,outer));points.Add(Point(b,outer));
                    points.Add(Point(b,inner));points.Add(Point(a,inner));
                    triangles.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
                }
            }
            for(int i=0;i<6;i++) Arc(60f*i+5f,60f*i+55f,.5f,.472f);
            for(int i=0;i<4;i++) Arc(90f*i-3f,90f*i+3f,.453f,.385f);
            var mark=new Mesh();
            mark.SetVertices(points);mark.SetTriangles(triangles,0);
            mark.RecalculateNormals();mark.RecalculateBounds();
            return Save("LandingMark",mark);
        }

        // One decorative mesh per finish/circuit. The fittings have no colliders.
        internal sealed class Fittings
        {
            private readonly Dictionary<Material,List<CombineInstance>> groups = new Dictionary<Material,List<CombineInstance>>();
            private readonly Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            private readonly Mesh cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");

            internal void Box(Material finish, Vector3 center, Vector3 size, Quaternion? rotation = null)
                => Add(finish,cube,Matrix4x4.TRS(center,rotation ?? Quaternion.identity,size));
            internal void Beam(Material finish, Vector3 a, Vector3 b, float width, float depth)
                => Box(finish,(a+b)*.5f,new Vector3(width,depth,Vector3.Distance(a,b)),Quaternion.LookRotation(b-a));
            internal void Pipe(Material finish, Vector3 a, Vector3 b, float radius)
                => Add(finish,cylinder,Matrix4x4.TRS((a+b)*.5f,Quaternion.FromToRotation(Vector3.up,(b-a).normalized),
                    new Vector3(2*radius,Vector3.Distance(a,b)*.5f,2*radius)));

            private void Add(Material finish, Mesh mesh, Matrix4x4 matrix)
            {
                if (!groups.TryGetValue(finish,out var instances)) groups[finish]=instances=new List<CombineInstance>();
                instances.Add(new CombineInstance{mesh=mesh,transform=matrix});
            }

            internal Dictionary<Material,Renderer> Build(Transform parent)
            {
                var result = new Dictionary<Material,Renderer>();
                foreach (var entry in groups)
                {
                    string name="Fittings_"+entry.Key.name;
                    var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};
                    mesh.CombineMeshes(entry.Value.ToArray(),true,true);
                    if (mesh.vertexCount<65535)
                    {
                        // The assembled mesh is small enough for mobile 16-bit indices.
                        var indices=mesh.triangles;
                        mesh.indexFormat=IndexFormat.UInt16;
                        mesh.triangles=indices;
                    }
                    var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
                    go.transform.SetParent(parent,false);
                    go.GetComponent<MeshFilter>().sharedMesh=Save(name,mesh);
                    var renderer=go.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial=entry.Key;
                    renderer.shadowCastingMode=ShadowCastingMode.Off;
                    result.Add(entry.Key,renderer);
                }
                return result;
            }
        }
    }
}
#endif
