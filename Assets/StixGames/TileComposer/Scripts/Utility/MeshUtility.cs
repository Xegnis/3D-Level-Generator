using UnityEngine;

namespace StixGames.TileComposer
{
    public class MeshUtility
    {
        /// <summary>
        /// Calculates barycentric coordinates for wireframe rendering.
        /// If a diagonal length is added, the algorithm will add the index of the diagonal in the alpha channel.
        /// The diagonal will be found by checking if the distance between two vertices is larger than the max non diagonal length
        /// </summary>
        /// <param name="mesh"></param>
        public static void CalculateBarycentricCoordinates(Mesh mesh, float maxNonDiagonalLength = -1)
        {
            var barycentricCoordinates = new Color[mesh.vertexCount];
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var alpha = 1.0f;
                
                // Find diagonal edge
                if (maxNonDiagonalLength > 0)
                {
                    if (Vector3.Distance(vertices[i], vertices[i+1]) > maxNonDiagonalLength)
                    {
                        alpha = 0.0f;
                    } 
                    else if (Vector3.Distance(vertices[i+1], vertices[i+2]) > maxNonDiagonalLength)
                    {
                        alpha = 1.0f / 3.0f;
                    } 
                    else if (Vector3.Distance(vertices[i], vertices[i+2]) > maxNonDiagonalLength)
                    {
                        alpha = 2.0f / 3.0f;
                    }
                }
                
                for (var vertexNum = 0; vertexNum < 3; vertexNum++)
                {
                    var index = i + vertexNum;
                    var vertex = triangles[index];
                    
                    Color color = Color.black;
                    switch (vertexNum)
                    {
                        case 0:
                            color = Color.red;
                            break;
                        case 1:
                            color = Color.green;
                            break;
                        case 2:
                            color = Color.blue;
                            break;
                    }

                    color.a = alpha;
                    
                    barycentricCoordinates[vertex] = color;
                }
            }

            mesh.colors = barycentricCoordinates;
        }
    }
}