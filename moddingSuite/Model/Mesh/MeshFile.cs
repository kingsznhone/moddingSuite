using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using moddingSuite.Model.Ndfbin;

namespace moddingSuite.Model.Mesh
{
    public class MeshFile
    {
        public MeshHeader Header { get; set; }
        public MeshSubHeader SubHeader { get; set; }

        public ObservableCollection<MeshContentFile> MultiMaterialMeshFiles { get; set; }
        public NdfBinary TextureBindings { get; set; }

        // ✅ PARSED — populated by ReadMultiMaterialMeshes()
        // Each entry groups consecutive SingleMaterialMesh entries (offset + count into that array)
        public List<MultiMaterialMesh> MultiMaterialMeshes { get; set; }

        // ✅ PARSED — populated by ReadSingleMaterialMeshes()
        // Each entry is one GPU draw call: VertexBufferIndex + IndexBufferIndex + S2 + S3
        public List<SingleMaterialMesh> SingleMaterialMeshes { get; set; }

        // ✅ PARSED — populated by ReadIndex1DBufferHeaders()
        public List<Index1DBufferHeader> Index1DBufferHeaders { get; set; }

        // ✅ PARSED — populated by ReadVertex1DBufferHeaders()
        public List<Vertex1DBufferHeader> Vertex1DBufferHeaders { get; set; }

        // ✅ PARSED — populated by ReadVertexTypeNames()
        // S2 in Vertex1DBufferHeader is an index into this list
        public List<string> VertexTypeNames { get; set; }

        // ✅ PARSED — populated by ReadVertexTypeNames() via VertexFormatParser
        public List<VertexFormat> VertexFormats { get; set; }

        // ✅ PARSED — populated by ReadIndexBufferStreams()
        // IndexBufferSlices[i] corresponds to IndexBufferHeaders[i].
        // Element type is ushort (16-bit) or uint (32-bit) depending on IndexStride.
        public int IndexStride { get; set; }
        public List<ushort[]> IndexBufferSlices { get; set; }

        // ✅ PARSED — populated by ReadVertexBufferStreams()
        // VertexBufferSlices[i] is a raw byte[] for VertexBufferHeaders[i].
        // Interpret using VertexFormats[VertexBufferHeaders[i].S2].
        public List<byte[]> VertexBufferSlices { get; set; }
    }
}
